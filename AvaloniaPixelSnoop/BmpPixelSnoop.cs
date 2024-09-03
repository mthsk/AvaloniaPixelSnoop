//   Copyright 2019 Kevin Godden
//   Copyright 2024 Matheus Skau
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.

using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AvaloniaPixelSnoop
{
    /// <summary>
    /// Wraps an Avalonia.Media.Imaging.WriteableBitmap and provides fast
    /// GetPixel() and SetPixel() functions for pixel access.
    ///
    /// NB While the snoop object is in scope the wrapped
    /// bitmap object is locked and cannot be used
    /// as normal.  Once you have finished snooping
    /// on a bitmap object, dispose of the snooper to
    /// unlock the bitmap and gain normal access to
    /// it again, it is best to employ the 'using' keyword
    /// to effectivly manage the snooper's scope as follows:
    ///
    ///
    /// using (var snoop = new BmpPixelSnoop(myBitmap))
    /// {
    ///
    ///     // Snoop away!
    ///     var pixel = snoop.GetPixel(0, 0);
    ///
    /// } // Snoop goes out of scope here and bitmap is unlocked
    ///
    /// </summary>
    public unsafe class BmpPixelSnoop : IDisposable
    {
        // A reference to the bitmap to be wrapped
        private readonly WriteableBitmap wrappedBitmap;

        // The bitmap's data (once it has been locked)
        private ILockedFramebuffer data = null;

        // Pointer to the first pixel
        private readonly byte* scan0;

        // Number of bytes per pixel
        private readonly int depth;

        // Number of bytes in an image row
        private readonly int stride;

        // The bitmap's width
        private readonly int width;

        // The bitmap's height
        private readonly int height;

        /// <summary>
        /// Constructs a BmpPixelSnoop object, the bitmap
        /// object to be wraped is passed as a parameter.
        /// </summary>
        /// <param name="bitmap">The bitmap to snoop</param>
        public BmpPixelSnoop(WriteableBitmap bitmap)
        {
            wrappedBitmap = bitmap ?? throw new ArgumentException("Bitmap parameter cannot be null", nameof(bitmap));

            // Currently works only for: PixelFormat.Bgra8888
            if (wrappedBitmap.Format != PixelFormat.Bgra8888)
                throw new ArgumentException("Only PixelFormat.Bgra8888 is supported", nameof(wrappedBitmap));

            // Record the width & height
            width = wrappedBitmap.PixelSize.Width;
            height = wrappedBitmap.PixelSize.Height;

            // So now we need to lock the bitmap so that we can gain access
            // to it's raw pixel data.  It will be unlocked when this snoop is
            // disposed.
            try
            {
                data = wrappedBitmap.Lock();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not lock bitmap, is it already being snooped somewhere else?", ex);
            }

            // Calculate number of bytes per pixel
            depth = wrappedBitmap.Format.Value.BitsPerPixel / 8; // bits per channel

            // Get pointer to first pixel
            scan0 = (byte*)data.Address.ToPointer();

            // Get the number of bytes in an image row
            // this will be used when determining a pixel's
            // memory address.
            stride = data.RowBytes;
        }

        /// <summary>
        /// Disposes BmpPixelSnoop object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes BmpPixelSnoop object, we unlock
        /// the wrapped bitmap.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                data?.Dispose();
            }
            // free native resources if there are any.
        }

        /// <summary>
        /// Calculate the pointer to a pixel at (x, x)
        /// </summary>
        /// <param name="x">The pixel's x coordinate</param>
        /// <param name="y">The pixel's y coordinate</param>
        /// <returns>A byte* pointer to the pixel's data</returns>
        private byte* PixelPointer(int x, int y)
        {
            return scan0 + y * stride + x * depth;
        }

        /// <summary>
        /// Snoop's implemetation of GetPixel() which is similar to
        /// System.Drawing.Bitmap's GetPixel() but should be faster.
        /// </summary>
        /// <param name="x">The pixel's x coordinate</param>
        /// <param name="y">The pixel's y coordinate</param>
        /// <returns>The pixel's colour</returns>
        public Color GetPixel(int x, int y)
        {
            // Better do the 'decent thing' and bounds check x & y
            if (x < 0 || y < 0 || x >= width || y >= height)
                throw new ArgumentException("x or y coordinate is out of range");

            byte b, g, r, a;

            // Get a pointer to this pixel
            byte* p = PixelPointer(x, y);

            // Pull out its colour data
            b = *p++;
            g = *p++;
            r = *p++;
            a = *p;

            // And return a color value for it (this is quite slow
            // but allows us to look like Bitmap.GetPixel())
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// Sets the passed colour to the pixel at (x, y)
        /// </summary>
        /// <param name="x">The pixel's x coordinate</param>
        /// <param name="y">The pixel's y coordinate</param>
        /// <param name="col">The value to be assigned to the pixel</param>
        public void SetPixel(int x, int y, Color col)
        {
            // Better do the 'decent thing' and bounds check x & y
            if (x < 0 || y < 0 || x >= width || y >= height)
                throw new ArgumentException("x or y coordinate is out of range");

            // Get a pointer to this pixel
            byte* p = PixelPointer(x, y);

            // Set the data
            *p++ = col.B;
            *p++ = col.G;
            *p++ = col.R;
            *p = col.A;
        }

        /// <summary>
        /// The bitmap's width
        /// </summary>
        public int Width { get { return width; } }

        // The bitmap's height
        public int Height { get { return height; } }
    }
}
