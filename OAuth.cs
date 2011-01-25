//
// Copyright (c) 2010 Jérémie "garuma" Laval
//
// Based on TweetStation code (c) Miguel de Icaza
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

		public UserInfos (string id, string name)
		{
			UserId = long.Parse (id);
			UserName = name;
		}

		public override string ToString ()
		{
			return string.Format ("User infos: ({0}, {1})", UserId.ToString (), UserName);
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
			var headers = new Dictionary<string,string> () {
				{ "oauth_callback", PercentEncode (config.UrlCallback) },
				{ "oauth_consumer_key", config.ConsumerKey },
				{ "oauth_nonce", MakeNonce () },
				{ "oauth_signature_method", "HMAC-SHA1" },
				{ "oauth_timestamp", MakeTimestamp () },
				{ "oauth_version", "1.0" }};
				
			string signature = MakeSignature ("POST", RequestUrl, headers);
			string compositeSigningKey = MakeSigningKey (config.ConsumerSecret, null);
			string oauth_signature = MakeOAuthSignature (compositeSigningKey, signature);

			var wc = new WebClient ();
			headers.Add ("oauth_signature", OAuth.PercentEncode (oauth_signature));
			wc.Headers [HttpRequestHeader.Authorization] = HeadersToOAuth (headers);

			return Task<OAuthToken>.Factory.StartNew (() => {
					try {
						System.Net.ServicePointManager.Expect100Continue = false;
						var result = HttpUtility.ParseQueryString (wc.UploadString (new Uri (RequestUrl), string.Empty));
						Console.WriteLine ("Twitter call handed out: {0} and {1}", result["oauth_token"], result["oauth_token_secret"]);
						OAuthToken token = new OAuthToken (result["oauth_token"], result["oauth_token_secret"]);
						return token;
					} catch (WebException e) {
						var x = e.Response.GetResponseStream ();
						var j = new System.IO.StreamReader (x);
						Console.WriteLine (j.ReadToEnd ());
						Console.WriteLine (e);
						throw e;
					}
				});
		}

		public Task<Tuple<OAuthToken, UserInfos>> AcquireAccessToken (OAuthToken requestToken, string tokenVerifier)
		{
			var headers = new Dictionary<string,string> () {
				{ "oauth_consumer_key", config.ConsumerKey },
				{ "oauth_nonce", MakeNonce () },
				{ "oauth_signature_method", "HMAC-SHA1" },
				{ "oauth_timestamp", MakeTimestamp () },
				{ "oauth_version", "1.0" }};
			var content = string.Empty;
			headers.Add ("oauth_token", requestToken.Token);
			headers.Add ("oauth_verifier", tokenVerifier);

			string signature = MakeSignature ("POST", AccessUrl, headers);
			string compositeSigningKey = MakeSigningKey (config.ConsumerSecret, requestToken.TokenSecret);
			string oauth_signature = MakeOAuthSignature (compositeSigningKey, signature);

			var wc = new WebClient ();
			headers.Add ("oauth_signature", OAuth.PercentEncode (oauth_signature));

			wc.Headers [HttpRequestHeader.Authorization] = HeadersToOAuth (headers);

			return Task<Tuple<OAuthToken, UserInfos>>.Factory.StartNew (() => {
					try {
						System.Net.ServicePointManager.Expect100Continue = false;
						var result = HttpUtility.ParseQueryString (wc.UploadString (new Uri (AccessUrl), content));

						OAuthToken token = new OAuthToken (result["oauth_token"], result["oauth_token_secret"]);
						UserInfos infos = new UserInfos (result["user_id"],
						                                 result["screen_name"]);

						return Tuple.Create (token, infos);
					} catch (WebException e) {
						var x = e.Response.GetResponseStream ();
						var j = new System.IO.StreamReader (x);
						Console.WriteLine (j.ReadToEnd ());
						Console.WriteLine (e);
						throw e;
						// fallthrough for errors
					}
				});
		}

		public string GetAuthorization (OAuthToken tokens, string method, Uri uri, string data)
		{
			var headers = new Dictionary<string, string>() {
				{ "oauth_consumer_key", config.ConsumerKey },
				{ "oauth_nonce", MakeNonce () },
				{ "oauth_signature_method", "HMAC-SHA1" },
				{ "oauth_timestamp", MakeTimestamp () },
				{ "oauth_token", tokens.Token },
				{ "oauth_version", "1.0" }};
			var signatureHeaders = new Dictionary<string,string> (headers);

			// Add the data and URL query string to the copy of the headers for computing the signature
			if (data != null && !string.IsNullOrEmpty (data)){
				var parsed = HttpUtility.ParseQueryString (data);
				foreach (string k in parsed.Keys){
					signatureHeaders.Add (k, OAuth.PercentEncode (parsed [k]));
				}
			}

			var nvc = HttpUtility.ParseQueryString (uri.Query);
			foreach (string key in nvc){
				if (key != null)
					signatureHeaders.Add (key, OAuth.PercentEncode (nvc [key]));
			}

			string signature = MakeSignature (method, uri.GetLeftPart (UriPartial.Path), signatureHeaders);
			string compositeSigningKey = MakeSigningKey (config.ConsumerSecret, tokens.TokenSecret);
			string oauth_signature = MakeOAuthSignature (compositeSigningKey, signature);

			headers.Add ("oauth_signature", OAuth.PercentEncode (oauth_signature));

			return HeadersToOAuth (headers);
		}

		public string GetAuthUrl (OAuthToken token)
		{
			return string.Format (AuthenticateUrl, token.Token);
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

		static string MakeSignature (string method, string base_uri, Dictionary<string,string> headers)
		{
			var items = from k in headers.Keys orderby k
				select k + "%3D" + OAuth.PercentEncode (headers [k]);

			return method + "&" + OAuth.PercentEncode (base_uri) + "&" + 
				string.Join ("%26", items.ToArray ());
		}
		
		static string MakeSigningKey (string consumerSecret, string oauthTokenSecret)
		{
			return OAuth.PercentEncode (consumerSecret) + "&" + (oauthTokenSecret != null ? OAuth.PercentEncode (oauthTokenSecret) : string.Empty);
		}
		
		static string MakeOAuthSignature (string compositeSigningKey, string signatureBase)
		{
			var sha1 = new HMACSHA1 (Encoding.UTF8.GetBytes (compositeSigningKey));
			
			return Convert.ToBase64String (sha1.ComputeHash (Encoding.UTF8.GetBytes (signatureBase)));
		}

		static string HeadersToOAuth (Dictionary<string,string> headers)
		{
			return "OAuth " + String.Join (",", (from x in headers.Keys select String.Format ("{0}=\"{1}\"", x, headers [x])).ToArray ());
		}

		public static string PercentEncode (string s)
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