using HelixToolkit.Nex.Engine;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Graphics.Vulkan;
using HelixToolkit.Nex.Rendering;
using HelixToolkit.Nex.Scene;

namespace NewBeeVG.ThreeD;

public class NB3DContext
{
    private IContext? _ctx;
    private readonly Engine? _engine;
    private readonly WorldDataProvider? _worldDataProvider;
    private readonly Node? _root;

    public NB3DContext()
    {
        var vulkanConfig = new VulkanContextConfig
        {
            TerminateOnValidationError = true, // 调试打开，生产关闭
        };

        _ctx = VulkanBuilder.CreateHeadless(vulkanConfig);
        _engine = EngineBuilder.Create(_ctx).WithDefaultNodes(false)
            .WithSMAA()
            .WithBloom()
            .RenderToCustomTarget(Format.RGBA_UN8)
            .Build();

        _worldDataProvider = _engine.CreateWorldDataProvider();
        _worldDataProvider.Initialize();
    }
}
