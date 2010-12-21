//--------------------------------------------------------------------------
// 
//  Copyright (c) Microsoft Corporation.  All rights reserved. 
// 
//  File: FastBitmap.cs
//
//--------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using Apachai.Effects.Core;

namespace System.Drawing
{
	[StructLayout(LayoutKind.Explicit)]
    public struct PixelData
    {
        [FieldOffset(0)] 
        public byte B;

        [FieldOffset(1)] 
        public byte G;

        [FieldOffset(2)] 
        public byte R;

        /// <summary>
        /// Lets you change B, G, R, and A at the same time.
        /// </summary>
        [FieldOffset(0)] 
        public uint Bgra;

	    public static PixelData FromBgra (byte b, byte g, byte r, byte a)
	    {
		    PixelData d = new PixelData ();
		    d.B = b;
		    d.G = g;
		    d.R = r;
		    return d;
	    }

	    public static PixelData FromBgr (byte b, byte g, byte r)
	    {
		    return FromBgra (b, g, r, 255);
	    }

	    public static PixelData FromUInt32 (uint value)
	    {
		    PixelData d = new PixelData ();
		    d.Bgra = value;
		    return d;
	    }

	    public unsafe byte this[int channel]
        {
	        get {
                if (channel < 0 || channel > 2)
                    throw new ArgumentOutOfRangeException("channel", channel, "valid range is [0,3]");

                fixed (byte *p = &B)
                    return p[channel];
            }
            set {
                if (channel < 0 || channel > 2)
                    throw new ArgumentOutOfRangeException("channel", channel, "valid range is [0,3]");

                fixed (byte *p = &B)
                    p[channel] = value;
            }
        }

        /// <summary>
        /// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
        /// </summary>
        /// <returns>A value in the range 0 to 1 inclusive.</returns>
        public double GetIntensity()
        {
            return ((0.114 * (double)B) + (0.587 * (double)G) + (0.299 * (double)R)) / 255.0;
        }

        /// <summary>
        /// Gets the luminance intensity of the pixel based on the values of the red, green, and blue components. Alpha is ignored.
        /// </summary>
        /// <returns>A value in the range 0 to 255 inclusive.</returns>
        public byte GetIntensityByte()
        {
            return (byte)((7471 * B + 38470 * G + 19595 * R) >> 16);
        }

        public static PixelData FromBgraClamped(int b, int g, int r, int a)
        {
            return FromBgra(
                Utility.ClampToByte(b),
                Utility.ClampToByte(g),
                Utility.ClampToByte(r),
                255);
        }


	    public static PixelData Black { get { return PixelData.FromBgra (0, 0, 0, 255); } }
	    public static PixelData Blue { get { return PixelData.FromBgra (255, 0, 0, 255); } }
	    public static PixelData Cyan { get { return PixelData.FromBgra (255, 255, 0, 255); } }
	    public static PixelData Green { get { return PixelData.FromBgra (0, 128, 0, 255); } }
	    public static PixelData Magenta { get { return PixelData.FromBgra (255, 0, 255, 255); } }
	    public static PixelData Red { get { return PixelData.FromBgra (0, 0, 255, 255); } }
	    public static PixelData White { get { return PixelData.FromBgra (255, 255, 255, 255); } }
	    public static PixelData Yellow { get { return PixelData.FromBgra (0, 255, 255, 255); } }

    }

    public unsafe class FastBitmap : IDisposable
    {
        private Bitmap _bitmap;
        private int _width;
        private BitmapData _bitmapData = null;
        private byte* _pBase = null;
        private PixelData* _pInitPixel = null;
        private Point _size;
        private bool _locked = false;

        public FastBitmap (Bitmap bmp)
        {
            if (bmp == null) throw new ArgumentNullException("bitmap");

            _bitmap = bmp;
            _size = new Point(bmp.Width, bmp.Height);

            LockBitmap();
        }

        public PixelData* GetInitialPixelForRow(int rowNumber)
        {
            return (PixelData*)(_pBase + rowNumber * _width);
        }

        public PixelData* this[int x, int y]
        {
            get { return (PixelData*)(_pBase + y * _width + x * sizeof(PixelData)); }
        }

	    public PixelData* DataPtr {
		    get {
			    return (PixelData*)_pBase;
		    }
	    }

        public Color GetColor(int x, int y)
        {
            PixelData* data = this[x, y];
            return Color.FromArgb(data->R, data->G, data->B);
        }

        public void SetColor(int x, int y, Color c)
        {
            PixelData* data = this[x, y];
            data->R = c.R;
            data->G = c.G;
            data->B = c.B;
        }

	    /*public unsafe PixelData* GetPointAddress (Point point)
	    {
		    return this[point.X, point.Y];
	    }

	    public unsafe PixelData* GetPointAddress (int x, int y)
	    {
		    return this[x, y];
	    }*/

	    public Rectangle GetBounds ()
	    {
		    return new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);
	    }

	    public int Width {
		    get {
			    return _bitmap.Width;
		    }
	    }

	    public int Height {
		    get {
			    return _bitmap.Height;
		    }
	    }

	    public void FinallySave (string path)
	    {
		    UnlockBitmap ();
		    _bitmap.Save (path, ImageFormat.Jpeg);
	    }

        private void LockBitmap()
        {
            if (_locked) throw new InvalidOperationException("Already locked");

            Rectangle bounds = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);

            // Figure out the number of bytes in a row. This is rounded up to be a multiple 
            // of 4 bytes, since a scan line in an image must always be a multiple of 4 bytes
            // in length. 
            _width = bounds.Width * sizeof(PixelData);
            if (_width % 4 != 0) _width = 4 * (_width / 4 + 1);

            _bitmapData = _bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);

            _pBase = (byte*)_bitmapData.Scan0.ToPointer();
            _locked = true;
        }

        private void InitCurrentPixel()
        {
            _pInitPixel = (PixelData*)_pBase;
        }

        private void UnlockBitmap()
        {
            if (!_locked) throw new InvalidOperationException("Not currently locked");

            _bitmap.UnlockBits(_bitmapData);
            _bitmapData = null;
            _pBase = null;
            _locked = false;
        }

        public void Dispose()
        {
            if (_locked) UnlockBitmap();
        }
    }
}
