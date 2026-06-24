using SkiaSharp;

namespace NewBeeVG;

public interface INBImage
{
    public Stretch Stretch { get; set; }

    public StretchDirection StretchDirection { get; set; }

    public SKBlendMode BlendMode { get; set; }
}

public static partial class NBExtentions
{
    public static T Stretch<T>(this T widget, Stretch stretch) where T : INBImage
    {
        widget.Stretch = stretch;
        return widget;
    }

    public static T StretchDirection<T>(this T widget, StretchDirection stretchDirection) where T : INBImage
    {
        widget.StretchDirection = stretchDirection;
        return widget;
    }

    public static T BlendMode<T>(this T widget, SKBlendMode blendMode) where T : INBImage
    {
        widget.BlendMode = blendMode;
        return widget;
    }
}