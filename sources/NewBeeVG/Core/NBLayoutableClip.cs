using SkiaSharp;

namespace NewBeeVG;

public class NBLayoutableClip : NBClip
{
    public SKBlendMode BlendMode { get; private set; } = default!;
    private readonly Func<NBDrawContext, NBClip, NBLayoutable?>? _skBuilder;
    private readonly Func<NBDrawContext, NBClip, NBLayoutable?>? _skMaskBuilder;

    public NBLayoutableClip(string name = "clip",
        Func<NBDrawContext, NBClip, NBLayoutable?>? builder = null,
        Func<NBDrawContext, NBClip, NBLayoutable?>? maskBuilder = null,
        SKBlendMode blend = SKBlendMode.SrcIn,
        int duration = 1, int? start = null)
        : base(name, ConvertBuilder(builder,maskBuilder,blend), duration, start)
    {
        _skBuilder = builder;
        _skMaskBuilder = maskBuilder;
        BlendMode = blend;
    }

    protected static Action<NBDrawContext, NBClip, SKCanvas>? ConvertBuilder(Func<NBDrawContext, NBClip, NBLayoutable?>? skBuilder,
        Func<NBDrawContext, NBClip, NBLayoutable?>? maskBuilder, SKBlendMode blend)
    {
        if (skBuilder == null) return null;
        if (maskBuilder == null) return skBuilder.Render;

        return (ctx, clip, canvas) =>
        {
            using var srcBitmap = new SKBitmap(ctx.width, ctx.height);
            using var maskBitmap = new SKBitmap(ctx.width, ctx.height);
            using var targetBitmap = new SKBitmap(ctx.width, ctx.height);

            using var maskCanvas = new SKCanvas(maskBitmap);
            maskBuilder.Render(ctx, clip, maskCanvas);

            using var srcCanvas = new SKCanvas(srcBitmap);
            skBuilder.Render(ctx, clip, srcCanvas);

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