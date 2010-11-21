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
	public class UrlShortener
	{
		const string user = "jeremie_laval";
		const string key = "SA7EgsngSYQPPqAp0Jp6qxpHK7AzbA";
		const string serviceUrl = "http://shr.im/api/1.0/post.text?api_user={0}&api_key={1}&url_src={2}&is_private=1";

		public static Task<string> GetShortenedUrl (string origUrl)
		{
			return Task<string>.Factory.StartNew (() => origUrl);
		}

		/* Shr.im is buggy, to aleviate pain, let's just return origUrl for now */
		/*public static Task<string> GetShortenedUrl (string origUrl)
		{
			var url = OAuth.PercentEncode (origUrl);
			var reqUrl = string.Format (serviceUrl, user, key, url);
			WebClient wc = new WebClient ();

			return Task<string>.Factory.StartNew (() => wc.DownloadString (reqUrl));
		}*/
	}
}