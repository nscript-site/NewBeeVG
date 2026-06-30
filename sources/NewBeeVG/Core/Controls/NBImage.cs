/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using SkiaSharp;

namespace NewBeeVG;

public class NBBaseImage : NBLayoutable, INBImage
{
    protected virtual SKSize? GetImageSize()
    {
        return null;
    }

    public Exception? DecodeException { get; protected set; }

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
        var size = GetImageSize();

        if (size != null)
        {
            return Stretch.CalculateSize(availableSize, new Size(size.Value.Width, size.Value.Height), StretchDirection);
        }
        else
        {
            TryShowException();

            if(_exceptionText != null)
            {
                _exceptionText.Measure(availableSize);
                return _exceptionText.DesiredSize;
            }
        }

        return new Size();
    }

    protected NBText? _exceptionText;
    protected void TryShowException()
    {
        if (DecodeException == null) return;
        if (_exceptionText != null) return;
        _exceptionText = Methods.TextBlock(DecodeException.Message, 32, SKColors.Red);
        this.VisualChildren.Add(_exceptionText);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = GetImageSize();

        if (size != null)
        {
            var sourceSize = new Size(size.Value.Width, size.Value.Height);
            var result = Stretch.CalculateSize(finalSize, sourceSize, StretchDirection);
            return result;
        }
        else
        {
            TryShowException();

            if (_exceptionText != null)
            {
                //_exceptionText.TryArrange(new Rect(0,0, finalSize.Width, finalSize.Height));
                return _exceptionText.DesiredSize;
            }

            return new Size();
        }
    }

    protected override void RenderCore(SKCanvas context)
    {
        if(_exceptionText != null)
        {
            base.RenderCore(context);
            return;
        }

        var imgSize = GetImageSize();
        if (imgSize == null) return;

        var source = imgSize.Value;

        if (Bounds.Width > 0 && Bounds.Height > 0)
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
            Draw(context, sourceRect, destRect, paint);
        }
    }

    protected virtual void Draw(SKCanvas context, SKRect sourceRect, SKRect destRect, SKPaint paint)
    {

    }
}

public class NBImage : NBBaseImage
{
    public SKBitmap? Source { get; set {
            field = value;
            if (value == null) _size = null;
            else _size = new SKSize(value.Width, value.Height);
        } }

    private SKSize? _size;
    protected override SKSize? GetImageSize()
    {
        return _size;
    }

    protected override void Draw(SKCanvas context, SKRect sourceRect, SKRect destRect, SKPaint paint)
    {
        if(Source != null)
        {
            context.DrawBitmap(Source, sourceRect, destRect, paint);
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
