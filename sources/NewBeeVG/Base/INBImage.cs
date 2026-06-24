using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// Describes how content is resized to fill its allocated space.
/// </summary>
public enum Stretch
{
    /// <summary>
    /// The content preserves its original size.
    /// </summary>
    None,

    /// <summary>
    /// The content is resized to fill the destination dimensions. The aspect ratio is not
    /// preserved.
    /// </summary>
    Fill,

    /// <summary>
    /// The content is resized to fit in the destination dimensions while preserving its
    /// native aspect ratio.
    /// </summary>
    Uniform,

    /// <summary>
    /// The content is resized to completely fill the destination rectangle while preserving
    /// its native aspect ratio. A portion of the content may not be visible if the aspect
    /// ratio of the content does not match the aspect ratio of the allocated space.
    /// </summary>
    UniformToFill,
}

/// <summary>
/// Describes the type of scaling that can be used when scaling content.
/// </summary>
public enum StretchDirection
{
    /// <summary>
    /// Only scales the content upwards when the content is smaller than the available space.
    /// If the content is larger, no scaling downwards is done.
    /// </summary>
    UpOnly,

    /// <summary>
    /// Only scales the content downwards when the content is larger than the available space.
    /// If the content is smaller, no scaling upwards is done.
    /// </summary>
    DownOnly,

    /// <summary>
    /// Always stretches to fit the available space according to the stretch mode.
    /// </summary>
    Both,
}

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