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

using Manos;


//
//  This the default StaticContentModule that comes with all Manos apps
//  if you do not wish to serve any static content with Manos you can
//  remove its route handler from <YourApp>.cs's constructor and delete
//  this file.
//
//  All Content placed on the Content/ folder should be handled by this
//  module.
//

namespace Apachai {

	public class StaticContentModule : ManosModule {

		public StaticContentModule ()
		{
			Get (".*", Content);
		}

		public static void Content (IManosContext ctx)
		{
			string path = ctx.Request.LocalPath;

			if (path.StartsWith ("/"))
				path = path.Substring (1);

			if (File.Exists (path))
				if (path.IndexOf ("/img/") != -1)
					ctx.Response.Headers.SetNormalizedHeader ("Content-Type", "image/jpeg");
				else
					ctx.Response.Headers.SetNormalizedHeader ("Content-Type", ManosMimeTypes.GetMimeType (path));
				ctx.Response.SendFile (path);
			else
				ctx.Response.StatusCode = 404;
			ctx.Response.End ();
		}
	}
}

