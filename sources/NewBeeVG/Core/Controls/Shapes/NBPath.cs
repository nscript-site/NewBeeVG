using SkiaSharp;
using SkiaSharp.Extended;

namespace NewBeeVG;

public class NBPath : NBVisual
{
    private SKPath? _path;

    public int? HAlign { get; set; } = -1;
    public int? VAlign { get; set; } = -1;

    public SKColor? Fill { get; set; }
    public NBBorder? Border { get; set; }

    public SKRect PathBounds { get; private set; }

    public NBPath(Action<SKPath> onCreate, SKColor? fill = null, NBBorder? border = null)
    {
        var path = new SKPath();
        onCreate(path);
        CreateFrom(path, fill, border);
    }

    public NBPath(SKPath? path, SKColor? fill = null, NBBorder? border = null)
    {
        CreateFrom(path, fill, border);
    }

    public NBPath(SKPath p0, SKPath p1, float t, SKColor? fill = null, NBBorder? border = null)
    {
        CreateFrom(p0, p1, t, fill, border);
    }

    public NBPath(Action<SKPath> onCreateStart, Action<SKPath> onCreateEnd, float t, SKColor? fill = null, NBBorder? border = null)
    {
        var p0 = new SKPath();
        onCreateStart(p0);

        var p1 = new SKPath();
        onCreateEnd(p1);

        var morph = new SKPathInterpolation(p0, p1);
        var p = morph.Interpolate(t);

        CreateFrom(p, fill, border);
    }

    public void UpdatePath(SKPath? path)
    {
        _path = path;
        PathBounds = _path?.ComputeTightBounds() ?? new SKRect();
    }

    public void UpdatePath(SKPath p0, SKPath p1, float t)
    {
        var morph = new SKPathInterpolation(p0, p1);
        var p = morph.Interpolate(t);
        UpdatePath(p);
    }

    private void CreateFrom(SKPath? path, SKColor? fill = null, NBBorder? border = null)
    {
        _path = path;
        PathBounds = _path?.ComputeTightBounds()??new SKRect();
        Fill = fill;
        Border = border;
    }

    private void CreateFrom(SKPath p0, SKPath p1, float t, SKColor? fill = null, NBBorder? border = null)
    {
        var morph = new SKPathInterpolation(p0, p1);
        var p = morph.Interpolate(t);
        CreateFrom(p, fill, border);
    }

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
        if (_path == null) return;

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
                        context.DrawPath(_path, paint);
                    }
                }
                if(Border != null && Border.Thickness > 0)
                {
                    using (var paint = new SKPaint { Color = Border.Color, StrokeWidth = Border.Thickness, IsStroke = true, IsAntialias = true })
                    {
                        context.DrawPath(_path, paint);
                    }
                }
            }
        }
        else
        {
            Shaders.BuildComposeShader(Bounds);
            using (var paint = new SKPaint { Shader = Shaders.ComposedShader, IsAntialias = true })
            {
                context.DrawPath(_path, paint);
            }
        }
        context.Restore();
    }
}

public static class NBPath_Extentions
{
    public static TCtrl Align<TCtrl>(this TCtrl self, int? hAlign = null, int? vAlign = null) where TCtrl : NBPath
    {
        self.HAlign = hAlign;
        self.VAlign = vAlign;
        return self;
    }

    public static TCtrl Path<TCtrl>(this TCtrl self, SKPath? path, SKPath? path2 = null, float t = 0) where TCtrl : NBPath
    {
        if(path2 == null)
            self.UpdatePath(path);
        else if(path != null)
            self.UpdatePath(path, path2, t);
        return self;
    }
}