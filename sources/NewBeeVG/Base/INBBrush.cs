using SkiaSharp;

namespace NewBeeVG;

public interface INBBrush
{
    public SKPaint GetPaint();
}

public abstract class NBBrush : INBBrush
{
    protected SKPaint? _paint;

    protected abstract SKPaint CreatePaint();

    public SKPaint GetPaint()
    {
        if(_paint == null)
        {
            _paint = CreatePaint();
        }
        return _paint;
    }

    /// <summary>
    /// 允许将 `SKColor` 自动转换为 `NBBrush`。
    /// </summary>
    public static implicit operator NBBrush(SKColor color)
    {
        return new NBColorBrush(color);
    }
}

public class NBColorBrush : NBBrush
{
    public SKColor Color { get; private set; }

    public NBColorBrush(SKColor color)
    {
        Color = color;
    }

    protected override SKPaint CreatePaint()
    {
        return new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = Color
        };
    }
}