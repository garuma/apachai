/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;
using System.Drawing;

namespace Apachai.Effects.Core
{
	/// <summary>
	/// Defines a way to operate on a pixel, or a region of pixels, in a unary fashion.
	/// That is, it is a simple function F that takes one parameter and returns a
	/// result of the form: d = F(c)
	/// </summary>
	[Serializable]
	public unsafe abstract class UnaryPixelOp : PixelOp
	{
		public UnaryPixelOp ()
		{
		}

		public abstract PixelData Apply (PixelData color);

		public unsafe override void Apply (PixelData* dst, PixelData* src, int length)
		{
			unsafe {
				while (length > 0) {
					*dst = Apply (*src);
					++dst;
					++src;
					--length;
				}
			}
		}

		public unsafe virtual void Apply (PixelData* ptr, int length)
		{
			unsafe {
				while (length > 0) {
					*ptr = Apply (*ptr);
					++ptr;
					--length;
				}
			}
		}

		private unsafe void ApplyRectangle (FastBitmap surface, Rectangle rect)
		{
			for (int y = rect.Left; y < rect.Bottom; ++y) {
				PixelData* ptr = surface.GetPointAddress (rect.Left, y);
				Apply (ptr, rect.Width);
			}
		}

		public void Apply (FastBitmap surface, Rectangle[] roi, int startIndex, int length)
		{
			Rectangle regionBounds = Utility.GetRegionBounds (roi, startIndex, length);

			if (regionBounds != Rectangle.Intersect (surface.GetBounds (), regionBounds))
				throw new ArgumentOutOfRangeException ("roi", "Region is out of bounds");

			unsafe {
				for (int x = startIndex; x < startIndex + length; ++x)
					ApplyRectangle (surface, roi[x]);
			}
		}

		public void Apply (FastBitmap surface, Rectangle[] roi)
		{
			Apply (surface, roi, 0, roi.Length);
		}

		public unsafe void Apply (FastBitmap surface, Rectangle roi)
		{
			ApplyRectangle (surface, roi);
		}

		public override void Apply (FastBitmap dst, Point dstOffset, FastBitmap src, Point srcOffset, int scanLength)
		{
			Apply (dst.GetPointAddress (dstOffset), src.GetPointAddress (srcOffset), scanLength);
		}
		
		public void Apply (FastBitmap dst, FastBitmap src, Rectangle roi)
		{
			PixelData* src_data_ptr = (PixelData*)src.DataPtr;
			int src_width = src.Width;
			PixelData* dst_data_ptr = (PixelData*)dst.DataPtr;
			int dst_width = dst.Width;

			for (int y = roi.Y; y < roi.Bottom; ++y) {
				PixelData* dstPtr = dst.GetPointAddressUnchecked (dst_data_ptr, dst_width, roi.X, y);
				PixelData* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, roi.X, y);
				Apply (dstPtr, srcPtr, roi.Width);
			}
		}

		public void Apply (FastBitmap dst, FastBitmap src, Rectangle[] rois)
		{
			foreach (Rectangle roi in rois)
				Apply (dst, src, roi);
		}
	}
}
