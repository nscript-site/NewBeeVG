using SkiaSharp;
using System.Runtime.CompilerServices;

namespace NewBeeVG;

public static partial class NBExtentions
{
    extension(SKRect rect)
    {
        public SKPoint Center => new SKPoint(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
    }

    public static T Styles<T>(this T t, Action<T>[]? styles) where T : NBVisual
    {
        if (styles != null)
        {
            foreach (var style in styles)
            {
                style(t);
            }
        }
        return t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool HasAllFlags<T>(this T value, T flags) where T : unmanaged, Enum
    {
        if (sizeof(T) == 1)
        {
            var byteValue = Unsafe.As<T, byte>(ref value);
            var byteFlags = Unsafe.As<T, byte>(ref flags);
            return (byteValue & byteFlags) == byteFlags;
        }
        else if (sizeof(T) == 2)
        {
            var shortValue = Unsafe.As<T, short>(ref value);
            var shortFlags = Unsafe.As<T, short>(ref flags);
            return (shortValue & shortFlags) == shortFlags;
        }
        else if (sizeof(T) == 4)
        {
            var intValue = Unsafe.As<T, int>(ref value);
            var intFlags = Unsafe.As<T, int>(ref flags);
            return (intValue & intFlags) == intFlags;
        }
        else if (sizeof(T) == 8)
        {
            var longValue = Unsafe.As<T, long>(ref value);
            var longFlags = Unsafe.As<T, long>(ref flags);
            return (longValue & longFlags) == longFlags;
        }
        else
            throw new NotSupportedException("Enum with size of " + Unsafe.SizeOf<T>() + " are not supported");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool HasAnyFlag<T>(this T value, T flags) where T : unmanaged, Enum
    {
        if (sizeof(T) == 1)
        {
            var byteValue = Unsafe.As<T, byte>(ref value);
            var byteFlags = Unsafe.As<T, byte>(ref flags);
            return (byteValue & byteFlags) != 0;
        }
        else if (sizeof(T) == 2)
        {
            var shortValue = Unsafe.As<T, short>(ref value);
            var shortFlags = Unsafe.As<T, short>(ref flags);
            return (shortValue & shortFlags) != 0;
        }
        else if (sizeof(T) == 4)
        {
            var intValue = Unsafe.As<T, int>(ref value);
            var intFlags = Unsafe.As<T, int>(ref flags);
            return (intValue & intFlags) != 0;
        }
        else if (sizeof(T) == 8)
        {
            var longValue = Unsafe.As<T, long>(ref value);
            var longFlags = Unsafe.As<T, long>(ref flags);
            return (longValue & longFlags) != 0;
        }
        else
            throw new NotSupportedException("Enum with size of " + Unsafe.SizeOf<T>() + " are not supported");
    }
}
