using SkiaSharp;
using System.Collections;

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

public class NBStrokeCollection : ICollection<NBStroke>
{
    public List<NBStroke> Strokes { get; private set; } = new List<NBStroke>();

    public int Count => ((ICollection<NBStroke>)Strokes).Count;

    public bool IsReadOnly => ((ICollection<NBStroke>)Strokes).IsReadOnly;

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

    public void Add(NBStroke item)
    {
        ((ICollection<NBStroke>)Strokes).Add(item);
    }

    public void Clear()
    {
        ((ICollection<NBStroke>)Strokes).Clear();
    }

    public bool Contains(NBStroke item)
    {
        return ((ICollection<NBStroke>)Strokes).Contains(item);
    }

    public void CopyTo(NBStroke[] array, int arrayIndex)
    {
        ((ICollection<NBStroke>)Strokes).CopyTo(array, arrayIndex);
    }

    public bool Remove(NBStroke item)
    {
        return ((ICollection<NBStroke>)Strokes).Remove(item);
    }

    public IEnumerator<NBStroke> GetEnumerator()
    {
        return ((IEnumerable<NBStroke>)Strokes).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Strokes).GetEnumerator();
    }
}
