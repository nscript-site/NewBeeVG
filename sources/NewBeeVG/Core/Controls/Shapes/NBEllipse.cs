using SkiaSharp;

namespace NewBeeVG;

public class NBEllipse : NBAlignableShape
{
    public NBEllipse(double width, double height)
    {
        Width = width;
        Height = height;
    }

    protected override void RenderCore(SKCanvas context)
    {
        if (Fill.HasValue)
        {
            using (var paint = new SKPaint { Color = Fill.Value })
            {
                context.DrawOval(Bounds, paint);
            }
        }
    }
}
