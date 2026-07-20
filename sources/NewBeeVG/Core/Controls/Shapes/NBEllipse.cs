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
        if (Shaders.IsEmpty() == true)
        {
            if (Fill.HasValue)
            {
                using (var paint = new SKPaint { Color = Fill.Value })
                {
                    context.DrawOval(Bounds, paint);
                }
            }
        }
        else
        {
            Shaders.UpdateShader(Bounds);
            using (var paint = new SKPaint { Shader = Shaders.Shader, IsAntialias = true })
            {
                context.DrawOval(Bounds, paint);
            }
        }
    }
}
