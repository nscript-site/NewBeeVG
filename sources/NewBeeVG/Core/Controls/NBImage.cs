/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

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

public class NBImage : NBLayoutable, INBImage
{
    public SKBitmap? Source { get; set; }

    public Stretch Stretch { get; set; } = Stretch.Uniform;

    public StretchDirection StretchDirection { get; set; } = StretchDirection.Both;

    public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;

    /// <summary>
    /// Measures the control.
    /// </summary>
    /// <param name="availableSize">The available size.</param>
    /// <returns>The desired size of the control.</returns>
    protected override Size MeasureOverride(Size availableSize)
    {
        var source = Source;
        var result = new Size();

        if (source != null)
        {
            result = Stretch.CalculateSize(availableSize, new Size(source.Width, source.Height), StretchDirection);
        }

        return result;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var source = Source;

        if (source != null)
        {
            var sourceSize = new Size(source.Width, source.Height);
            var result = Stretch.CalculateSize(finalSize, sourceSize, StretchDirection);
            return result;
        }
        else
        {
            return new Size();
        }
    }

    protected override void RenderCore(SKCanvas context)
    {
        var source = Source;

        if (source != null && Bounds.Width > 0 && Bounds.Height > 0)
        {
            var viewPort = new SKRect(Bounds.Left, Bounds.Top, Bounds.Right, Bounds.Bottom);
            var sourceSize = new SKSize(source.Width, source.Height);

            Vector scale = Stretch.CalculateScaling(Bounds.Size, sourceSize, StretchDirection);
            SKSize scaledSize = new SKSize((float)(sourceSize.Width * scale.X), (float)(sourceSize.Height * scale.Y));
            SKRect destRect = viewPort
                .CenterRect(new SKRect(0, 0, scaledSize.Width, scaledSize.Height))
                .IntersectRect(viewPort);
            SKRect sourceRect = new SKRect(0, 0, sourceSize.Width, sourceSize.Height)
                .CenterRect(new SKRect(0, 0, (float)(destRect.Width / scale.X), (float)(destRect.Height / scale.Y)));

            // 绘制图像
            using var paint = new SKPaint { BlendMode = BlendMode };
            context.DrawBitmap(source, sourceRect, destRect, paint);
        }
    }
}

public static partial class NBExtentions
{
    public static T Source<T>(this T widget, SKBitmap? source) where T : NBImage
    {
        widget.Source = source;
        return widget;
    }
}
