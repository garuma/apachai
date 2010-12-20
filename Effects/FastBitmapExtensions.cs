
using System;
using System.Drawing;

namespace Apachai.Effects.Core
{
	public static class FastBitmapExtensions
	{
		public static unsafe PixelData* GetPointAddressUnchecked (this FastBitmap surf, int x, int y)
		{
			PixelData* dstPtr = (PixelData*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return dstPtr;
		}

		public static unsafe PixelData* GetPointAddressUnchecked (this FastBitmap surf, PixelData* surfDataPtr, int surfWidth, int x, int y)
		{
			PixelData* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

			return dstPtr;
		}

		public static unsafe PixelData GetPointUnchecked (this FastBitmap surf, int x, int y)
		{
			PixelData* dstPtr = (PixelData*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return *dstPtr;
		}

		// This isn't really an extension method, since it doesn't use
		// the passed in argument, but it's nice to have the same calling
		// convention as the uncached version.  If you can use this one
		// over the other, it is much faster in tight loops (like effects).
		public static unsafe PixelData GetPointUnchecked (this FastBitmap surf, PixelData* surfDataPtr, int surfWidth, int x, int y)
		{
			PixelData* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

			return *dstPtr;
		}

		public static unsafe PixelData* GetRowAddressUnchecked (this FastBitmap surf, int y)
		{
			PixelData* dstPtr = (PixelData*)surf.DataPtr;

			dstPtr += y * surf.Width;

			return dstPtr;
		}

		public static unsafe PixelData* GetRowAddressUnchecked (this FastBitmap surf, PixelData* surfDataPtr, int surfWidth, int y)
		{
			PixelData* dstPtr = surfDataPtr;

			dstPtr += y * surfWidth;

			return dstPtr;
		}

		public static unsafe PixelData* GetPointAddress (this FastBitmap surf, int x, int y)
		{
			if (x < 0 || x >= surf.Width)
				throw new ArgumentOutOfRangeException ("x", "Out of bounds: x=" + x.ToString ());

			return surf.GetPointAddressUnchecked (x, y);
		}

		public static unsafe PixelData* GetPointAddress (this FastBitmap surf, Point point)
		{
			return surf.GetPointAddress (point.X, point.Y);
		}
	}
}