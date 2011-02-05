
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Apachai.Effects
{
	using Core;

	public static class PhotoEffect
	{
		public static bool ApplyTransformFromString (string transformation, string path)
		{
			if (string.IsNullOrEmpty (transformation))
				return false;

			switch (transformation) {
			case "eff_original":
				return false;
			case "eff_sepia":
				PhotoEffect.ApplySepiaEffect (path, path);
				break;
			case "eff_invert":
				PhotoEffect.ApplyInvertAdjustment (path, path);
				break;
			case "eff_blackwhite":
				PhotoEffect.ApplyBlackAndWhiteEffect (path, path);
				break;
			}

			return true;
		}

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

			src.Dispose ();
			result.Dispose ();
		}
	}
}