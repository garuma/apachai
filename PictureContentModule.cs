//
// Copyright (c) 2011 Jérémie "garuma" Laval
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
using System.Drawing;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Manos;

namespace Apachai
{
	public class PictureContentModule : ManosModule
	{
		readonly string expires;
		readonly string cacheControl;
		readonly ConcurrentDictionary<string, string> etagCache;

		const string smallCache = "Small";
		const int smallMax = 600;

		public PictureContentModule () : this (TimeSpan.FromDays (60))
		{
		}

		public PictureContentModule (TimeSpan expireTime)
		{
			this.expires = (DateTime.Now + expireTime).ToString ("R");
			this.cacheControl = "max-age=" + ((int)expireTime.TotalSeconds).ToString ();
			this.etagCache = new ConcurrentDictionary<string, string> ();

			Get (".*", Content);
		}

		public void Content (IManosContext ctx)
		{
			string path = ctx.Request.Path;

			if (path.StartsWith ("/"))
				path = path.Substring (1);

			if (string.Equals (ctx.Request.QueryData.Get ("v"), "small", StringComparison.Ordinal))
				path = EnsureSmallAvailable (path);

			string etag, fileEtag;
			if (ctx.Request.Headers.TryGetNormalizedValue ("If-None-Match", out etag)
			    && etagCache.TryGetValue (path, out fileEtag)
			    && fileEtag.Equals (etag, StringComparison.Ordinal)) {

				ctx.Response.StatusCode = 304;
				ctx.Response.End ();
				return;
			}

			if (File.Exists (path)) {
				ctx.Response.Headers.SetNormalizedHeader ("Content-Type", "image/jpeg");
				ctx.Response.Headers.SetNormalizedHeader ("Expires", expires);
				ctx.Response.Headers.SetNormalizedHeader ("Cache-Control", cacheControl);
				if (!etagCache.ContainsKey (path))
					etagCache[path] = GetEtagFromFile (path);
				ctx.Response.Headers.SetNormalizedHeader ("ETag", etagCache[path]);

				ctx.Response.SendFile (path);
			} else
				ctx.Response.StatusCode = 404;

			ctx.Response.End ();
		}

		static string GetEtagFromFile (string path)
		{
			return File.GetLastWriteTimeUtc (path).ToString ("s");
		}

		static string EnsureSmallAvailable (string path)
		{
			if (!Directory.Exists (smallCache))
				Directory.CreateDirectory (smallCache);

			var newPath = Path.Combine (smallCache, Path.GetFileName (path));
			if (File.Exists (newPath))
				return newPath;

			var original = Bitmap.FromFile (path);
			int width = 0, height = 0;
			if (original.Width >= original.Height) {
				width = smallMax;
				height = original.Height * smallMax / original.Width;
			} else {
				height = smallMax;
				width = original.Width * smallMax / original.Height;
			}

			var newPic = new Bitmap (original, width, height);
			newPic.Save (newPath, System.Drawing.Imaging.ImageFormat.Jpeg);

			return newPath;
		}
	}
}

