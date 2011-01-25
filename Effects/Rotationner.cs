
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Apachai.Effects
{
	using Core;

	public static class Rotationner
	{
		public static void RotationatePathIfNeeded (string path, RotateFlipType type)
		{
			if (type == RotateFlipType.RotateNoneFlipNone)
				return;

			var bmp = Bitmap.FromFile (path);
			bmp.RotateFlip (type);
			bmp.Save (path);
		}
	}
}