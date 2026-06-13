/***********************
 * 代码修改自 avalonia (https://github.com/AvaloniaUI/Avalonia)
 * license: MIT
 ***********************/

using System.Runtime.CompilerServices;

namespace NewBeeVG;

internal struct MinMax
{
    public double MinWidth;
    public double MaxWidth;
    public double MinHeight;
    public double MaxHeight;

    public MinMax(NBLayoutable e)
    {
        (MinWidth, MaxWidth) = CalcMinMax(e.Width, e.MinWidth, e.MaxWidth);
        (MinHeight, MaxHeight) = CalcMinMax(e.Height, e.MinHeight, e.MaxHeight);
    }

    private static (double Min, double Max) CalcMinMax(double value, double min, double max)
    {
        double v0, v1;

        if (double.IsNaN(value))
        {
            v0 = 0.0;
            v1 = double.PositiveInfinity;
        }
        else
        {
            v0 = v1 = value;
        }

        max = ClampUnchecked(v1, min, max);
        min = ClampUnchecked(v0, min, max);

        return (min, max);
    }

    // Don't use Math.Clamp, it's possible for min to be greater than max here
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ClampUnchecked(double value, double min, double max)
    {
        if (value > max)
            value = max;

        if (value < min)
            value = min;

        return value;
    }

}
