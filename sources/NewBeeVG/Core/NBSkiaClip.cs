using SkiaSharp;

namespace NewBeeVG;

public class NBSkiaClip : NBClip
{
    public NBSkiaClip(string name = "clip", Action<NBDrawContext, NBClip, SKCanvas>? builder = null, int duration = 1, int? start = null)
        :base(name, ConvertBuilder(builder), duration, start)
    {
    }

    protected static Func<NBDrawContext, NBClip, Control?>? ConvertBuilder(Action<NBDrawContext, NBClip, SKCanvas>? skBuilder)
    {
        if (skBuilder == null) return null;

        return (ctx, clip) =>
        {
            var targetBitmap = new SKBitmap(ctx.width, ctx.height);
            using var canvas = new SKCanvas(targetBitmap);
            skBuilder(ctx, clip,canvas);
            return new NBSkiaBitmap() { Bitmap = targetBitmap };
        };
    }
}

public class NBMaskedSkiaClip : NBClip
{
    public SKBlendMode BlendMode { get; private set; }

    public NBMaskedSkiaClip(string name = "clip", 
        Action<NBDrawContext, NBClip, SKCanvas>? builder = null,
        Action<NBDrawContext, NBClip, SKCanvas>? maskBuilder = null, 
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
        : base(name, ConvertBuilder(builder, maskBuilder, blend), duration, start)
    {
        BlendMode = blend;
    }

    protected static Func<NBDrawContext, NBClip, Control?>? ConvertBuilder(
        Action<NBDrawContext, NBClip, SKCanvas>? skBuilder,
        Action<NBDrawContext, NBClip, SKCanvas>? maskBuilder, SKBlendMode blend)
    {
        if (skBuilder == null) return null;

        if(maskBuilder == null)
        {
            return (ctx, clip) =>
            {
                var targetBitmap = new SKBitmap(ctx.width, ctx.height);
                using var canvas = new SKCanvas(targetBitmap);
                skBuilder(ctx, clip, canvas);
                return new NBSkiaBitmap() { Bitmap = targetBitmap };
            };
        }

        return (ctx, clip) =>
        {
            using var srcBitmap = new SKBitmap(ctx.width, ctx.height);
            using var maskBitmap = new SKBitmap(ctx.width, ctx.height);
            var targetBitmap = new SKBitmap(ctx.width, ctx.height);

            using var maskCanvas = new SKCanvas(maskBitmap);
            maskBuilder(ctx, clip, maskCanvas);

            using var srcCanvas = new SKCanvas(srcBitmap);
            skBuilder(ctx, clip, srcCanvas);

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

            return new NBSkiaBitmap() { Bitmap = targetBitmap };
        };
    }
}