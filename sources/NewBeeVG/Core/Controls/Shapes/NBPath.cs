using SkiaSharp;

namespace NewBeeVG;

public class NBPath : NBVisual
{
    private SKPath Path { get; init; }

    public int? HAlign { get; set; } = -1;
    public int? VAlign { get; set; } = -1;

    public SKRect PathBounds { get; init; }

    public NBPath(Action<SKPath> onCreate, SKColor? fill = null, NBBorder? border = null)
    {
        var path = new SKPath();
        onCreate(path);
        PathBounds = path.ComputeTightBounds();
        Path = path;
        Fill = fill;
        Border = border;
    }

    public SKColor? Fill { get; set; }
    public NBBorder? Border { get; set; }

    protected internal override void TryMeasure(Size availableSize)
    {
        var size = PathBounds.Size;
        double width = size.Width;
        double height = size.Height;
        if (VAlign == null) height = availableSize.Height;
        if (HAlign == null) height = availableSize.Width;

        this.DesiredSize = new Size(width, height);
    }

    protected internal override void TryArrange(Rect rect)
    {
        var size = PathBounds.Size;
        double width = size.Width;
        double height = size.Height;

        SKPoint origin = new SKPoint((float)rect.Left, (float)rect.Top);
        if (HAlign != null)
        {
            if (HAlign < 0) origin.X = (float)rect.Left;
            else if (HAlign == 0) origin.X = (float)(rect.Left + rect.Width / 2 - width / 2);
            else if (HAlign > 0) origin.X = (float)(rect.Right - width);
        }
        if (VAlign != null)
        {
            if (VAlign < 0) origin.Y = (float)rect.Top;
            else if (VAlign == 0) origin.Y = (float)(rect.Top + rect.Height / 2 - height / 2);
            else if (VAlign > 0) origin.Y = (float)(rect.Bottom - height);
        }

        this.Bounds = new SKRect(origin.X, origin.Y, origin.X + (float)width, origin.Y + (float)height);
    }

    protected override void RenderContent(SKCanvas context)
    {
        var size = PathBounds.Size;
        double width = size.Width;
        double height = size.Height;
        double sx = width / Bounds.Width;
        double sy = height / Bounds.Height;

        context.Save();
        context.Translate(Bounds.Left, Bounds.Top);
        context.Scale((float)sx, (float)sy);
        if (Shaders.IsEmpty() == true)
        {
            if (Fill.HasValue || Border?.Thickness > 0)
            {
                if(Fill.HasValue)
                {
                    using (var paint = new SKPaint { Color = Fill.Value, IsAntialias = true })
                    {
                        context.DrawPath(Path, paint);
                    }
                }
                if(Border != null && Border.Thickness > 0)
                {
                    using (var paint = new SKPaint { Color = Border.Color, StrokeWidth = Border.Thickness, IsStroke = true, IsAntialias = true })
                    {
                        context.DrawPath(Path, paint);
                    }
                }
            }
        }
        else
        {
            Shaders.BuildComposeShader(Bounds);
            using (var paint = new SKPaint { Shader = Shaders.ComposedShader, IsAntialias = true })
            {
                context.DrawPath(Path, paint);
            }
        }
        context.Restore();
    }
}

public static class NBPath_Extentions
{
    public static TCtrl Align<TCtrl>(this TCtrl ctrl, int? hAlign = null, int? vAlign = null) where TCtrl : NBPath
    {
        ctrl.HAlign = hAlign;
        ctrl.VAlign = vAlign;
        return ctrl;
    }
}