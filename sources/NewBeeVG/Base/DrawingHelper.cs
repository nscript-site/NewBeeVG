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
        var skColor = ToSKColor(stage.Background);
        using var canvas = new SKCanvas(bmp);
        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = skColor
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
