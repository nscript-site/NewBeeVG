using SkiaSharp;

namespace NewBeeVG;

public static class Filters
{
    public static NBImageFilter Blur(double sigmaX, double sigmaY, SKShaderTileMode tileMode = SKShaderTileMode.Decal)
    {
        var filter = SKImageFilter.CreateBlur((float)sigmaX, (float)sigmaY, tileMode);
        return new NBSimpleImageFilter(filter);
    }

    public static NBImageFilter DropShadow(SKColor color, double dx, double dy, double sigmaX, double sigmaY)
    {
        var filter = SKImageFilter.CreateDropShadow((float)dx, (float)dy, (float)sigmaX, (float)sigmaY, color);
        return new NBSimpleImageFilter(filter);
    }
}
