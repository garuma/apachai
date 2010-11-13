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
using System.Security.Cryptography;
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
		static MD5 md5sum = MD5.Create ();
		static Redis redis = new Redis ();

		public Apachai ()
		{
			Route ("/Content/", new StaticContentModule ());
		}

		[Route ("/", "/Home", "/Index", "/Post")]
		public void Index (IManosContext ctx)
		{
			ctx.Response.SendFile ("post.html");
		}

		[Route ("/DoPost")]
		public void DoPost (IManosContext ctx, string twittertext)
		{
			IHttpRequest req = ctx.Request;

			if (req.Files.Count == 0)
				Console.WriteLine ("No file received");

			if (string.IsNullOrEmpty (twittertext) || req.Files.Count == 0 || CheckImageType (req.Files.Keys.First ())) {
				ctx.Response.Redirect ("/Post?error=1");
				return;
			}

			var filename = HandleUploadedFile (req.Files.Values.First ().Contents);

			// TODO: find that back with ctx
			var domain = "http://localhost:8080";
			var shorturl = GetShortenedUrl (domain + "/i/" + filename);
			// TODO: setup a continuation that post the link + twittertext to Twitter with OAuth

			ctx.Response.Redirect ("/i/" + filename);
		}

		[Route ("/i/{id}")]
		public void ShowPicture (IManosContext ctx, string id)
		{
			Console.WriteLine ("ShowPicture: " + id);
			if (!File.Exists ("Content/img/" + id)) {
				ctx.Response.StatusCode = 404;
				return;
			}

			ctx.Response.SendFile ("home.html");
		}

		[Route ("/infos/{id}")]
		public void FetchInformations (IManosContext ctx, string id)
		{
			Console.WriteLine ("Fetching infos for: " + id);
			if (!File.Exists ("Content/img/" + id)) {
				ctx.Response.StatusCode = 404;
				return;
			}

			string json = null;
			if (redis.ContainsKey (id)) {
				ctx.Response.WriteLine (redis[id]);
				return;
			}

			TagLib.File file = null;

			try {
				file = TagLib.File.Create("Content/img/" + id, "image/jpeg", TagLib.ReadStyle.Average);
			} catch (Exception e) {
				Console.WriteLine (e.ToString ());
				ctx.Response.StatusCode = 500;
				return;
			}
			Console.WriteLine ("File created");

			var image = file as TagLib.Image.File;
			
			var dict = new JsonStringDictionary ();

			if (image.Properties != null) {
				CheckAndAdd (dict, "Width: ", image.Properties.PhotoWidth);
				CheckAndAdd (dict, "Height: ", image.Properties.PhotoHeight);
				CheckAndAdd (dict, "Type: ", image.Properties.Description);
			}
			
			if (image.ImageTag != null) {
				CheckAndAdd (dict, "Comment: ", image.ImageTag.Comment);
				CheckAndAdd (dict, "Rating: ", image.ImageTag.Rating);
				CheckAndAdd (dict, "Date: ", image.ImageTag.DateTime);
				CheckAndAdd (dict, "Rating: ", image.ImageTag.Rating);
				CheckAndAdd (dict, "DateTime: ", image.ImageTag.DateTime);
				CheckAndAdd (dict, "Orientation: ", image.ImageTag.Orientation);
				CheckAndAdd (dict, "Software: ", image.ImageTag.Software);
				CheckAndAdd (dict, "ExposureTime: ", image.ImageTag.ExposureTime);
				CheckAndAdd (dict, "FNumber: ", image.ImageTag.FNumber);
				CheckAndAdd (dict, "ISOSpeedRatings: ", image.ImageTag.ISOSpeedRatings);
				CheckAndAdd (dict, "FocalLength: ", image.ImageTag.FocalLength);
				CheckAndAdd (dict, "FocalLength35mm: ", image.ImageTag.FocalLengthIn35mmFilm);
				CheckAndAdd (dict, "Make: ", image.ImageTag.Make);
				CheckAndAdd (dict, "Model: ", image.ImageTag.Model);
			}

			json = dict.Json;
			if (json == null) {
				ctx.Response.StatusCode = 500;
				return;
			}

			redis[id] = json;

			Console.WriteLine ("Returning: " + json);

			ctx.Response.WriteLine (json);
			ctx.Response.Finish ();
		}
		
		static bool CheckImageType (string mime)
		{
			return string.IsNullOrEmpty (mime) || (!mime.Equals ("image/jpg", StringComparison.Ordinal) && !mime.Equals ("image/png", StringComparison.Ordinal));
		}

		static void CheckAndAdd<TValue> (JsonStringDictionary dict, string key, TValue value)
		{
			if (value == null || string.IsNullOrEmpty (key))
				return;

			string sValue = value.ToString ();

			if (!string.IsNullOrEmpty (sValue) && !IsWhiteSpaces (sValue))
				dict[key] = sValue;
		}

		static bool IsWhiteSpaces (string str)
		{
			return str.All (char.IsWhiteSpace);
		}

		static string HandleUploadedFile (Stream file)
		{
			MemoryStream buffer = new MemoryStream ();
			file.CopyTo (buffer);

			byte[] hash = md5sum.ComputeHash (buffer);
			string filename = Convert.ToBase64String (hash);
			buffer.Seek (0, SeekOrigin.Begin);
			using (FileStream fs = File.OpenWrite ("Content/img/" + filename))
				buffer.CopyTo (fs);

			return filename;
		}

		static Task<string> GetShortenedUrl (string initial_url)
		{
			WebClient wc = new WebClient ();
			wc.Encoding = Encoding.UTF8;

			return Task<string>.Factory.StartNew (() => wc.DownloadString ("http://goo.gl/api/shorten?url=" + initial_url));
		}
	}
}
