# AvaloniaPixelSnoop
Fast *GetPixel()* and *SetPixel()* functionality for [AvaloniaUI](https://github.com/AvaloniaUI/Avalonia) Bitmaps

AvaloniaPixelSnoop is a fork of [DotNetPixelSnoop](https://github.com/kgodden/DotNetPixelSnoop).  This project introduces a class called *BmpPixelSnoop* which wraps an Avalonia `WriteableBitmap` and provides fast `GetPixel()` and `SetPixel()` access to the original bitmap.

Unlike the original DotNetPixelSnoop, AvaloniaPixelSnoop is intended to be used as a shared library, thus you do not need to allow unsafe code in your project to use it.

## Usage
```
using Avalonia.Media.Imaging;
using AvaloniaPixelSnoop;

FileStream fs = FileStream.OpenRead("path/to/image.png");
var bitmap = WriteableBitmap.Decode(fs);

using (var snoop = new BmpPixelSnoop(bitmap))
{
  // Now use GetPixel() and SetPixel(), e.g.
  var col = snoop.GetPixel(0, 0);
}
```
For more usage examples, see the [demo program](AvaloniaPixelSnoop.Test/Program.cs).

When you are snooping a bitmap, you cannot access the snooped bitmap using the normal functions until the BmpPixelSnoop object goes out of scope, (e.g. when execution leaves the `using` block in the code example above). This is because the bitmap is locked (using `Lock()`) when it's being snooped, it is unlocked when the snoop object is disposed.

## License

AvaloniaPixelSnoop is licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE) for the full license text.
