using System;
using Mono.Cairo;
using Pinta.Effects;

namespace Apachai
{
	public static class PhotoEffect
	{
		public static void ApplyInvertAdjustment (string origPath, string destPath)
		{
			ApplyPhotoEffect (() => new InvertColorsEffect (), origPath, destPath);
		}

		public static void ApplySepiaEffect (string origPath, string destPath)
		{
			ApplyPhotoEffect (() => new SepiaEffect (), origPath, destPath);
		}

		static void ApplyPhotoEffect (Func<BaseEffect> effect, string originPath, string destPath)
		{
			
		}
	}
}