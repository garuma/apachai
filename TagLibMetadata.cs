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
using System.Drawing;
using TagLib.Image;

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

				if (!System.IO.File.Exists (path))
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
			
			if (image.ImageTag != null) {
				CheckAndAdd (dict, "Comment", image.ImageTag.Comment);
				CheckAndAdd (dict, "Rating", image.ImageTag.Rating);
				CheckAndAdd (dict, "Date", image.ImageTag.DateTime);
				CheckAndAdd (dict, "Altitude", image.ImageTag.Altitude);
				CheckAndAdd (dict, "Exposure time", image.ImageTag.ExposureTime);
				CheckAndAdd (dict, "FNumber", image.ImageTag.FNumber);
				CheckAndAdd (dict, "ISO speed", image.ImageTag.ISOSpeedRatings);
				CheckAndAdd (dict, "Focal length", image.ImageTag.FocalLength);
				CheckAndAdd (dict, "Camera", image.ImageTag.Model);
			}
		}

		public void Close ()
		{
			if (file != null) {
				file.Dispose ();
				file = null;
			}
		}

		public static TagLib.Image.File ApplyNeededRotation (string path)
		{
			TagLib.Image.File file;
			var rotation = GetNeededRotation (path, out file);
			file.EnsureAvailableTags ();

			if (!global::Apachai.Effects.Rotationner.RotationatePathIfNeeded (path, rotation))
				return file;

			RestoreMetadata (path, file);

			return file;
		}

		public static void RestoreMetadata (string path, TagLib.Image.File metadataSave)
		{
			var newFile = (TagLib.Image.File)TagLib.File.Create (path, "image/jpeg", TagLib.ReadStyle.Average);
			newFile.CopyFrom (metadataSave);
			newFile.ImageTag.Orientation = ImageOrientation.TopLeft;
			newFile.Save ();
		}

		public static RotateFlipType GetNeededRotation (string path, out TagLib.Image.File file)
		{
			file = null;
			var taglib = new TagLibMetadata (path, string.Empty);

			if (!taglib.IsValid)
				return RotateFlipType.RotateNoneFlipNone;

			file = taglib.file as TagLib.Image.File;
			var orientation = file.ImageTag.Orientation;

			switch (orientation) {
			case ImageOrientation.TopRight:
				return RotateFlipType.RotateNoneFlipY;
			case ImageOrientation.BottomRight:
				return RotateFlipType.Rotate180FlipNone;
			case ImageOrientation.BottomLeft:
				return RotateFlipType.RotateNoneFlipX;
			case ImageOrientation.LeftTop:
				return RotateFlipType.Rotate90FlipY;
			case ImageOrientation.RightTop:
				return RotateFlipType.Rotate90FlipNone;
			case ImageOrientation.RightBottom:
				return RotateFlipType.Rotate270FlipY;
			case ImageOrientation.LeftBottom:
				return RotateFlipType.Rotate270FlipNone;
			default:
				return RotateFlipType.RotateNoneFlipNone;
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