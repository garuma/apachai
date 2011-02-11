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
using System.Text;

namespace Apachai
{
	public class JsonStringDictionary
	{
		StringBuilder sb = new StringBuilder ();
		bool finished;

		public JsonStringDictionary ()
		{
			sb.Append ('{');
		}

		public string Json {
			get {
				if (!finished) {
					sb[sb.Length - 1] = '}';
					finished = true;
				}
				
				return sb.ToString ();
			}
		}

		public string this[string key] {
			set {
				if (finished)
					return;

				sb.Append ('"');
				sb.Append (key);
				sb.Append ('"');
				sb.Append (':');
				sb.Append ('"');
				sb.Append (Escape (value));
				sb.Append ('"');
				sb.Append (',');
			}
		}

		static string Escape (string foo)
		{
			return foo.Replace ("\"", "\\\"").Replace ("\n", "\\n").Replace ("\r", "\\r").Trim ();
		}
	}
}