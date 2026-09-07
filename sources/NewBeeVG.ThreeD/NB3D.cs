using HelixToolkit.Nex.Engine;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Graphics.Vulkan;
using HelixToolkit.Nex.Maths;

namespace NewBeeVG.ThreeD;

public class NB3D
{
    public static IContext CreateHeadlessContext()
    {
        var vulkanConfig = new VulkanContextConfig
        {
            TerminateOnValidationError = true, // 调试打开，生产关闭
        };
        var ctx = VulkanBuilder.CreateHeadless(vulkanConfig);
        return ctx;
    }

    public static NBTexture2D CreateTexture2D(IContext ctx, int width, int height, bool owned = true)
    {
        return new NBTexture2D(ctx, width, height, owned);
    }

    public static Engine CreateEngine(IContext ctx)
    {
        var engine = NB3DEngineBuilder.Create(ctx)
         .WithDefaultNodes()
         .WithSMAA()
         .WithBloom()
         .RenderToCustomTarget(Format.BGRA_UN8)
         .Build();
        return engine;
    }

    public static NBEngine CreateEngine(int width, int height, Color? backgroundColor = null)
    {
        return new NBEngine(width, height, backgroundColor);
    }
}
