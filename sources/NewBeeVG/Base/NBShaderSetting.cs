using SkiaSharp;

namespace NewBeeVG;

public class NBShaderSetting
{
    public List<Func<SKRect, SKShader>> ShaderFuncs { get; private set; } = new List<Func<SKRect, SKShader>>();

    public SKRect? InputBound { get; private set; }

    public SKShader? Shader { get; private set; }

    public void AddShaderFunc(Func<SKRect, SKShader> shaderFunc)
    {
        ShaderFuncs.Add(shaderFunc);
    }

    public void ClearShaderFuncs()
    {
        ShaderFuncs.Clear();
    }

    public bool IsEmpty()
    {
        return ShaderFuncs.Count == 0;
    }

    public bool UpdateShader(SKRect inputBound)
    {
        InputBound = inputBound;

        if (ShaderFuncs.Count == 0)
        {
            Shader = null;
            return false;
        }
        else if (ShaderFuncs.Count == 1)
        {
            Shader = ShaderFuncs[0](inputBound);
            return true;
        }
        else
        {
            var shaders = new List<SKShader>();
            foreach (var shaderFunc in ShaderFuncs)
            {
                var shader = shaderFunc(inputBound);
                if (shader != null)
                {
                    shaders.Add(shader);
                }
            }
            if (shaders.Count == 1)
            {
                Shader = shaders[0];
                return true;
            }
            else if (shaders.Count > 1)
            {
                var f0 = shaders[0];
                for (int i = 1; i < shaders.Count; i++)
                {
                    f0 = SKShader.CreateCompose(f0, shaders[i]);
                }
                Shader = f0;
                return true;
            }
            else
            {
                Shader = null;
                return false;
            }
        }
    }
}
