//
// Copyright (c) 2010 Jérémie "garuma" Laval
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Net;
using System.Web;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Apachai
{
	public class OAuthToken
	{
		public string Token {
			get;
			private set;
		}
		public string TokenSecret {
			get;
			private set;
		}

		public OAuthToken (string token, string tokenSecret)
		{
			this.Token = token;
			this.TokenSecret = tokenSecret;
		}

		public override string ToString ()
		{
			return string.Format ("OAuthTokens({0}, {1})", Token, TokenSecret);
		}
	}

	public class OAuthConfig
	{
		public string ConsumerKey {
			get;
			set;
		}

		public string ConsumerSecret {
			get;
			set;
		}

		public string UrlCallback {
			get;
			set;
		}

		public OAuthConfig (string key, string secret, string urlCallback)
		{
			ConsumerKey = key;
			ConsumerSecret = secret;
			UrlCallback = urlCallback;
		}
	}

	public class UserInfos 
	{
		public long UserId {
			get;
			private set;
		}

		public string UserName {
			get;
			private set;
		}

		public string UserAvatarUrl {
			get;
			private set;
		}

		public UserInfos (string id, string name, string avatarUrl)
		{
			UserId = long.Parse (id);
			UserName = name;
			UserAvatarUrl = avatarUrl;
		}

		public override string ToString ()
		{
			return string.Format ("User infos: ({0}, {1}, {2})", UserId.ToString (), UserName, UserAvatarUrl);
		}
	}

	public class OAuth
	{
		const string RequestUrl = "https://api.twitter.com/oauth/request_token";
		const string AuthenticateUrl = "https://api.twitter.com/oauth/authenticate?oauth_token={0}";
		const string AccessUrl = "https://api.twitter.com/oauth/access_token";

		static Random random = new Random ();
		static DateTime UnixBaseTime = new DateTime (1970, 1, 1);

		OAuthConfig config;

		public OAuth (OAuthConfig config)
		{
			if (config == null)
				throw new ArgumentNullException ();

			this.config = config;
		}

		public Task<OAuthToken> AcquireRequestToken ()
		{
			var data = new NameValueCollection ();
			data["oauth_callback"] = PercentEncode (config.UrlCallback);
			data["oauth_consumer_key"] = config.ConsumerKey;
			data["oauth_nonce"] = MakeNonce ();
			data["oauth_signature_method"] = "HMAC-SHA1";
			data["oauth_timestamp"] = MakeTimestamp ();
			data["oauth_version"] = "1.0";
				
			string signature = MakeSignature ("POST", RequestUrl, data);
			string compositeSigningKey = MakeSigningKey (config.ConsumerSecret, null);
			string oauth_signature = MakeOAuthSignature (compositeSigningKey, signature);
			data["oauth_signature"] = PercentEncode (oauth_signature);

			var t = Task<OAuthToken>.Factory.StartNew (() => {
					var wc = new WebClient ();
					wc.Headers[HttpRequestHeader.Authorization] = HeadersToOAuth (data);

					try {
						var result = wc.UploadString (RequestUrl, string.Empty);
						var parsed = HttpUtility.ParseQueryString (result);
						OAuthToken token = new OAuthToken (parsed["oauth_token"], parsed["oauth_token_secret"]);

						return token;
					} catch (Exception e) {
						Console.WriteLine (e);
					}

					return null;
				});
			t.Wait ();
			return t;
		}

		public string GetAuthUrl (OAuthToken token)
		{
			return string.Format (AuthenticateUrl, token.Token);
		}

		public Task<Tuple<OAuthToken, UserInfos>> AcquireAccessToken (OAuthToken requestToken, string tokenVerifier)
		{
			var data = new NameValueCollection ();
			data["oauth_consumer_key"] = config.ConsumerKey;
			data["oauth_nonce"] = MakeNonce ();
			data["oauth_signature_method"] = "HMAC-SHA1";
			data["oauth_token"] = requestToken.Token;
			data["oauth_timestamp"] = MakeTimestamp ();
			data["oauth_verifier"] = tokenVerifier;
			data["oauth_version"] = "1.0";

			Console.WriteLine ("Token secret for signing is: " + requestToken.TokenSecret);

			string signature = MakeSignature ("POST", AccessUrl, data);
			string compositeSigningKey = MakeSigningKey (config.ConsumerSecret, requestToken.TokenSecret);
			string oauth_signature = MakeOAuthSignature (compositeSigningKey, signature);
			data["oauth_signature"] = PercentEncode (oauth_signature);
						
			var t = Task<Tuple<OAuthToken, UserInfos>>.Factory.StartNew (() => {
					var wc = new WebClient ();
					wc.Headers[HttpRequestHeader.Authorization] = HeadersToOAuth (data);

					try {
						var result = wc.UploadString (RequestUrl, string.Empty);
						var parsed = HttpUtility.ParseQueryString (result);
						OAuthToken token = new OAuthToken (parsed["oauth_token"], parsed["oauth_token_secret"]);
						UserInfos infos = new UserInfos (parsed["user_id"],
						                                 parsed["screen_name"],
						                                 string.Empty);

						return Tuple.Create (token, infos);
					} catch (Exception e) {
						Console.WriteLine (e);
					}

					return null;
				});
			t.Wait ();
			return t;
		}

		static string HeadersToOAuth (NameValueCollection headers)
		{
			return "OAuth " + String.Join (",", headers.Keys.Cast<string> ().Select (k => string.Format ("{0}=\"{1}\"", k, headers[k])).ToArray ());
		}

		// 16-byte lower-case or digit string
		static string MakeNonce ()
		{
			var ret = new char [16];
			for (int i = 0; i < ret.Length; i++){
				int n = random.Next (35);
				if (n < 10)
					ret [i] = (char) (n + '0');
				else
					ret [i] = (char) (n-10 + 'a');
			}
			return new string (ret);
		}

		static string MakeTimestamp ()
		{
			return ((long) (DateTime.UtcNow - UnixBaseTime).TotalSeconds).ToString ();
		}
		
		// Makes an OAuth signature out of the HTTP method, the base URI and the headers
		static string MakeSignature (string method, string base_uri, NameValueCollection headers)
		{
			var items = headers.Cast<string> ().OrderBy (k => k).Select (ks =>ks + "%3D" + OAuth.PercentEncode (headers [ks]));

			return method + "&" + OAuth.PercentEncode (base_uri) + "&" + 
				string.Join ("%26", items.ToArray ());
		}
		
		static string MakeSigningKey (string consumerSecret, string oauthTokenSecret)
		{
			return OAuth.PercentEncode (consumerSecret) + "&" + (oauthTokenSecret != null ? OAuth.PercentEncode (oauthTokenSecret) : "");
		}
		
		static string MakeOAuthSignature (string compositeSigningKey, string signatureBase)
		{
			var sha1 = new HMACSHA1 (Encoding.UTF8.GetBytes (compositeSigningKey));
			
			return Convert.ToBase64String (sha1.ComputeHash (Encoding.UTF8.GetBytes (signatureBase)));
		}

		static string PercentEncode (string s)
		{
			var sb = new StringBuilder ();
			
			foreach (byte c in Encoding.UTF8.GetBytes (s)){
				if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.' || c == '~')
					sb.Append ((char) c);
				else {
					sb.AppendFormat ("%{0:X2}", c);
				}
			}
			return sb.ToString ();
		}
	}
}