using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// Shader 的基类 NBShader
/// </summary>
public class NBShader
{
    public virtual SKShader? CreateShader(NBDrawContext ctx, SKRect rect)
    {
        return null;
    }
}

/// <summary>
/// NBShader 的函数式实现
/// </summary>
public class NBFuncShader : NBShader
{
    public Func<NBDrawContext, SKRect, SKShader>? ShaderFunc1 { get; set; }
    public Func<SKRect, SKShader>? ShaderFunc2 { get; set; }

    public NBFuncShader(Func<NBDrawContext, SKRect, SKShader> shaderFunc)
    {
        ShaderFunc1 = shaderFunc;
    }

    public NBFuncShader(Func<SKRect, SKShader> shaderFunc)
    {
        ShaderFunc2 = shaderFunc;
    }

    public override SKShader? CreateShader(NBDrawContext ctx, SKRect rect)
    {
        if (ShaderFunc1 != null)
        {
            return ShaderFunc1(ctx, rect);
        }
        else if (ShaderFunc2 != null)
        {
            return ShaderFunc2(rect);
        }
        return base.CreateShader(ctx, rect);
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

    public override SKShader? CreateShader(NBDrawContext ctx, SKRect rect)
    {
        return base.CreateShader(ctx, rect);
    }
}