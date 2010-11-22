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
	public class Twitter
	{
		static readonly Uri twitterPushUrl = new Uri ("http://api.twitter.com/1/statuses/update.json");
		static readonly Uri twitterVerifyCredentials = new Uri ("http://api.twitter.com/1/account/verify_credentials.json");

		OAuth oauth;

		public Twitter (OAuth oauth)
		{
			this.oauth = oauth;
		}

		public OAuthToken Tokens {
			get;
			set;
		}

		public Task<string> SendApachaiTweet (string tweet, string pictureUrl)
		{
			return UrlShortener.GetShortenedUrl (pictureUrl)
				.ContinueWith ((ret) => {
						Console.WriteLine ("Got url back: " + ret.Result);

						// If no tweet, no need for the API call
						if (string.IsNullOrEmpty (tweet))
							return ret.Result;

						string status = OAuth.PercentEncode (tweet + " " + ret.Result);

						WebClient wc = new WebClient ();
						wc.Headers[HttpRequestHeader.Authorization] = oauth.GetAuthorization (Tokens, "POST", twitterPushUrl, "status=" + status);
						var postData = "status=" + status;
						try {
							// See http://groups.google.com/group/twitter-development-talk/browse_thread/thread/7c67ff1a2407dee7
							System.Net.ServicePointManager.Expect100Continue = false;
							wc.UploadData (twitterPushUrl, Encoding.UTF8.GetBytes (postData));
						} catch (WebException e) {
							var x = e.Response.GetResponseStream ();
							var j = new System.IO.StreamReader (x);
							Console.WriteLine (j.ReadToEnd ());
							Console.WriteLine (e);
							throw e;
						}

						return ret.Result;
					});
		}

		public string GetUserInformations ()
		{
			WebClient wc = new WebClient ();

			wc.Headers[HttpRequestHeader.Authorization]
				= oauth.GetAuthorization (Tokens, "GET", twitterVerifyCredentials, string.Empty);

			System.Net.ServicePointManager.Expect100Continue = false;
			return wc.DownloadString (twitterVerifyCredentials);
		}
	}
}