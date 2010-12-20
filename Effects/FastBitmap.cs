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

namespace System.Drawing
{
    public struct PixelData
    {
        public byte B;
        public byte G;
        public byte R;
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

	    public unsafe PixelData* GetPointAddress (Point point)
	    {
		    return this[point.X, point.Y];
	    }

	    public unsafe PixelData* GetPointAddress (int x, int y)
	    {
		    return this[x, y];
	    }

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

        private void LockBitmap()
        {
            if (_locked) throw new InvalidOperationException("Already locked");

            Rectangle bounds = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);

            // Figure out the number of bytes in a row. This is rounded up to be a multiple 
            // of 4 bytes, since a scan line in an image must always be a multiple of 4 bytes
            // in length. 
            _width = bounds.Width * sizeof(PixelData);
            if (_width % 4 != 0) _width = 4 * (_width / 4 + 1);

            _bitmapData = _bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

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
