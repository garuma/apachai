/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Apachai.Effects by: Jonathan Pobst <monkey@jpobst.com>                      //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;
using Apachai.Effects.Core;

namespace Apachai.Effects
{
	public class InvertColorsEffect : BaseEffect
	{
		UnaryPixelOp op = new UnaryPixelOps.Invert ();

		public override string Text {
			get { return "Invert Colors"; }
		}

		public override EffectAdjustment EffectOrAdjustment {
			get { return EffectAdjustment.Adjustment; }
		}
				
		public override void RenderEffect (FastBitmap src, FastBitmap dest, Rectangle[] rois)
		{
			op.Apply (dest, src, rois);
		}
	}
}
