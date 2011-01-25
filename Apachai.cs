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
	using Effects;

	public class Apachai : ManosApp 
	{
		readonly static ConfigManager c;
		readonly static BackingStore store;
		readonly static OAuthConfig oauthConfig;
		readonly static OAuth oauth;
		readonly static string baseServerUrl;
		readonly static string imgDirectory;
		readonly static bool testInstance;

		readonly StaticContentModule staticContent;

		static Apachai ()
		{
			c = new ConfigManager ("config.json");
			store = new BackingStore (c);
			oauthConfig = new OAuthConfig (c.GetOrThrow<string> ("twitterKey"),
			                               c.GetOrThrow<string> ("twitterSecret"),
			                               c.GetOrThrow<string> ("twitterCallback"));
			oauth = new OAuth (oauthConfig);
			testInstance = c.GetOrDefault<bool> ("testInstance", false);
			baseServerUrl = c.GetOrThrow<string> ("serverBaseUrl");
			imgDirectory = c.GetOrDefault<string> ("imagesDirectory", "Pictures");
			UrlShortener.Store = store;
		}

		public Apachai ()
		{
			Route ("/Content/", (staticContent = new StaticContentModule ("Content")));
			Route ("/Pictures/", new PictureContentModule ());
			AddPipe (new Manos.Util.AccessLogger ("access.log"));
		}

		[Route ("/", "/Home", "/Index", "/Post")]
		public void Index (IManosContext ctx)
		{
			string id = ctx.Request.Cookies.Get ("apachai:userId");
			string token = ctx.Request.Cookies.Get ("apachai:token");
			long uid;

			if (string.IsNullOrEmpty (id) || !long.TryParse (id, out uid) || !store.DoWeKnowUser (uid, token))
				ctx.Response.Redirect ("/Login");

			HttpServing (ctx, HtmlPaths.PostPage);
		}

		[Route ("/Login")]
		public void Login (IManosContext ctx)
		{
			HttpServing (ctx, HtmlPaths.SignPage);
		}

		[Route ("/DoLogin")]
		public void Test (IManosContext ctx)
		{
			HttpServing (ctx, HtmlPaths.AuthPage);
		}

		[Route ("/RequestTokens")]
		public void DoLogin (IManosContext ctx)
		{
			if (testInstance) {
				ctx.Response.End ("/AuthCallback");
				return;
			}

			oauth.AcquireRequestToken ().ContinueWith (req => {
					Log.Info ("Got back from request token call: " + req.Result);
					var url = oauth.GetAuthUrl (req.Result);
					store.SaveTempTokenSecret (req.Result.Token, req.Result.TokenSecret);
					Log.Info ("Redirect URL is: " + url);

					ctx.Response.End (url);
				}, TaskContinuationOptions.ExecuteSynchronously);
		}

		[Route ("/AuthCallback")]
		public void AuthCallback (IManosContext ctx)
		{
			if (testInstance) {
				ctx.Response.SetCookie ("apachai:userId", 1.ToString ());
				ctx.Response.SetCookie ("apachai:token", "bar");
				store.SetUserInfos (1, "the_test");
				store.SetExtraUserInfos (1, "http://neteril.org/img/twitter.png", "The Test", "http://neteril.org", "My Foo Bar Life");
				store.SetUserAccessTokens (1, "bar", "bar");
				ctx.Response.Redirect ("/Post");

				return;
			}

			string token = ctx.Request.Data["oauth_token"];
			string tokenVerifier = ctx.Request.Data["oauth_verifier"];

			Log.Info ("Args: {0} and {1}", token, tokenVerifier);

			oauth.AcquireAccessToken (new OAuthToken (token, store.GetTempTokenSecret (token)), tokenVerifier)
				.ContinueWith (resultTask => {
						var result = resultTask.Result;
						var userInfos = result.Item2;
						var tokens = result.Item1;
						
						Log.Info ("Got back from access token call: {0} and {1}", userInfos.ToString (), tokens.ToString ());
						
						bool first = false;
						if ((first = !store.DoWeKnowUser (userInfos.UserId)))
								store.SetUserInfos (userInfos.UserId, userInfos.UserName);

						if (first || store.DoesUserNeedInfoUpdate (userInfos.UserId) ) {
							var twitter = new Twitter (oauth);
							twitter.Tokens = tokens;

							var retDict = JSON.JsonDecode (twitter.GetUserInformations ()) as Dictionary<object, object>;

							if (retDict != null)
								store.SetExtraUserInfos (userInfos.UserId,
								                         ((string)retDict["profile_image_url"]).Replace ("normal.", "reasonably_small."),
								                         (string)retDict["name"],
								                         (string)retDict["url"],
								                         (string)retDict["description"]);

						}
						store.SetUserAccessTokens (userInfos.UserId, tokens.Token, tokens.TokenSecret);
						
						ctx.Response.SetCookie ("apachai:userId", userInfos.UserId.ToString ());
						ctx.Response.SetCookie ("apachai:token", tokens.Token);
						ctx.Response.Redirect ("/Post");
					}, TaskContinuationOptions.ExecuteSynchronously);
		}

		[Route ("/favicon.ico")]
		public void Favicon (IManosContext ctx)
		{
			ctx.Response.SendFile (Path.Combine ("Content", "img", "favicon.ico"));
			ctx.Response.End ();
		}

		[Post ("/DoPost")]
		public void DoPost (IManosContext ctx)
		{
			IHttpRequest req = ctx.Request;

			var cookie = req.Cookies.Get ("apachai:userId");
			long uid;
			if (string.IsNullOrEmpty (cookie) || !long.TryParse (cookie, out uid) || !store.DoWeKnowUser (uid))
				ctx.Response.Redirect ("/Login");

			string twittertext = req.PostData.GetString ("twittertext").TrimEnd ('\n', '\r').Trim ();

			if (req.Files.Count == 0)
				Log.Debug ("No file received");

			var file = req.Files.Values.First ().Contents;

			if (req.Files.Count == 0 || !CheckImageType (file)) {
				ctx.Response.Redirect ("/Post?error=1");
				return;
			}

			// HACK: the TrimEnd should be in Manos
			var filename = HandleUploadedFile (file, uid.ToString (), req.PostData.GetString ("effect").TrimEnd ('\n', '\r'));

			// TODO: find that back with ctx
			var finalUrl = baseServerUrl + "/i/" + filename;
			var twitter = new Twitter (oauth);
			twitter.Tokens = testInstance ? null : store.GetUserAccessTokens (uid);

			Log.Info ("Going to send tweet with (text = {0}) and (url = {1})", twittertext, finalUrl);

			var task = !testInstance ?
				twitter.SendApachaiTweet (twittertext, finalUrl, filename, baseServerUrl + "/s/") :
				UrlShortener.GetShortenedId ();

			task.ContinueWith ((ret) => {
					Log.Info ("Registered final tweet, {0} | {1} | {2} | {3}", uid, filename, twittertext, ret.Result);
					store.RegisterImageWithTweet (uid,
					                              filename,
					                              string.IsNullOrEmpty (twittertext) ? string.Empty : twittertext,
					                              finalUrl,
					                              ret.Result);
					store.MapShortToLongUrl (ret.Result, filename);
					ctx.Response.Redirect ("/i/" + filename);
				}, TaskContinuationOptions.ExecuteSynchronously);
		}

		[Route ("/s/{id}")]
		public void ShowShortUrlPicture (IManosContext ctx, string id)
		{
			string permaId;
			Log.Info ("Want us to show {0}", id);

			if (string.IsNullOrEmpty (id) || !store.FindPermaFromShort (id, out permaId)) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
			}

			ctx.Response.Redirect ("/i/" + permaId);
		}

		[Route ("/i/{id}")]
		public void ShowPicture (IManosContext ctx, string id)
		{
			Log.Info ("ShowPicture: " + id);
			if (string.IsNullOrEmpty (id) || !File.Exists (Path.Combine (imgDirectory, id))) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
				return;
			}

			HttpServing (ctx, HtmlPaths.HomePage);
		}

		[Route ("/og/{id}")]
		public void ShowOpenGraphData (IManosContext ctx, string id)
		{
			if (string.IsNullOrEmpty (id) || !File.Exists (Path.Combine (imgDirectory, id))) {
				ctx.Response.StatusCode = 404;
				ctx.Response.End ();
				return;
			}

			string pageUrl = baseServerUrl + "/i/" + id;
			string imageUrl = baseServerUrl + "/Pictures/" + id;

			RawServing (ctx.Response, "text/html", 600, string.Format (@"<html xmlns:og=""http://ogp.me/ns#"">
<head>
<title>Picture on Apachaï</title>
<meta charset=""utf-8"" />
<meta property=""og:title"" content=""Picture on Apachaï"" />
<meta property=""og:type"" content=""article"" />
<meta property=""og:url"" content=""{0}"" />
<meta property=""og:image"" content=""{1}"" />
<meta property=""og:description"" content=""Apachaï is designed to be a small and lightweight photo and picture sharing application (for services like Twitter) built on the Manos framework"" />
<meta property=""og:site_name"" content=""Apachaï"" />
</head>
<script type=""text/javascript"">window.location = '{0}';</script>
</html>", pageUrl, imageUrl));
		}

		[Route ("/infos/{id}")]
		public void FetchInformations (IManosContext ctx, string id)
		{
			if (string.IsNullOrEmpty (id))
				HandleJson (string.Empty, ctx.Response);

			string json;

			if (store.GetPicturesInfos (id, out json)) {
				Log.Info ("Fetching infos for {0} and returning: {1}", id.ToString (), json);
				HandleJson (json, ctx.Response);
				return;
			}

			if (!File.Exists (Path.Combine (imgDirectory, id))) {
				HandleJson (string.Empty, ctx.Response);
				return;
			}

			Task.Factory.StartNew (() => {
					JsonStringDictionary dict = new JsonStringDictionary ();

					TagLibMetadata metadata = new TagLibMetadata (imgDirectory, id);
					if (!metadata.IsValid) {
						Log.Info (id + " is invalid file");
						json = string.Empty;
					} else {
						metadata.FillUp (dict);
						json = dict.Json;
					}
					metadata.Close ();

					store.SetPictureInfos (id, json);
					HandleJson (json, ctx.Response);
				});
		}

		[Route ("/tweet/{id}")]
		public void FetchTweetInformations (IManosContext ctx, string id)
		{
			if (string.IsNullOrEmpty (id))
				HandleJson (string.Empty, ctx.Response);

			string json;
			if (store.TryGetCachedTwitterInfos (id, out json)) {
				HandleJson (json, ctx.Response);
				return;
			}

			string avatar, tweet, screenname, name, url, desc;
			store.GetTwitterInfosFromImage (id, out avatar, out tweet, out screenname, out name, out url, out desc);

			JsonStringDictionary dict = new JsonStringDictionary ();
			dict["avatar"] = avatar;
			dict["tweet"] = System.Web.HttpUtility.HtmlEncode (tweet);
			dict["name"] = name;
			dict["screenname"] = screenname;
			dict["url"] = url;
			dict["desc"] = desc;

			json = dict.Json;
			store.SetCachedTwitterInfos (id, json);

			Log.Info ("Fetching tweet infos for {0} and returning {1}", id.ToString (), json);
			HandleJson (json, ctx.Response);
		}

		[Route ("/links/{id}")]
		public void FetchLinkInformations (IManosContext ctx, string id)
		{
			if (string.IsNullOrEmpty (id))
				HandleJson (string.Empty, ctx.Response);

			string json;
			if (store.TryGetPictureLinks (id, out json)) {
				Log.Info ("Sending back links blob: {0}", json);
				HandleJson (json, ctx.Response);
				return;
			}

			JsonStringDictionary dict = new JsonStringDictionary ();

			var shortUrl = baseServerUrl + "/s/" + store.GetShortUrlForImg (id);
			var longUrl = baseServerUrl + "/i/" + id;
			var ogUrl = baseServerUrl + "/og/" + id;

			dict["short"] = shortUrl;
			dict["permanent"] = longUrl;
			dict["facebook"] = ogUrl;

			json = dict.Json;
			store.SetPictureLinks (id, json);

			HandleJson (json, ctx.Response);
		}

		[Route ("/recent/{id}")]
		public void FetchRecentPictures (IManosContext ctx, string id)
		{
			if (string.IsNullOrEmpty (id))
				HandleJson (string.Empty, ctx.Response);

			var list = store.GetImagesOfUserFromPic (id, 10);
			var json = '[' + list.Select (e => '"' + e + '"').Aggregate ((e1, e2) => e1 + ',' + e2) + ']';
			HandleJson (json, ctx.Response);
		}

		[Route ("/geo/{id}")]
		public void FetchGeoInformations (IManosContext ctx, string id)
		{
			if (string.IsNullOrEmpty (id))
				HandleJson (string.Empty, ctx.Response);

			string json;

			if (store.GetPictureGeo (id, out json)) {
				Log.Info ("Geolocation for {0}: {1}", id.ToString (), json);
				HandleJson (json, ctx.Response);
				return;
			}

			if (!File.Exists (Path.Combine (imgDirectory, id))) {
				HandleJson (string.Empty, ctx.Response);
				return;
			}

			Task.Factory.StartNew (() => {
					json = string.Empty;
					JsonStringDictionary dict = new JsonStringDictionary ();
					TagLibMetadata metadata = new TagLibMetadata (imgDirectory, id);

					if (!metadata.IsValid) {
						Log.Info (id + " is invalid file");
					} else {
						var coordinates = metadata.GeoCoordinates;
						if (coordinates != null) {
							var invCult = System.Globalization.CultureInfo.InvariantCulture;
							dict["latitude"] = coordinates.Item1.ToString (invCult);
							dict["longitude"] = coordinates.Item2.ToString (invCult);
							json = dict.Json;
						}
					}
					metadata.Close ();

					store.SetPictureGeo (id, json);
					HandleJson (json, ctx.Response);
				});
		}

		[Route ("/stats")]
		public void FetchStatistics (IManosContext ctx, string id)
		{
			string json;

			if (store.TryGetCachedStats (out json)) {
				HandleJson (json, ctx.Response);
				return;
			}

			var dict = new Dictionary<object, object> ();

			int picCount, userCount;
			store.GetCountStats (out picCount, out userCount);

			dict["picNumber"] = picCount;
			dict["userNumber"] = userCount;
			dict["latestPics"] = store.GetLastPicturesIds ();

			json = JSON.JsonEncode (dict);
			store.SetCachedStats (json);

			HandleJson (json, ctx.Response);
		}

		static bool CheckImageType (Stream file)
		{
			// For now only check some magic header value (not that we can do much else)
			return 0xD8FF == new BinaryReader (file).ReadUInt16 ();
		}

		static void HandleJson (string json, IHttpResponse response)
		{
			if (string.IsNullOrEmpty (json)) {
				response.StatusCode = 404;
				response.End ();
			} else {
				RawServing (response, "application/json", 600, json);
			}
		}

		string HandleUploadedFile (Stream file, string user, string transformation)
		{
			string filename = user + Hasher.Hash (file);
			string path = Path.Combine (imgDirectory, filename);

			using (FileStream fs = File.OpenWrite (path)) {
				file.CopyTo (fs);
				file.Close ();
			}

			Log.Info ("Transforming according to: " + transformation);
			PhotoEffect.ApplyTransformFromString (transformation, path);

			return filename;
		}

		void HttpServing (IManosContext ctx, string htmlPath)
		{
			staticContent.Content (ctx, htmlPath);
		}

		static void RawServing (IHttpResponse response, string mime, int maxAge, string data)
		{
			response.Headers.SetNormalizedHeader ("Content-Type", "Content-Type: "+mime+"; charset=utf-8");
			response.Headers.SetNormalizedHeader ("Cache-Control", "max-age=" + maxAge);
			response.End (data);
		}
	}
}
