I haven’t found a satisfying GIF encoder for dotnet yet, so I built one with AI (KIMI K3).

GIF89a animation encoder based on SkiaSharp:

- Encodes multiple equally sized SKBitmap frames sequentially into a single GIF animation
- Supports scaling compression (Scale / MaxWidth / MaxHeight) to drastically reduce file size
- Frame-difference optimization: only encodes regions changed from the previous frame; unchanged pixels are output as transparent
- Toggleable LZW compression: when disabled, raw literal bitstream is used (valid GIF, larger file size but faster encoding)
- Built-in NeuQuant neural-network palette quantization (automatically trims unused colors)
- Supports transparent pixels (alpha threshold binarization), loop count, and per-frame delay

Dependencies: NuGet package SkiaSharp (written against 2.88.x API; notes inside ScaleBitmap() explain adaptation for 3.x)

Usage Example:

```csharp
int delayHundredths = (int)Math.Round(100.0 / stage.FrameRate);
using var gifEncoder = GifEncoder.Create(filePath, delayHundredths);
for (int currentFrame = 0; currentFrame < frames; currentFrame++)
{
    using var bmp = ...;
    if (bmp == null) break;
    gifEncoder.AddFrame(bmp);
}
```

Scaling compression example (halve width and height; file size roughly 1/4 of original):

```csharp
using var gifEncoder = GifEncoder.Create(filePath, delayHundredths,
    new GifEncoderOptions { Scale = 0.5 });
```

Or limit the longest edge:

```csharp
new GifEncoderOptions { MaxWidth = 480 }
```

Disable frame-difference optimization / LZW compression:

```csharp
new GifEncoderOptions { EnableFrameDiff = false, EnableLzwCompression = false }
```