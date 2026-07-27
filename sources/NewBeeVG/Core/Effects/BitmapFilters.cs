using SkiaSharp;

namespace NewBeeVG;

public static class BitmapFilters
{
}

public class NBStrokeBitmapFilter : NBBitmapFilter
{
    public SKColor StrokeColor { get; init; } = SKColors.White;
    public int StrokeWidth { get; init; } = 5;
    public byte AlphaThreshold { get; init; } = 128;

    public override (SKBitmap?, SKMatrix?) Filter(SKBitmap? bitmap)
    {
        if (bitmap == null) return (null, null);
        var newBitmap = AddStroke(bitmap, AlphaThreshold, StrokeWidth, StrokeColor);
        return (newBitmap, null);
    }

    internal static SKBitmap AddStroke(
       SKBitmap source,
       byte threshold,
       int strokeWidth,
       SKColor strokeColor)
    {
        int w = source.Width, h = source.Height;
        int total = w * h;

        // ---------- 1. 提取 Alpha 并二值化（普通循环）----------
        bool[] binaryMask = new bool[total];
        using (SKPixmap pixmap = source.PeekPixels())
        {
            if (pixmap == null) throw new Exception("无法获取像素数据");
            ReadOnlySpan<SKColor> colors = pixmap.GetPixelSpan<SKColor>();

            for (int i = 0; i < total; i++)
            {
                binaryMask[i] = colors[i].Alpha >= threshold;
            }
        }

        // ---------- 2. 将二值化蒙版转换为位图（白色=不透明）----------
        using (SKBitmap maskBitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque))
        using (SKPixmap maskPix = maskBitmap.PeekPixels())
        {
            Span<SKColor> maskColors = maskPix.GetPixelSpan<SKColor>();
            for (int i = 0; i < total; i++)
            {
                maskColors[i] = binaryMask[i] ? SKColors.White : SKColors.Black;
            }

            // ---------- 3. 使用膨胀滤镜（C++ 实现，高性能）----------
            using (SKImageFilter dilateFilter = SKImageFilter.CreateDilate(strokeWidth, strokeWidth))
            using (SKPaint paint = new SKPaint { ImageFilter = dilateFilter })
            using (SKBitmap dilated = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Opaque))
            using (SKCanvas canvas = new SKCanvas(dilated))
            {
                canvas.Clear(SKColors.Black);
                canvas.DrawBitmap(maskBitmap, 0, 0, paint);
                // 现在 dilated 中白色区域就是膨胀后的蒙版

                // ---------- 4. 生成描边层（普通循环比较）----------
                SKBitmap strokeBitmap = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
                using (SKPixmap strokePix = strokeBitmap.PeekPixels())
                using (SKPixmap dilatedPix = dilated.PeekPixels())
                {
                    Span<SKColor> strokeColors = strokePix.GetPixelSpan<SKColor>();
                    ReadOnlySpan<SKColor> dilatedColors = dilatedPix.GetPixelSpan<SKColor>();

                    SKColor strokeCol = new SKColor(strokeColor.Red, strokeColor.Green, strokeColor.Blue, strokeColor.Alpha);

                    for (int i = 0; i < total; i++)
                    {
                        // 膨胀蒙版为白（Red>128） 且 原二值蒙版为黑 → 描边区域
                        bool isDilated = dilatedColors[i].Red > 128;
                        if (isDilated && !binaryMask[i])
                        {
                            strokeColors[i] = strokeCol;
                        }
                        else
                        {
                            strokeColors[i] = SKColors.Transparent;
                        }
                    }
                }

                // ---------- 5. 合成最终结果（抗锯齿）----------
                SKBitmap result = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
                using (SKCanvas finalCanvas = new SKCanvas(result))
                using (SKPaint antialiasPaint = new SKPaint { IsAntialias = true })
                {
                    finalCanvas.DrawBitmap(strokeBitmap, 0, 0, antialiasPaint);
                    finalCanvas.DrawBitmap(source, 0, 0, antialiasPaint);
                }

                strokeBitmap.Dispose();
                return result;
            }
        }
    }
}