using SkiaSharp;

namespace NewBeeVG;

public class NBRect : NBAlignableShape
{
    public double CornerRadius { get; set; }

    public NBRect(double width, double height)
    {
        Width = width;
        Height = height;
    }

    protected override void RenderContent(SKCanvas context)
    {
        if(Shaders.IsEmpty() == true)
        {
            if (Fill.HasValue)
            {
                using (var paint = new SKPaint { Color = Fill.Value })
                {
                    if (CornerRadius <= 0)
                        context.DrawRect(Bounds, paint);
                    else
                        context.DrawRoundRect(Bounds, (float)CornerRadius, (float)CornerRadius, paint);
                }
            }
        }
        else
        {
            Shaders.BuildComposeShader(Bounds);
            using (var paint = new SKPaint { Shader = Shaders.ComposedShader, IsAntialias = true })
            {
                if (CornerRadius <= 0)
                    context.DrawRect(Bounds, paint);
                else
                    context.DrawRoundRect(Bounds, (float)CornerRadius, (float)CornerRadius, paint);
            }
        }
    }
}
