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
	public class SepiaEffect : BaseEffect
	{
		UnaryPixelOp desat = new UnaryPixelOps.Desaturate ();
		UnaryPixelOp level = new UnaryPixelOps.Desaturate ();

		public override string Text {
			get { return "Sepia"; }
		}
		
		public override EffectAdjustment EffectOrAdjustment {
			get { return EffectAdjustment.Adjustment; }
		}

		public SepiaEffect ()
		{
			desat = new UnaryPixelOps.Desaturate ();
			level = new UnaryPixelOps.Level (
				PixelData.Black,
				PixelData.White,
				new float[] { 1.2f, 1.0f, 0.8f },
				PixelData.Black,
				PixelData.White);
		}

		public override void RenderEffect (FastBitmap src, FastBitmap dest, Rectangle[] rois)
		{
			desat.Apply (dest, src, rois);
			level.Apply (dest, dest, rois);
		}
	}
}
