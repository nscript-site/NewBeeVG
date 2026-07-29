using SkiaSharp;

namespace NewBeeVG;

public class NBEllipse : NBAlignableShape
{
    public NBEllipse(double width, double height)
    {
        Width = width;
        Height = height;
    }

    protected override void RenderContent(SKCanvas context)
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
            Shaders.BuildComposeShader(Bounds);
            using (var paint = new SKPaint { Shader = Shaders.ComposedShader, IsAntialias = true })
            {
                context.DrawOval(Bounds, paint);
            }
        }
    }
}
