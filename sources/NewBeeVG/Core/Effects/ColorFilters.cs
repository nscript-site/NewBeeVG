using SkiaSharp;

namespace NewBeeVG;

public static class ColorFilters
{
    public static NBColorFilter Gray(float[]? grayMat = null)
    {
        grayMat ??= new float[] {
                        0.299f,0.587f,0.114f,0,0,
                        0.299f,0.587f,0.114f,0,0,
                        0.299f,0.587f,0.114f,0,0,
                        0,0,0,1,0
                    };

        var filterGray = SKColorFilter.CreateColorMatrix(grayMat);
        return new NBSimpleColorFilter(filterGray);
    }

    public static NBColorFilter FromColorMatrix(float[] mat)
    {
        var filter = SKColorFilter.CreateColorMatrix(mat);
        return new NBSimpleColorFilter(filter);
    }
}
