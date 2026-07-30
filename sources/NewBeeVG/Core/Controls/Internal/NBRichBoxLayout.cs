using SkiaSharp;

namespace NewBeeVG;

internal class NBRichTextLineInfo
{
    public float Height { get; set; } = 0;
    public float Length { get; set; } = 0;
    public List<NBTextRunClipInfo> Clips { get; set; } = new List<NBTextRunClipInfo>();
}

internal class NBRichBoxLineLayoutInfo
{
    public SKRect Bound { get; set; }
    public List<NBRichTextLineInfo> Lines { get; set; } = new List<NBRichTextLineInfo>();
}

internal class NBRichBoxLayout
{
    NBRichText Owner;
    public NBRichBoxLayout(NBRichText owner)
    {
        Owner = owner;
    }

    internal NBRichBoxLineLayoutInfo Measure(Size availableSize, Thickness padding)
    {
        if (Owner.Orientation == Orientation.Horizontal)
        {
            return MeasureHorizontal(availableSize, padding);
        }
        else
        {
            return MeasureVertical(availableSize, padding);
        }
    }

    internal NBRichBoxLineLayoutInfo MeasureHorizontal(Size availableSize, Thickness padding)
    {
        return new NBRichBoxLineLayoutInfo();
    }

    internal NBRichBoxLineLayoutInfo MeasureVertical(Size availableSize, Thickness padding)
    {
        return new NBRichBoxLineLayoutInfo();
    }
}
