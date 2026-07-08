using SkiaSharp;

namespace NewBeeVG;

public class NBAlignableShape : NBVisual
{
    public int? HAlign { get; set; } = -1;
    public int? VAlign { get; set; } = -1;

    public double Width { get; set { field = Math.Max(0, value); } }
    public double Height { get; set { field = Math.Max(0, value); } }

    public SKColor? Fill { get; set; }

    protected internal override void TryMeasure(Size availableSize)
    {
        if (VAlign == null) Height = availableSize.Height;
        if (HAlign == null) Width = availableSize.Width;

        this.DesiredSize = new Size(Width, Height);
    }

    protected internal override void TryArrange(Rect rect)
    {
        SKPoint origin = new SKPoint((float)rect.Left, (float)rect.Top);
        if(HAlign != null)
        {
            if (HAlign < 0) origin.X = (float)rect.Left;
            else if (HAlign == 0) origin.X = (float)(rect.Left + rect.Width / 2 - Width / 2);
            else if (HAlign > 0) origin.X = (float)(rect.Right - Width);
        }
        if(VAlign != null)
        {
            if (VAlign < 0) origin.Y = (float)rect.Top;
            else if (VAlign == 0) origin.Y = (float)(rect.Top + rect.Height / 2 - Height / 2);
            else if (VAlign > 0) origin.Y = (float)(rect.Bottom - Height);
        }

        this.Bounds = new SKRect(origin.X, origin.Y, origin.X + (float)Width, origin.Y + (float)Height);
    }
}

public static class NBAlignableShape_Extentions
{
    public static TCtrl Align<TCtrl>(this TCtrl ctrl, int? hAlign = null, int? vAlign = null) where TCtrl : NBAlignableShape
    {
        ctrl.HAlign = hAlign;
        ctrl.VAlign = vAlign;
        return ctrl;
    }

    public static TCtrl Size<TCtrl>(this TCtrl ctrl, double width, double height) where TCtrl : NBAlignableShape
    {
        ctrl.Width = width;
        ctrl.Height = height;
        return ctrl;
    }

    public static TCtrl Height<TCtrl>(this TCtrl ctrl, double height) where TCtrl : NBAlignableShape
    {
        ctrl.Height = height;
        return ctrl;
    }

    public static TCtrl Width<TCtrl>(this TCtrl ctrl, double width) where TCtrl : NBAlignableShape
    {
        ctrl.Width = width;
        return ctrl;
    }
}