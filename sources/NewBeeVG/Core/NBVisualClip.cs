using SkiaSharp;

namespace NewBeeVG;

public class NBVisualClip : NBClip
{
    private readonly Func<NBDrawContext, NBClip, NBVisual?>? _skBuilder;

    public NBVisualClip(string name = "clip", Func<NBDrawContext, NBClip, NBVisual?>? builder = null, int duration = 1, int? start = null)
        : base(name, ConvertBuilder(builder), duration, start)
    {
        _skBuilder = builder;
    }

    protected static Func<NBDrawContext, NBClip, SKBitmap?>? ConvertBuilder(Func<NBDrawContext, NBClip, NBVisual?>? builder)
    {
        if (builder == null) return null;

        return (ctx, clip) =>
        {
            var visual = builder(ctx, clip);
            if (visual == null) return null;
            var targetBitmap = new SKBitmap(ctx.width, ctx.height);
            using var canvas = new SKCanvas(targetBitmap);
            visual.Render(canvas);
            return targetBitmap;
        };
    }
}
