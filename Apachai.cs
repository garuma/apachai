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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Text;

using Manos;
using Manos.Http;

namespace Apachai
{
	public static class ResponseExtensions
	{
		public static void End (this IHttpResponse response)
		{
			response.Finish ();
		}
	}

	public class Apachai : ManosApp 
	{
		readonly static BackingStore store = new BackingStore ();
		readonly static OAuthConfig oauthConfig	=
			new OAuthConfig ("MK4e0OGcH1Ni7fxpfiwjcg", "XJSiiavfjtqN1VOa4AIIOlnerRPCcJJnDDBLNoLIU", "http://localhost:8080/AuthCallback");

		public Apachai ()
		{
			Route ("/Content/", new StaticContentModule ());
		}

		[Route ("/", "/Home", "/Index", "/Post")]
		public void Index (IManosContext ctx)
		{
			if (string.IsNullOrEmpty (ctx.Request.Cookies.Get ("apachai:userId"))) {
				ctx.Response.Redirect ("/Login");
				ctx.Response.End ();
			}
			ctx.Response.SendFile ("post.html");
			ctx.Response.End ();
		}

		[Route ("/Login")]
		public void Login (IManosContext ctx)
		{
			ctx.Response.SendFile ("sign.html");
			ctx.Response.End ();
		}

		[Route ("/DoLogin")]
		public void DoLogin (IManosContext ctx)
		{
			OAuth oauth = new OAuth (oauthConfig);

			var cont = oauth.AcquireRequestToken ().ContinueWith (req => {
					Console.WriteLine ("Got back from request token call: " + req.Result);
					var url = oauth.GetAuthUrl (req.Result);
					store.SaveTempTokenSecret (req.Result.Token, req.Result.TokenSecret);
					Console.WriteLine ("Redirect URL is: " + url);

					ctx.Response.Redirect (url);
					ctx.Response.End ();					
				});
			cont.Wait ();
		}

		[Route ("/AuthCallback")]
		public void AuthCallback (IManosContext ctx)
		{
			string token = ctx.Request.Data["oauth_token"];
			string tokenVerifier = ctx.Request.Data["oauth_verifier"];

			Console.WriteLine ("Args: {0} and {1}", token, tokenVerifier);

			OAuth oauth = new OAuth (oauthConfig);

			var cont = oauth.AcquireAccessToken (new OAuthToken (token, store.GetTempTokenSecret (token)), tokenVerifier)
				.ContinueWith (resultTask => {
						var result = resultTask.Result;
						var userInfos = result.Item2;
						var tokens = result.Item1;
						
						Console.WriteLine ("Got back from access token call: {0} and {1}", userInfos.ToString (), tokens.ToString ());
						
						if (!store.DoWeKnowUser (userInfos.UserId))
							store.SetUserInfos (userInfos.UserId, userInfos.UserName, userInfos.UserAvatarUrl);
						store.SetUserAccessTokens (userInfos.UserId, tokens.Token, tokens.TokenSecret);
						
						ctx.Response.SetCookie ("apachai:userId", userInfos.UserId.ToString ());
						ctx.Response.Redirect ("/Post");
						ctx.Response.End ();
					});

			cont.Wait ();
		}

		[Route ("/favicon.ico")]
		public void Favicon (IManosContext ctx)
		{
			ctx.Response.SendFile ("Content/img/favicon.ico");
			ctx.Response.End ();
		}

		[Route ("/DoPost")]
		public void DoPost (IManosContext ctx, string twittertext)
		{
			IHttpRequest req = ctx.Request;

			if (req.Files.Count == 0)
				Console.WriteLine ("No file received");

			if (string.IsNullOrEmpty (twittertext) || req.Files.Count == 0 || CheckImageType (req.Files.Keys.First ())) {
				ctx.Response.Redirect ("/Post?error=1");
				ctx.Response.End ();
				return;
			}

			var filename = HandleUploadedFile (req.Files.Values.First ().Contents);

			// TODO: find that back with ctx
			var domain = "http://localhost:8080";
			//var shorturl = GetShortenedUrl (domain + "/i/" + filename);
			// TODO: setup a continuation that post the link + twittertext to Twitter with OAuth

			ctx.Response.Redirect ("/i/" + filename);
			ctx.Response.End ();
		}

		[Route ("/i/{id}")]
		public void ShowPicture (IManosContext ctx, string id)
		{
			Console.WriteLine ("ShowPicture: " + id);
			if (!File.Exists ("Content/img/" + id)) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
				return;
			}

			ctx.Response.SendFile ("home.html");
			ctx.Response.End ();
		}

		[Route ("/infos/{id}")]
		public void FetchInformations (IManosContext ctx, string id)
		{
			Console.WriteLine ("Fetching infos for: " + id);
			
			var json = store.GetOrSetPictureInfos (id, () => {
					TagLibMetadata metadata = new TagLibMetadata (id);
					if (!metadata.IsValid) {
						Console.WriteLine (id + " is invalid file");
						return string.Empty;
					}
					
					JsonStringDictionary dict = new JsonStringDictionary ();
					metadata.FillUp (dict);
					
					return dict.Json;
				});

			Console.WriteLine ("Returning: " + json);

			if (string.IsNullOrEmpty (json)) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
				return;
			}

			ctx.Response.WriteLine (json);
			ctx.Response.End ();
		}
		
		static bool CheckImageType (string mime)
		{
			return false;
			return string.IsNullOrEmpty (mime) || (!mime.Equals ("image/jpg", StringComparison.Ordinal) && !mime.Equals ("image/png", StringComparison.Ordinal));
		}

		static string HandleUploadedFile (Stream file)
		{
			using (MemoryStream buffer = new MemoryStream ((int)file.Length)) {
				file.CopyTo (buffer);
				string filename = Hasher.Hash (buffer);
				using (FileStream fs = File.OpenWrite ("Content/img/" + filename))
					buffer.CopyTo (fs);

				return filename;
			}
		}

		static Task<string> GetShortenedUrl (string initial_url)
		{
			WebClient wc = new WebClient ();
			wc.Encoding = Encoding.UTF8;

			return Task<string>.Factory.StartNew (() => wc.DownloadString ("http://goo.gl/api/shorten?url=" + initial_url));
		}
	}
}
