using SkiaSharp;

namespace NewBeeVG;

public class NBDrawingClip : NBClip
{
    public SKBlendMode BlendMode { get; private set; }

    public NBDrawingClip(string name = "clip", 
        Action<NBDrawContext, NBClip, SKCanvas>? builder = null,
        Action<NBDrawContext, NBClip, SKCanvas>? mask = null, 
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
        : base(name, ConvertBuilder(builder, null, mask, null, blend), duration, start)
    {
        BlendMode = blend;
    }

    public NBDrawingClip(string name = "clip",
        Action<NBDrawContext, NBClip, SKCanvas>? builder = null,
        Func<NBDrawContext, NBClip, NBVisual?>? mask = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
    : base(name, ConvertBuilder(builder, null, null, mask, blend), duration, start)
    {
        BlendMode = blend;
    }

    public NBDrawingClip(string name = "clip",
        Func<NBDrawContext, NBClip, NBVisual?>? builder = null,
        Action<NBDrawContext, NBClip, SKCanvas>? mask = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
    : base(name, ConvertBuilder(null, builder, mask, null, blend), duration, start)
    {
        BlendMode = blend;
    }

    public NBDrawingClip(string name = "clip",
        Func<NBDrawContext, NBClip, NBVisual?>? builder = null,
        Func<NBDrawContext, NBClip, NBVisual?>? mask = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
        : base(name, ConvertBuilder(null, builder, null, mask, blend), duration, start)
    {
        BlendMode = blend;
    }


    protected static Action<NBDrawContext, NBClip, SKCanvas>? ConvertBuilder(
        Action<NBDrawContext, NBClip, SKCanvas>? builder1,
        Func<NBDrawContext, NBClip, NBVisual?>? builder2,
        Action<NBDrawContext, NBClip, SKCanvas>? mask1,
        Func<NBDrawContext, NBClip, NBVisual?>? mask2,
        SKBlendMode blend)
    {
        if (builder1 == null && builder2 == null) return null;

        if(mask1 == null && mask2 == null)
        {
            if (builder1 != null) return builder1;
            else return builder2!.Render;
        }

        return (ctx, clip, canvas) =>
        {
            using var srcBitmap = new SKBitmap(ctx.width, ctx.height);
            using var maskBitmap = new SKBitmap(ctx.width, ctx.height);
            using var targetBitmap = new SKBitmap(ctx.width, ctx.height);

            NBLayoutable? content = null;

            using var srcCanvas = new SKCanvas(srcBitmap);
            if (builder1 != null)
                builder1(ctx, clip, srcCanvas);
            else if (builder2 != null)
                content = builder2.RenderCore(ctx, clip, srcCanvas, null);

            using var maskCanvas = new SKCanvas(maskBitmap);
            if (mask1 != null)
            {
                mask1(ctx, clip, maskCanvas);
            }
            else if (mask2 != null)
            {
                mask2.RenderCore(ctx, clip, maskCanvas, content);
            }

            using var targetCanvas = new SKCanvas(targetBitmap);
            targetCanvas.Clear(SKColors.Transparent); // 确保目标位图初始透明
            targetCanvas.DrawBitmap(maskBitmap, new SKPoint(0, 0));

            using var paint = new SKPaint
            {
                BlendMode = blend,
                IsAntialias = true // 抗锯齿，边缘更平滑
            };
            targetCanvas.DrawBitmap(srcBitmap, new SKPoint(0, 0), paint);
            canvas.DrawBitmap(targetBitmap, new SKPoint(0, 0));
        };
    }
}