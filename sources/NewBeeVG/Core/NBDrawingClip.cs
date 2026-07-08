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
        Func<NBDrawContext, NBClip, NBLayoutable?>? mask = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
    : base(name, ConvertBuilder(builder, null, null, mask, blend), duration, start)
    {
        BlendMode = blend;
    }

    public NBDrawingClip(string name = "clip",
        Func<NBDrawContext, NBClip, NBLayoutable?>? builder = null,
        Action<NBDrawContext, NBClip, SKCanvas>? mask = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
    : base(name, ConvertBuilder(null, builder, mask, null, blend), duration, start)
    {
        BlendMode = blend;
    }

    public NBDrawingClip(string name = "clip",
        Func<NBDrawContext, NBClip, NBLayoutable?>? builder = null,
        Func<NBDrawContext, NBClip, NBLayoutable?>? mask = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
        : base(name, ConvertBuilder(null, builder, null, mask, blend), duration, start)
    {
        BlendMode = blend;
    }


    protected static Action<NBDrawContext, NBClip, SKCanvas>? ConvertBuilder(
        Action<NBDrawContext, NBClip, SKCanvas>? builder1,
        Func<NBDrawContext, NBClip, NBLayoutable?>? builder2,
        Action<NBDrawContext, NBClip, SKCanvas>? mask1,
        Func<NBDrawContext, NBClip, NBLayoutable?>? mask2,
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
            var targetBitmap = new SKBitmap(ctx.width, ctx.height);

            using var maskCanvas = new SKCanvas(maskBitmap);
            if (mask1 != null)
            {
                mask1(ctx, clip, maskCanvas);
            }
            else if (mask2 != null)
            {
                mask2.Render(ctx, clip, maskCanvas);
            }

            using var srcCanvas = new SKCanvas(srcBitmap);
            if(builder1 != null)
                builder1(ctx, clip, srcCanvas);
            else if(builder2 != null)
                builder2.Render(ctx, clip, srcCanvas);

            using var targetCanvas = new SKCanvas(targetBitmap);
            targetCanvas.Clear(SKColors.Transparent); // 确保目标位图初始透明
            targetCanvas.DrawBitmap(maskBitmap, new SKPoint(0, 0));

            using var paint = new SKPaint
            {
                BlendMode = blend,
                IsAntialias = true // 抗锯齿，边缘更平滑
            };
            // 绘制遮罩位图（尺寸和目标图一致，保证覆盖）
            targetCanvas.DrawBitmap(srcBitmap, new SKPoint(0, 0), paint);
            canvas.DrawBitmap(targetBitmap, new SKPoint(0, 0));
        };
    }
}