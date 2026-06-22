using Avalonia.Media.Imaging;
using SkiaSharp;

namespace NewBeeVG;

internal class DrawingHelper
{
    public static void DrawBitmap(RenderTargetBitmap bitmap, Control? content, int width, int height)
    {
        if (content == null) return;

        Layout(content, width, height);

        bitmap.Render(content);
    }

    public static unsafe WriteableBitmap? ToWriteableBitmap(SKBitmap? bitmap)
    {
        if (bitmap == null || bitmap.IsEmpty) return null;

        var pixelSize = new PixelSize(bitmap.Width, bitmap.Height);
        var dpi = new Vector(96, 96);

        // 先把 SKBitmap 的像素直接拷贝到 WriteableBitmap
        var writeableBitmap = new WriteableBitmap(
            pixelSize,
            dpi,
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul);

        var srcPixels = bitmap.GetPixels();
        if (srcPixels == IntPtr.Zero)
            return null;

        using (var locked = writeableBitmap.Lock())
        {
            var srcStride = bitmap.RowBytes;
            var dstStride = locked.RowBytes;
            var copyStride = Math.Min(srcStride, dstStride);

            byte* srcBase = (byte*)srcPixels.ToPointer();
            byte* dstBase = (byte*)locked.Address.ToPointer();

            for (int y = 0; y < bitmap.Height; y++)
            {
                Buffer.MemoryCopy(
                    srcBase + y * srcStride,
                    dstBase + y * dstStride,
                    dstStride,
                    copyStride);
            }
        }

        return writeableBitmap;
    }

    public static SKBitmap ToSKBitmap(RenderTargetBitmap rtBmp, NBStage? stage = null, bool drawStageBackground = false)
    {
        var width = rtBmp.PixelSize.Width;
        var height = rtBmp.PixelSize.Height;

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var skBmp = new SKBitmap(info);

        if(stage != null && drawStageBackground == true)
        {
            FillStageBackgroundIfSet(skBmp, stage, drawStageBackground);
        }

        var pixels = skBmp.GetPixels();
        if (pixels == IntPtr.Zero)
        {
            skBmp.Dispose();
            throw new InvalidOperationException("Failed to get pixels from SKBitmap.");
        }

        var pixelRect = new PixelRect(0, 0, width, height);
        rtBmp.CopyPixels(pixelRect, pixels, skBmp.RowBytes * height, skBmp.RowBytes);

        return skBmp;
    }

    public static void FillStageBackgroundIfSet(SKBitmap? bmp, NBStage stage, bool drawStageBackground)
    {
        if (bmp == null || drawStageBackground == false || stage.Background == null) return;
        var skColor = stage.Background.Value;
        using var canvas = new SKCanvas(bmp);
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = skColor,
            IsAntialias = true
        };
        canvas.DrawRect(new SKRect(0, 0, bmp.Width, bmp.Height), paint);
    }

    public static SKBitmap? Render(Control? control, NBStage stage, bool includeStageBackground)
    {
        if (control == null) return null;

        using var rtBmp = new RenderTargetBitmap(new PixelSize(stage.Width, stage.Height));
        DrawBitmap(rtBmp, control, stage.Width, stage.Height);
        return DrawingHelper.ToSKBitmap(rtBmp, stage, includeStageBackground);
    }

    public static void Layout(Control? content, int width, int height)
    {
        if (content == null) return;

        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
    }

    public static SKColor ToSKColor(ISolidColorBrush brush)
    {
        return new SKColor(
            brush.Color.R,
            brush.Color.G,
            brush.Color.B,
            (byte)(brush.Opacity * 255));
    }
}
