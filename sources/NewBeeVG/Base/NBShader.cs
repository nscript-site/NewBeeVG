using ShimSkiaSharp;

namespace NewBeeVG;

public class NBShader
{
    public SKRect Bound { get; init; }

    public virtual SKShader? CreateShader()
    {
        return null;
    }
}

public enum NBRectDirection
{
    TopToBottom,
    BottomToTop,
    LeftToRight,
    RightToLeft,
    Custom
}

public class NBAlphaLinearGradientShader : NBShader
{
    public NBRectDirection Direction { get; init; } = NBRectDirection.LeftToRight;

    public override SKShader? CreateShader()
    {
        return null;
    }
}