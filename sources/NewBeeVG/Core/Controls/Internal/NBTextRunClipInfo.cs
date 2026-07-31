using SkiaSharp;

namespace NewBeeVG;

internal class NBTextRunClipInfo
{
    public float Height { get; set; } = 0;
    public float Length { get; set; } = 0;
    public NBTextRun Run { get; set; } = default!;
    public String Text { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public bool RTL { get; set; }
    public Orientation Orientation { get; set; }
    public float DeltaX { get; set; }
    public float DeltaY { get; set; }

    public SKRect GetBound()
    {
        if (Orientation == Orientation.Horizontal)
            return new SKRect(X, Y, X + Length, Y + Height);
        else
            return new SKRect(X, Y, X + Height, Y + Length);
    }

    public void UpdateCrossAxis(NBTextAlign align, float maxHeight)
    {
        if(Orientation == Orientation.Horizontal)
        {
            if (align == NBTextAlign.Center)
                Y += (maxHeight - Height) / 2;
            else if (align == NBTextAlign.RightOrBottom)
                Y += maxHeight - Height;

        }
        else
        {
            if(RTL == false)
            {
                if (align == NBTextAlign.Center)
                    X += (maxHeight - Height) / 2;
                else if (align == NBTextAlign.RightOrBottom)
                    X += maxHeight - Height;
            }
            else
            {
                if (align == NBTextAlign.Center)
                    X -= (maxHeight - Height) / 2;
                else if (align == NBTextAlign.RightOrBottom)
                    X -= maxHeight - Height;
            }
        }
    }
}
