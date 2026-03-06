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
