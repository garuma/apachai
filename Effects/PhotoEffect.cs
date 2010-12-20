
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Apachai.Effects
{
	using Core;

	public static class PhotoEffect
	{
		public static void ApplyInvertAdjustment (string origPath, string destPath)
		{
			ApplyPhotoEffect<InvertColorsEffect> (origPath, destPath);
		}

		public static void ApplySepiaEffect (string origPath, string destPath)
		{
			ApplyPhotoEffect<SepiaEffect> (origPath, destPath);
		}

		public static void ApplyBlackAndWhiteEffect (string origPath, string destPath)
		{
			ApplyPhotoEffect<BlackAndWhiteEffect> (origPath, destPath);
		}

		static void ApplyPhotoEffect<T> (string originPath, string destPath) where T : BaseEffect, new ()
		{
			Bitmap bmp = new Bitmap (originPath);
			FastBitmap src = new FastBitmap (bmp);
			BaseEffect effect = new T ();
			FastBitmap result = new FastBitmap (new Bitmap (bmp.Width, bmp.Height, PixelFormat.Format32bppRgb));

			effect.RenderEffect (src, result);

			result.FinallySave (destPath);
		}
	}
}