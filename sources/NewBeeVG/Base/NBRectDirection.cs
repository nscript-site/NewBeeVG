using SkiaSharp;

namespace NewBeeVG;

public enum NBRectDirection
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft,
    Custom
}

public static partial class NBExtentions
{
    public static (SKPoint start, SKPoint end) ToLine(this NBRectDirection self, SKRect rect)
    {
        return self switch
        {
            NBRectDirection.LeftToRight => (rect.LeftMiddle, rect.RightMiddle),
            NBRectDirection.RightToLeft => (rect.RightMiddle, rect.LeftMiddle),
            NBRectDirection.BottomToTop => (rect.BottomMiddle, rect.TopMiddle),
            NBRectDirection.TopToBottom => (rect.TopMiddle, rect.BottomMiddle),
            _ => (rect.LeftMiddle, rect.RightMiddle)
        };
    }
}
