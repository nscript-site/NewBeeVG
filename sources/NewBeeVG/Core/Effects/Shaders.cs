using SkiaSharp;

namespace NewBeeVG;

public static class Shaders
{
    public static NBShader AlphaLinearGradient(double p)
    { 
        return new NBRectAlphaLinearGradientShader([0, 0, 1, 1], [0 - 0.4f, ((float)p - 0.4f) / 0.6f, 0.1f + ((float)p - 0.4f) / 0.6f, 1 + 0.2f]); 
    }

    public static NBShader AlphaLinearGradient(SKPoint start, SKPoint end, float[] alphas, float[] positions)
    {
        Func<SKRect, (SKPoint start, SKPoint end)> func = _ => (start, end);
        return new NBAlphaLinearGradientShader(func,alphas,positions);
    }

    public static NBShader LinearGradientOnRect(SKColor[] colors, float[] positions, NBRectDirection direction = NBRectDirection.LeftToRight, SKShaderTileMode tile = SKShaderTileMode.Clamp)
    {
        return new NBRectLinearGradientShader(colors, positions, direction, tile);
    }

    public static NBShader AlphaLinearGradientOnRect(float[] alphas, float[] positions, NBRectDirection direction = NBRectDirection.LeftToRight, SKShaderTileMode tile = SKShaderTileMode.Clamp)
    {
        return new NBRectAlphaLinearGradientShader(alphas, positions, direction, tile);
    }

    public static NBShader RadialGradientOnRect(SKColor[] colors, float[] positions, SKShaderTileMode tile = SKShaderTileMode.Clamp)
    {
        return new NBRadialGradientShader(r => (r.Center, r.MaxRadius), colors, positions, tile);
    }

    public static NBShader FromTexture(SKBitmap bitmap, SKShaderTileMode tileX = SKShaderTileMode.Clamp, SKShaderTileMode tileY = SKShaderTileMode.Clamp)
    {
        return new NBBitmapShader((ctx, rect) => bitmap, tileX, tileY);
    }

    public static NBShader FromTexture(Func<NBDrawContext, SKRect, SKBitmap> bitmapFunc, SKShaderTileMode tileX = SKShaderTileMode.Clamp, SKShaderTileMode tileY = SKShaderTileMode.Clamp)
    {
        return new NBBitmapShader(bitmapFunc, tileX, tileY);
    }

    public static NBShader FromTexture(Func<SKRect, SKBitmap> bitmapFunc, SKShaderTileMode tileX = SKShaderTileMode.Clamp, SKShaderTileMode tileY = SKShaderTileMode.Clamp)
    {
        return new NBBitmapShader((ctx, rect) => bitmapFunc(rect), tileX, tileY);
    }
}

//public class NBAlphaLinearGradientShader : NBShader
//{
//    public NBRectDirection Direction { get; init; } = NBRectDirection.LeftToRight;

//    public override SKShader? CreateShader(NBDrawContext ctx, SKRect rect)
//    {
//        float v = (float)ctx.progress;
//        return SKShader.CreateAlphaLinearGradient(rect.LeftMiddle, rect.RightMiddle,
//            [0 - 0.4f, (v - 0.4f) / 0.6f, 0.1f + (v - 0.4f) / 0.6f, 1 + 0.2f],
//            [0, 0, 1, 1]);
//    }
//}

public class NBLinearGradientShader : NBShader
{
    public Func<SKRect, (SKPoint start, SKPoint end)>? Func { get; protected set; }

    public SKColor[] Colors { get; init; }

    public float[] Positions { get; init; }

    public SKShaderTileMode TileMode { get; init; }

    public NBLinearGradientShader(Func<SKRect, (SKPoint start, SKPoint end)>? func, SKColor[] colors, float[] positions,
        SKShaderTileMode tile = SKShaderTileMode.Clamp)
    {
        Func = func;
        Colors = colors;
        Positions = positions;
        TileMode = tile;
    }

    public override SKShader? CreateShader(NBDrawContext ctx, SKRect rect)
    {
        if(Func == null) return null;

        var pair = Func(rect);
        var shader = SKShader.CreateLinearGradient(pair.start, pair.end,
            Colors,
            Positions,
            TileMode);
        return shader;
    }
}

public class NBAlphaLinearGradientShader : NBLinearGradientShader
{
    public NBAlphaLinearGradientShader(Func<SKRect, (SKPoint start, SKPoint end)>? func, float[] alphas, float[] positions,
        SKShaderTileMode tile = SKShaderTileMode.Clamp)
        :base(func,ToColors(alphas),positions,tile)
    {
    }
}

public class NBRectLinearGradientShader : NBLinearGradientShader
{
    public NBRectDirection Direction { get; init; }

    public NBRectLinearGradientShader(SKColor[] colors, float[] positions,
        NBRectDirection direction = NBRectDirection.LeftToRight,
        SKShaderTileMode tile = SKShaderTileMode.Clamp)
        : base(null, colors, positions, tile)
    {
        this.Direction = direction;
        this.Func = rect => Direction.ToLine(rect);
    }
}

public class NBRectAlphaLinearGradientShader : NBLinearGradientShader
{
    public NBRectDirection Direction { get; init; }

    public NBRectAlphaLinearGradientShader(float[] alphas, float[] positions,
        NBRectDirection direction = NBRectDirection.LeftToRight,
        SKShaderTileMode tile = SKShaderTileMode.Clamp)
        : base(null, ToColors(alphas), positions, tile)
    {
        this.Direction = direction;
        this.Func = rect => Direction.ToLine(rect);
    }
}

public class NBRadialGradientShader : NBShader
{
    public Func<SKRect, (SKPoint center, float radius)>? Func { get; protected set; }

    public SKColor[] Colors { get; init; }

    public float[] Positions { get; init; }

    public SKShaderTileMode TileMode { get; init; }

    public NBRadialGradientShader(Func<SKRect, (SKPoint center, float radius)>? func, SKColor[] colors, float[] positions,  SKShaderTileMode tileMode = SKShaderTileMode.Clamp)
    {
        this.Func = func;
        this.Colors = colors;
        this.Positions = positions;
        this.TileMode = tileMode;
    }

    public override SKShader? CreateShader(NBDrawContext ctx, SKRect rect)
    {
        if (Func == null) return null;

        var circle = Func(rect);
        var shader = SKShader.CreateRadialGradient(circle.center, circle.radius,
            Colors,
            Positions,
            TileMode);
        return shader;
    }
}

public class NBAlphaRadialGradientShader : NBRadialGradientShader
{
    public NBAlphaRadialGradientShader(Func<SKRect, (SKPoint center, float radius)>? func, float[] alphas, float[] positions,
        SKShaderTileMode tile = SKShaderTileMode.Clamp)
        : base(func, ToColors(alphas), positions, tile)
    {
    }
}

public class NBRectRadialGradientShader : NBRadialGradientShader
{
    public NBRectRadialGradientShader(SKColor[] colors, float[] positions,
        SKShaderTileMode tile = SKShaderTileMode.Clamp)
        : base(null, colors, positions, tile)
    {
        this.Func = rect => (rect.Center, rect.MaxRadius);
    }
}