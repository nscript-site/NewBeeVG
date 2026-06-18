using SkiaSharp;

namespace NewBeeVG;

public class NBLayoutableClip : NBClip
{
    private readonly Func<NBDrawContext, NBClip, NBLayoutable?>? _skBuilder;

    public NBLayoutableClip(string name = "clip", Func<NBDrawContext, NBClip, NBLayoutable?>? builder = null, int duration = 1, int? start = null)
        : base(name, ConvertBuilder(builder), duration, start)
    {
        _skBuilder = builder;
    }

    protected static Func<NBDrawContext, NBClip, SKBitmap?>? ConvertBuilder(Func<NBDrawContext, NBClip, NBLayoutable?>? builder)
    {
        if (builder == null) return null;

        return (ctx, clip) =>
        {
            var visual = builder(ctx, clip);
            if (visual == null) return null;

            var targetBitmap = new SKBitmap(ctx.width, ctx.height);
            using var canvas = new SKCanvas(targetBitmap);
            visual.Measure(ctx.width, ctx.height);
            visual.Arrange(0, 0, ctx.width, ctx.height);
            visual.Render(canvas);
            return targetBitmap;
        };
    }
}