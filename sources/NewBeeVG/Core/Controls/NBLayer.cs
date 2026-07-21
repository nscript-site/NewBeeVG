using SkiaSharp;

namespace NewBeeVG;

public class NBLayer : NBBaseImage
{
    private SKSize? _size;

    public NBVisual? Source { get; set; }

    public NBVisual? Mask { get; set; }

    public SKBlendMode MaskBlendMode { get; set; } = SKBlendMode.SrcIn;

    protected override SKSize? GetImageSize()
    {
        if(_size == null)
        {
            if (double.IsNaN(Width) || double.IsNaN(Height) || Source == null) 
                _size = new SKSize(0,0);
            else
                _size = new SKSize((float)Math.Max(0, Width), (float)Math.Max(0, Height));
        }
        return _size;
    }

    protected override void Draw(SKCanvas context, SKRect sourceRect, SKRect destRect, SKPaint paint)
    {
        if (Source == null) return;

        var size = GetImageSize();
        if (size == null || size.Value.Width <= 0 || size.Value.Height <= 0) return;

        if(Mask == null)
        {
            using var srcBitmap = new SKBitmap((int)size.Value.Width, (int)size.Value.Height);
            using var srcCanvas = new SKCanvas(srcBitmap);
            var content = Source.RenderCore(size.Value, srcCanvas, null);
            context?.DrawBitmap(srcBitmap, sourceRect, destRect, paint);
        }
        else
        {
            using var srcBitmap = new SKBitmap((int)size.Value.Width, (int)size.Value.Height);
            using var maskBitmap = new SKBitmap((int)size.Value.Width, (int)size.Value.Height);
            var targetBitmap = new SKBitmap((int)size.Value.Width, (int)size.Value.Height);

            NBLayoutable? content = null;

            using var srcCanvas = new SKCanvas(srcBitmap);
            content = Source.RenderCore(size.Value, srcCanvas, null);

            using var maskCanvas = new SKCanvas(maskBitmap);
            Mask.RenderCore(size.Value, maskCanvas, content);

            using var targetCanvas = new SKCanvas(targetBitmap);
            targetCanvas.Clear(SKColors.Transparent); // 确保目标位图初始透明
            targetCanvas.DrawBitmap(maskBitmap, new SKPoint(0, 0));

            using var p = new SKPaint
            {
                BlendMode = MaskBlendMode,
                IsAntialias = true // 抗锯齿，边缘更平滑
            };

            // 绘制遮罩位图（尺寸和目标图一致，保证覆盖）
            targetCanvas.DrawBitmap(srcBitmap, new SKPoint(0, 0), p);

            context.DrawBitmap(targetBitmap, sourceRect, destRect, paint);
        }
    }
}

public static partial class NBExtentions
{
    public static T Mask<T>(this T self, NBVisual? mask) where T : NBLayer
    {
        self.Mask = mask;
        return self;
    }

    public static T MaskBlend<T>(this T self, SKBlendMode blendMode) where T : NBLayer
    {
        self.MaskBlendMode = blendMode;
        return self;
    }

    public static T Source<T>(this T self, NBVisual? source) where T : NBLayer
    {
        self.Source = source;
        return self;
    }
}