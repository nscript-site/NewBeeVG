using HelixToolkit.Nex;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Maths;
using System.Diagnostics;
using System.Numerics;

namespace HelixNexDemo;

internal class HelloTriangleApp : IDisposable
{
    private const string Vs = """
        #version 460
        layout (location=0) out vec3 color;
        const vec2 pos[3] = vec2[3](
            vec2(-0.6, -0.4),
            vec2( 0.6, -0.4),
            vec2( 0.0,  0.6)
        );
        const vec3 col[3] = vec3[3](
            vec3(1.0, 0.0, 0.0),
            vec3(0.0, 1.0, 0.0),
            vec3(0.0, 0.0, 1.0)
        );

        layout (push_constant) uniform constants {
            mat4 transform;
        } PushConstants;

        void main() {
            vec4 pos = vec4(pos[gl_VertexIndex], 0.0, 1) * PushConstants.transform;
            gl_Position = pos;
            color = col[gl_VertexIndex];
        }
        """;

    private const string Ps = """
        #version 460
        layout (location=0) in vec3 color;
        layout (location=0) out vec4 out_FragColor;

        void main() {
        	out_FragColor = vec4(color, 1.0);
        };
        """;

    private IContext? _ctx;
    private RenderPipelineResource _renderPipeline = RenderPipelineResource.Null;
    private readonly RenderPass _pass = new();
    private readonly Framebuffer _frameBuffer = new();
    private NBTexture2D Canvas;

    public HelloTriangleApp(int width, int height)
    {
        _ctx = NB3D.CreateHeadlessContext();
        Canvas = NB3D.CreateTexture2D(_ctx, width, height);

        _ctx.CreateShaderModuleGlsl(Vs, ShaderStage.Vertex, out var vsModule).CheckResult();
        _ctx.CreateShaderModuleGlsl(Ps, ShaderStage.Fragment, out var psModule).CheckResult();
        var pipelineDesc = new RenderPipelineDesc
        {
            VertexShader = vsModule,
            FragmentShader = psModule,
            Topology = Topology.Triangle,
        };
        pipelineDesc.Colors[0].Format = Format.BGRA_SRGB8;
        _ctx.CreateRenderPipeline(pipelineDesc, out _renderPipeline).CheckResult();

        _pass.Colors[0] = new RenderPass.AttachmentDesc
        {
            ClearColor = new Color4(0.1f, 0.2f, 0.3f, 1.0f),
            StoreOp = StoreOp.Store,
            LoadOp = LoadOp.Clear,
        };
    }

    public void Render()
    {
        Debug.Assert(_ctx != null, "Vulkan context should not be null at this point.");

        var cmdBuffer = _ctx!.AcquireCommandBuffer();
        _frameBuffer.Colors[0].Texture = Canvas!.Texture;
        _pass.Colors[0].ClearColor = new Color4((0 % 1000) / 1000f, 0.2f, 0.3f, 1.0f);
        cmdBuffer.BeginRendering(_pass, _frameBuffer, Dependencies.Empty);
        cmdBuffer.BindRenderPipeline(_renderPipeline);
        var aspect = Canvas!.Aspect;
        var transform = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, (2000) / 1000f);
        var cam =
            Matrix4x4.CreateLookAt(
                (-Vector3.UnitZ * 10).TransformCoordinate(transform),
                Vector3.Zero,
                Vector3.UnitY
            ) * Matrix4x4.CreateOrthographic(2, 2 * aspect, 0.1f, 100f);

        cmdBuffer.PushConstants(cam);
        cmdBuffer.Draw(3); // Draw 3 vertices (a triangle)
        cmdBuffer.EndRendering();
        _ctx!.Submit(cmdBuffer,true);
    }

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _renderPipeline.Dispose();
            Canvas?.Dispose();
            _ctx?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public static void Run()
    {
        var demo = new HelloTriangleApp(800, 600);
        demo.Render();
        demo.Canvas?.Save("output_triangle.bmp");
    }
}
