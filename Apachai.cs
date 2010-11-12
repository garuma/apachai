
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;

using Manos;
using Manos.Http;

namespace Apachai 
{
	public class Apachai : ManosApp 
	{
		static MD5 md5sum = MD5.Create ();

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

			if (string.IsNullOrEmpty (twittertext) || req.Files.Count == 0 || CheckImageType (req.Files.Keys.First ())) {
				ctx.Response.Redirect ("/Post?error=1");
				return;
			}

			MemoryStream buffer = new MemoryStream ();
			req.Files.Values.First ().Contents.CopyTo (buffer);
			
			byte[] hash = md5sum.ComputeHash (buffer);
			string filename = Convert.ToBase64String (hash);
			buffer.Seek (0, SeekOrigin.Begin);
			using (FileStream fs = File.OpenWrite ("Content/img/" + filename))
				buffer.CopyTo (fs);
			
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
			if ((json = Cache[id] as string) != null) {
				ctx.Response.WriteLine (json);
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
			
			var dict = new Dictionary<object, object> ();

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

			Console.WriteLine ("Got the following in the dict:");
			foreach (var kvp in dict)
				Console.WriteLine ("{0}, {1}", kvp.Key.ToString (), kvp.Value.ToString ());

			json = JSON.JsonEncode (dict);
			if (json == null) {
				ctx.Response.StatusCode = 500;
				return;
			}

			Cache[id] = json;
			Console.WriteLine ("Returning: " + json);

			ctx.Response.WriteLine (json);
			ctx.Response.Finish ();
		}
		
		static bool CheckImageType (string mime)
		{
			return string.IsNullOrEmpty (mime) || (!mime.Equals ("image/jpg", StringComparison.Ordinal) && !mime.Equals ("image/png", StringComparison.Ordinal));
		}

		static void CheckAndAdd<TValue> (Dictionary<object, object> dict, string key, TValue value)
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
	}
}
