using SkiaSharp;

namespace NewBeeVG;

/// <summary>
/// Shader 的基类 NBShader
/// </summary>
public abstract class NBShader
{
    public abstract SKShader? CreateShader(NBDrawContext ctx, SKRect rect);

    protected static SKColor[] ToColors(float[] alphas)
    {
        SKColor[] colors = new SKColor[alphas.Length];
        for (int i = 0; i < alphas.Length; i++)
        {
            colors[i] = new SKColor(255, 255, 255, (byte)(alphas[i] * 255));
        }
        return colors;
    }
}

/// <summary>
/// Represents a collection of shaders that can be composed together to create a single shader.
/// </summary>
public class NBShaderCollection
{
    public List<NBShader> Shaders { get; private set; } = new List<NBShader>();

    public SKShader? ComposedShader { get; private set; }

    public void AddShader(NBShader shader)
    {
        Shaders.Add(shader);
    }

    public void ClearShaders()
    {
        Shaders.Clear();
    }

    public bool IsEmpty()
    {
        return Shaders.Count == 0;
    }

    public bool BuildComposeShader(SKRect inputBound)
    {
        var ctxRef = NBDrawContext.Current;
        if (ctxRef == null) return false;

        var ctx = ctxRef.Value;

        if (Shaders.Count == 0)
        {
            ComposedShader = null;
            return false;
        }
        else if (Shaders.Count == 1)
        {
            ComposedShader = Shaders[0].CreateShader(ctx, inputBound);
            return true;
        }
        else
        {
            var shaders = new List<SKShader>();
            foreach (var shaderFunc in Shaders)
            {
                var shader = shaderFunc.CreateShader(ctx, inputBound);
                if (shader != null)
                {
                    shaders.Add(shader);
                }
            }
            if (shaders.Count == 1)
            {
                ComposedShader = shaders[0];
                return true;
            }
            else if (shaders.Count > 1)
            {
                var f0 = shaders[0];
                for (int i = 1; i < shaders.Count; i++)
                {
                    f0 = SKShader.CreateCompose(f0, shaders[i]);
                }
                ComposedShader = f0;
                return true;
            }
            else
            {
                ComposedShader = null;
                return false;
            }
        }
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
        return null;
    }
}