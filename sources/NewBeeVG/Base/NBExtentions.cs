using SkiaSharp;

namespace NewBeeVG;

public static partial class NBExtentions
{
    extension(SKRect rect)
    {
        public SKPoint Center => new SKPoint(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
    }
}
