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
	public class Apachai : ManosApp 
	{
		readonly static ConfigManager c = new ConfigManager ("config.json");
		readonly static BackingStore store = new BackingStore ();
		readonly static OAuthConfig oauthConfig;
		readonly static OAuth oauth;
		readonly static string baseServerUrl;

		static Apachai ()
		{
			oauthConfig = new OAuthConfig (c.GetOrThrow<string> ("twitterKey"),
			                               c.GetOrThrow<string> ("twitterSecret"),
			                               c.GetOrThrow<string> ("twitterCallback"));
			oauth = new OAuth (oauthConfig);
			baseServerUrl = c.GetOrThrow<string> ("serverBaseUrl");
		}

		public Apachai ()
		{
			Route ("/Content/", new StaticContentModule ());
			AddPipe (new Manos.Util.AccessLogger ("access.log"));
		}

		[Route ("/", "/Home", "/Index", "/Post")]
		public void Index (IManosContext ctx)
		{
			string id = ctx.Request.Cookies.Get ("apachai:userId");
			string token = ctx.Request.Cookies.Get ("apachai:token");

			if (string.IsNullOrEmpty (id) || !store.DoWeKnowUser (long.Parse (id), token)) {
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
		public void Test (IManosContext ctx)
		{
			ctx.Response.SendFile ("authenticating.html");
			ctx.Response.End ();
		}

		[Route ("/RequestTokens")]
		public void DoLogin (IManosContext ctx)
		{
			oauth.AcquireRequestToken ().ContinueWith (req => {
					Log.Info ("Got back from request token call: " + req.Result);
					var url = oauth.GetAuthUrl (req.Result);
					store.SaveTempTokenSecret (req.Result.Token, req.Result.TokenSecret);
					Log.Info ("Redirect URL is: " + url);

					ctx.Response.WriteLine (url);
					ctx.Response.End ();
				}, TaskContinuationOptions.ExecuteSynchronously);
		}

		[Route ("/AuthCallback")]
		public void AuthCallback (IManosContext ctx)
		{
			string token = ctx.Request.Data["oauth_token"];
			string tokenVerifier = ctx.Request.Data["oauth_verifier"];

			Log.Info ("Args: {0} and {1}", token, tokenVerifier);

			oauth.AcquireAccessToken (new OAuthToken (token, store.GetTempTokenSecret (token)), tokenVerifier)
				.ContinueWith (resultTask => {
						var result = resultTask.Result;
						var userInfos = result.Item2;
						var tokens = result.Item1;
						
						Log.Info ("Got back from access token call: {0} and {1}", userInfos.ToString (), tokens.ToString ());
						
						if (!store.DoWeKnowUser (userInfos.UserId)) {
							store.SetUserInfos (userInfos.UserId, userInfos.UserName);

							var twitter = new Twitter (oauth);
							twitter.Tokens = tokens;

							var retDict = JSON.JsonDecode (twitter.GetUserInformations ()) as Dictionary<object, object>;

							if (retDict != null)
								store.SetExtraUserInfos (userInfos.UserId,
								                         (string)retDict["profile_image_url"],
								                         (string)retDict["name"]);
						}
						store.SetUserAccessTokens (userInfos.UserId, tokens.Token, tokens.TokenSecret);
						
						ctx.Response.SetCookie ("apachai:userId", userInfos.UserId.ToString ());
						ctx.Response.SetCookie ("apachai:token", tokens.Token);
						ctx.Response.Redirect ("/Post");
						ctx.Response.End ();
					}, TaskContinuationOptions.ExecuteSynchronously);
		}

		[Route ("/favicon.ico")]
		public void Favicon (IManosContext ctx)
		{
			ctx.Response.SendFile ("Content/img/favicon.ico");
			ctx.Response.End ();
		}

		[Post ("/DoPost")]
		public void DoPost (IManosContext ctx)
		{
			IHttpRequest req = ctx.Request;

			var uid = long.Parse (req.Cookies.Get ("apachai:userId"));
			if (!store.DoWeKnowUser (uid)) {
				ctx.Response.Redirect ("/Login");
				ctx.Response.End ();
			}

			string twittertext = req.PostData.GetString ("twittertext").TrimEnd ('\n', '\r', ' ');

			if (req.Files.Count == 0)
				Log.Debug ("No file received");

			var file = req.Files.Values.First ().Contents;

			if (req.Files.Count == 0 || !CheckImageType (file)) {
				ctx.Response.Redirect ("/Post?error=1");
				ctx.Response.End ();

				return;
			}

			var filename = HandleUploadedFile (file);

			// TODO: find that back with ctx
			var finalUrl = baseServerUrl + "/i/" + filename;
			var twitter = new Twitter (oauth);
			twitter.Tokens = store.GetUserAccessTokens (uid);
			Log.Info ("Going to send tweet with (text = {0}) and (url = {1})", twittertext, finalUrl);

			twitter.SendApachaiTweet (twittertext, finalUrl)
				.ContinueWith ((ret) => {
						Log.Info ("Registered final tweet, {0} | {1} | {2} | {3}", uid, filename, twittertext, ret.Result);
						store.RegisterImageWithTweet (uid,
						                              filename,
						                              string.IsNullOrEmpty (twittertext) ? string.Empty : twittertext,
						                              ret.Result);

						ctx.Response.Redirect ("/i/" + filename);
						ctx.Response.End ();
					});
		}

		[Route ("/s/{id}")]
		public void ShowShortUrlPicture (IManosContext ctx, string id)
		{
			string permaId;

			if (string.IsNullOrEmpty (id) || !store.FindPermaFromShort (id, out permaId)) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
			}

			ctx.Response.Redirect ("/i/" + permaId);
			ctx.Response.End ();
		}

		[Route ("/i/{id}")]
		public void ShowPicture (IManosContext ctx, string id)
		{
			Log.Info ("ShowPicture: " + id);
			if (!File.Exists ("Content/img/" + id)) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
				return;
			}

			ctx.Response.SendFile ("home.html");
			ctx.Response.End ();
		}

		// TODO: also wrap that up in a Task, image processing can be long running
		[Route ("/infos/{id}")]
		public void FetchInformations (IManosContext ctx, string id)
		{
			var json = store.GetOrSetPictureInfos (id, () => {
					if (!File.Exists ("Content/img/" + id))
						return string.Empty;

					JsonStringDictionary dict = new JsonStringDictionary ();
					var shortUrl = store.GetShortUrlForImg (id);
					dict[string.Empty] = string.Format ("<a href=\\\"{0}\\\">Link to picture</a>", shortUrl);

					TagLibMetadata metadata = new TagLibMetadata (id);
					if (!metadata.IsValid) {
						Log.Info (id + " is invalid file");
						return dict.Json;
					}

					metadata.FillUp (dict);

					return dict.Json;
				});

			Log.Info ("Fetching infos for {0} and returning: {2}", id.ToString (), json);

			if (string.IsNullOrEmpty (json)) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
				return;
			}

			ctx.Response.WriteLine (json);
			ctx.Response.End ();
		}

		[Route ("/tweet/{id}")]
		public void FetchTweetInformations (IManosContext ctx, string id)
		{
			string avatar, tweet;
			store.GetTwitterInfosFromImage (id, out avatar, out tweet);

			JsonStringDictionary dict = new JsonStringDictionary ();
			dict["avatar"] = avatar;
			dict["tweet"] = System.Web.HttpUtility.HtmlEncode (tweet);

			var json = dict.Json;

			Log.Info ("Fetching tweet infos for {0} and returning {1}", id.ToString (), json);

			if (string.IsNullOrEmpty (json)) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
				return;
			}

			ctx.Response.WriteLine (json);
			ctx.Response.End ();
		}
		
		static bool CheckImageType (Stream file)
		{
			// For now only check some magic header value (not that we can do much else)
			return 0xD8FF == new BinaryReader (file).ReadUInt16 ();
		}

		static string HandleUploadedFile (Stream file)
		{
			string filename = Hasher.Hash (file);
			using (FileStream fs = File.OpenWrite ("Content/img/" + filename))
				file.CopyTo (fs);

			return filename;
		}
	}
}
