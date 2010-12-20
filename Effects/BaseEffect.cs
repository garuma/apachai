// 
// BaseEffect.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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

using System;
using System.Drawing;

namespace Apachai.Effects.Core
{
	public abstract class BaseEffect
	{
		public abstract string Text { get; }
		public EffectData EffectData { get; protected set; }
		public virtual EffectAdjustment EffectOrAdjustment { get { return EffectAdjustment.Effect; } }

		#region Overrideable Render Methods
		public virtual void RenderEffect (FastBitmap src, FastBitmap dst, Rectangle[] rois)
		{
			foreach (var rect in rois)
				RenderEffect (src, dst, rect);
		}

		protected unsafe virtual void RenderEffect (FastBitmap src, FastBitmap dst, Rectangle roi)
		{
			PixelData* src_data_ptr = (PixelData*)src.DataPtr;
			int src_width = src.Width;
			PixelData* dst_data_ptr = (PixelData*)dst.DataPtr;
			int dst_width = dst.Width;

			for (int y = roi.Y; y < roi.Bottom; ++y) {
				PixelData* srcPtr = src.GetPointAddressUnchecked (src_data_ptr, src_width, roi.X, y);
				PixelData* dstPtr = dst.GetPointAddressUnchecked (dst_data_ptr, dst_width, roi.X, y);
				RenderLine (srcPtr, dstPtr, roi.Width);
			}
		}

		protected unsafe virtual void RenderLine (PixelData* src, PixelData* dst, int length)
		{
			while (length > 0) {
				*dst = RenderPixel (*src);
				++dst;
				++src;
				--length;
			}
		}

		protected virtual PixelData RenderPixel (PixelData color)
		{
			return color;
		}
		#endregion
				
		// Effects that have any configuration state which is changed
		// during live preview, and this this state is stored in
		// non-value-type fields should override this method.
		// Generally this state should be stored in the effect data
        // class, not in the effect.
		public virtual BaseEffect Clone ()
		{
			var effect = (BaseEffect) this.MemberwiseClone ();
			if (effect.EffectData != null)
				effect.EffectData = EffectData.Clone ();
			return effect;
		}		
	}
	
	public abstract class EffectData
	{
		// EffectData classes that have any state stored in non-value-type
		// fields must override this method, and clone those members.
		public virtual EffectData Clone ()
		{
			return (EffectData) this.MemberwiseClone ();
		}

		public virtual bool IsDefault { get { return false; } }
	}
}
