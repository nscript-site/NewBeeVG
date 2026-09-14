using SkiaSharp;

namespace NewBeeVG;

public abstract class NBFrameMask
{
    public abstract SKBitmap? BuildMaskBitmap(NBDrawContext ctx, SKRect rect);

    public SKBlendMode FrameMaskBlendMode { get; set; } = SKBlendMode.SrcOut;
}

public class NBBitmapFrameMask : NBFrameMask
{
    public Func<NBDrawContext, SKRect, SKBitmap> BitmapFunc { get; init; }
    public NBBitmapFrameMask(Func<NBDrawContext, SKRect, SKBitmap> bitmapFunc)
    {
        BitmapFunc = bitmapFunc;
    }
    public override SKBitmap? BuildMaskBitmap(NBDrawContext ctx, SKRect rect)
    {
        return BitmapFunc(ctx, rect);
    }
}

public class NBShaderFrameMask : NBFrameMask
{
    public Func<NBDrawContext, SKRect, NBShader> ShanderFunc { get; init; }
    public NBShaderFrameMask(Func<NBDrawContext, SKRect, NBShader> shaderFunc)
    {
        ShanderFunc = shaderFunc;
    }

    public override SKBitmap? BuildMaskBitmap(NBDrawContext ctx, SKRect rect)
    {
        var shader = ShanderFunc(ctx, rect);
        var bmp = new SKBitmap((int)rect.Width, (int)rect.Height);
        using var canvas = new SKCanvas(bmp);
        using (var paint = new SKPaint { Shader = shader.CreateShader(ctx, rect), IsAntialias = true })
        {
            canvas.DrawRect(new SKRect(0,0,rect.Width,rect.Height), paint);
        }
        return bmp;
    }
}