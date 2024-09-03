using System.Diagnostics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvaloniaPixelSnoop;

// Initialize Avalonia
// AvaloniaPixelSnoop is not really intended for use in
// CLI applications but here I do it for demonstration purposes
AppBuilder.Configure<Application>()
    .UsePlatformDetect()
    .SetupWithoutStarting();

Stopwatch stopwatch = new();

// You can also create a WriteableBitmap from an existing 
// Avalonia.Media.Imaging.Bitmap
// by saving your Bitmap to a Stream and then decoding it
using FileStream fs = File.OpenRead("path/to/image.png");
WriteableBitmap myBitmap = WriteableBitmap.Decode(fs);

// Basic Usage
stopwatch.Start();
using (BmpPixelSnoop snoop = new(myBitmap))
{
    // Get the color of a pixel
    Color pixelColor = snoop.GetPixel(10, 20);
    Console.WriteLine($"Pixel color at (10, 20): R={pixelColor.R}, G={pixelColor.G}, B={pixelColor.B}, A={pixelColor.A}");

    Color red = Color.FromRgb(255, 0, 0); // Color: Red

    // Set the color of a pixel
    snoop.SetPixel(15, 25, red);
}
stopwatch.Stop();
Console.WriteLine($"Basic - Time taken: {stopwatch.ElapsedMilliseconds}ms");

// Inverting the colors of an image
fs.Seek(0, SeekOrigin.Begin);
myBitmap = WriteableBitmap.Decode(fs);
stopwatch.Reset();
stopwatch.Start();
using (BmpPixelSnoop snoop = new(myBitmap))
{
    for (int y = 0; y < snoop.Height; y++)
    {
        for (int x = 0; x < snoop.Width; x++)
        {
            Color originalColor = snoop.GetPixel(x, y);
            Color invertedColor = Color.FromArgb(
                originalColor.A,
                (byte)(255 - originalColor.R),
                (byte)(255 - originalColor.G),
                (byte)(255 - originalColor.B)
            );
            snoop.SetPixel(x, y, invertedColor);
        }
    }
}
stopwatch.Stop();
Console.WriteLine($"Invert - Time taken: {stopwatch.ElapsedMilliseconds}ms");

myBitmap.Save("path/to/inverted.png");

// Creating a grayscale version of an image
fs.Seek(0, SeekOrigin.Begin);
myBitmap = WriteableBitmap.Decode(fs);
stopwatch.Reset();
stopwatch.Start();
using (BmpPixelSnoop snoop = new(myBitmap))
{
    for (int y = 0; y < snoop.Height; y++)
    {
        for (int x = 0; x < snoop.Width; x++)
        {
            Color originalColor = snoop.GetPixel(x, y);
            byte grayValue = (byte)(0.299 * originalColor.R + 0.587 * originalColor.G + 0.114 * originalColor.B);
            Color grayColor = Color.FromArgb(originalColor.A, grayValue, grayValue, grayValue);
            snoop.SetPixel(x, y, grayColor);
        }
    }
}
stopwatch.Stop();
Console.WriteLine($"Grayscale - Time taken: {stopwatch.ElapsedMilliseconds}ms");

myBitmap.Save("path/to/grayscale.png");

// Applying a simple blur effect
fs.Seek(0, SeekOrigin.Begin);
myBitmap = WriteableBitmap.Decode(fs);
WriteableBitmap blurredBitmap = new(
    myBitmap.PixelSize,
    myBitmap.Dpi,
    myBitmap.Format,
    myBitmap.AlphaFormat
    );

stopwatch.Reset();
stopwatch.Start();
using (BmpPixelSnoop sourceSnoop = new(myBitmap))
using (BmpPixelSnoop destSnoop = new(blurredBitmap))
{
    for (int y = 1; y < sourceSnoop.Height - 1; y++)
    {
        for (int x = 1; x < sourceSnoop.Width - 1; x++)
        {
            int totalR = 0, totalG = 0, totalB = 0;

            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    Color neighborColor = sourceSnoop.GetPixel(x + offsetX, y + offsetY);
                    totalR += neighborColor.R;
                    totalG += neighborColor.G;
                    totalB += neighborColor.B;
                }
            }

            Color averageColor = Color.FromRgb(
                (byte)(totalR / 9),
                (byte)(totalG / 9),
                (byte)(totalB / 9)
            );

            destSnoop.SetPixel(x, y, averageColor);
        }
    }
}
stopwatch.Stop();
Console.WriteLine($"Blur - Time taken: {stopwatch.ElapsedMilliseconds}ms");

blurredBitmap.Save("path/to/blurred.png");

// Crop an image
PixelRect sourceRect = new(
    myBitmap.PixelSize.Width / 4,
    myBitmap.PixelSize.Height / 4,
    myBitmap.PixelSize.Width / 2,
    myBitmap.PixelSize.Height / 2
    );
PixelRect destRect = new(
    0, 0,
    sourceRect.Width,
    sourceRect.Height
    );
WriteableBitmap croppedBitmap = new(
    new(destRect.Width, destRect.Height),
    myBitmap.Dpi,
    myBitmap.Format,
    myBitmap.AlphaFormat
    );

stopwatch.Reset();
stopwatch.Start();
using (BmpPixelSnoop sourceSnoop = new(myBitmap))
using (BmpPixelSnoop destSnoop = new(croppedBitmap))
{
    for (int x = 0; x < destRect.Width; x++)
    {
        for (int y = 0; y < destRect.Height; y++)
        {
            int sourceX = sourceRect.X + x;
            int sourceY = sourceRect.Y + y;
            int destX = destRect.X + x;
            int destY = destRect.Y + y;

            // Ensure we're not reading outside the source image bounds
            // and not writing outside the destination image bounds
            if (sourceX < myBitmap.Size.Width && sourceY < myBitmap.Size.Height &&
                destX < croppedBitmap.Size.Width && destY < croppedBitmap.Size.Height)
            {
                Color pixelColor = sourceSnoop.GetPixel(sourceX, sourceY);
                destSnoop.SetPixel(destX, destY, pixelColor);
            }
            else if (destX < croppedBitmap.Size.Width && destY < croppedBitmap.Size.Height)
            {
                // Set a color (default: white) for out-of-bounds pixels
                // Only if we're within the destination bounds
                destSnoop.SetPixel(x, y, Color.FromRgb(255, 255, 255));
            }
        }
    }
}
stopwatch.Stop();
Console.WriteLine($"Crop - Time taken: {stopwatch.ElapsedMilliseconds}ms");

croppedBitmap.Save("path/to/cropped.png");