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
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace Apachai
{
	public class Hasher
	{
		static MD5 md5 = MD5.Create ();

		static readonly int[] baseMap = new int[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };

		public static string Hash (Stream input)
		{
			input.Seek (0, SeekOrigin.Begin);
			byte[] hash = md5.ComputeHash (input);
			input.Seek (0, SeekOrigin.Begin);

			StringBuilder sb = new StringBuilder (hash.Length * 2);

			foreach (byte b in hash)
				sb.Append (b.ToString ("x"));

			return sb.ToString ();
		}

		public static string ComputeShortValue (long value)
		{
			int baseSize = baseMap.Length;
			long dividend = value;
			StringBuilder sb = new StringBuilder ();

			while (dividend > baseSize) {
				sb.Append (baseMap[dividend % baseSize]);
				dividend /= baseSize;
			}

			sb.Append (baseMap[dividend]);

			return sb.ToString ();
		}
	}
}
