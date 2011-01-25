
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Apachai.Effects
{
	using Core;

	public static class Rotationner
	{
		public static bool RotationatePathIfNeeded (string path, RotateFlipType type)
		{
			if (type == RotateFlipType.RotateNoneFlipNone)
				return false;

			var bmp = Bitmap.FromFile (path);
			bmp.RotateFlip (type);
			bmp.Save (path);

			return true;
		}
	}
}