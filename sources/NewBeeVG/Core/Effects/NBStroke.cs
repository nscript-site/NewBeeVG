using SkiaSharp;

namespace NewBeeVG;

public class NBStroke
{
    public SKColor StrokeColor { get; set; }

    public float StrokeWidth { get; set; } = 0f;

    public SKStrokeCap StrokeCap { get; set; } = SKStrokeCap.Square;

    public SKStrokeJoin StrokeJoin { get; set; } = SKStrokeJoin.Bevel;

    public SKPaint CreatePaint()
    {
        return new SKPaint
        {
            IsAntialias = true,
            Color = this.StrokeColor,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            IsDither = true,
            StrokeCap = StrokeCap,
            StrokeJoin = StrokeJoin
        };
    }
}

public class NBStrokeCollection
{
    public List<NBStroke> Strokes { get; private set; } = new List<NBStroke>();

    public void AddStroke(NBStroke stroke)
    {
        Strokes.Add(stroke);
    }

    public void ClearStrokes()
    {
        Strokes.Clear();
    }

    public bool IsEmpty()
    {
        return Strokes.Count == 0;
    }

    public float GetMaxStrokeWidth()
    {
        if (Strokes.Count == 0) return 0;

        float maxWidth = 0f;
        foreach (var stroke in Strokes)
        {
            if (stroke.StrokeWidth > maxWidth)
            {
                maxWidth = stroke.StrokeWidth;
            }
        }
        return maxWidth;
    }

    public void ForEachStroke(Action<NBStroke> action)
    {
        foreach (var stroke in Strokes)
        {
            action(stroke);
        }
    }
}
