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
using System.Globalization;

namespace Apachai
{
	public class TagLibMetadata
	{
		TagLib.File file;
		string id;
		readonly string basePath;

		public TagLibMetadata (string basePath, string id)
		{
			this.id = id;
			this.basePath = basePath;
		}

		public bool IsValid {
			get {
				if (file != null)
					return true;
				string path = Path.Combine (basePath, id);

				if (!File.Exists (path))
					return false;

				try {
					file = TagLib.File.Create(path, "image/jpeg", TagLib.ReadStyle.Average);
				} catch (Exception e) {
					Console.WriteLine (e.ToString ());
					return false;
				}

				return true;
			}
		}

		public Tuple<double, double> GeoCoordinates {
			get {
				var image = file as TagLib.Image.File;

				if (image.ImageTag.Longitude == null || image.ImageTag.Latitude == null)
					return null;

				return Tuple.Create ((double)image.ImageTag.Latitude, (double)image.ImageTag.Longitude);
			}
		}

		public void FillUp (JsonStringDictionary dict)
		{
			var image = file as TagLib.Image.File;

			if (image.Properties != null) {
				CheckAndAdd (dict, "Width: ", image.Properties.PhotoWidth);
				CheckAndAdd (dict, "Height: ", image.Properties.PhotoHeight);
				CheckAndAdd (dict, "Type: ", image.Properties.Description);
			}
			
			if (image.ImageTag != null) {
				CheckAndAdd (dict, "Comment: ", image.ImageTag.Comment);
				CheckAndAdd (dict, "Rating: ", image.ImageTag.Rating);
				CheckAndAdd (dict, "Date: ", image.ImageTag.DateTime);
				CheckAndAdd (dict, "Altitude: ", image.ImageTag.Altitude);
				CheckAndAdd (dict, "Orientation: ", image.ImageTag.Orientation);
				CheckAndAdd (dict, "Exposure time: ", image.ImageTag.ExposureTime);
				CheckAndAdd (dict, "FNumber: ", image.ImageTag.FNumber);
				CheckAndAdd (dict, "ISO speed: ", image.ImageTag.ISOSpeedRatings);
				CheckAndAdd (dict, "Focal length: ", image.ImageTag.FocalLength);
				CheckAndAdd (dict, "Software: ", image.ImageTag.Software);
				CheckAndAdd (dict, "Make: ", image.ImageTag.Make);
				CheckAndAdd (dict, "Model: ", image.ImageTag.Model);
			}
		}

		static void CheckAndAdd<TValue> (JsonStringDictionary dict, string key, TValue value)
		{
			if (value == null || string.IsNullOrEmpty (key))
				return;

			IFormattable f = value as IFormattable;
			string sValue = (f != null) ? f.ToString (null, CultureInfo.InvariantCulture) : value.ToString ();
			sValue = sValue.Trim ();

			if (!string.IsNullOrEmpty (sValue) && !IsWhiteSpaces (sValue))
				dict[key] = sValue;
		}

		static bool IsWhiteSpaces (string str)
		{
			return str.All (char.IsWhiteSpace);
		}
	}
}