using HelixToolkit.Nex;
using HelixToolkit.Nex.ECS;
using HelixToolkit.Nex.Engine;
using HelixToolkit.Nex.Engine.CameraControllers;
using HelixToolkit.Nex.Engine.Cameras;
using HelixToolkit.Nex.Engine.Components;
using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Graphics;
using HelixToolkit.Nex.Graphics.Vulkan;
using HelixToolkit.Nex.Material;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Rendering;
using HelixToolkit.Nex.Rendering.Components;
using HelixToolkit.Nex.Scene;
using HelixToolkit.Nex.Shaders;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Numerics;
using static HelixToolkit.Nex.Rendering.PostEffects.BorderHighlightPostEffect;

namespace NewBeeVG.ThreeD;

/// <summary>
/// Comprehensive point cloud rendering demo with:
/// - Multiple point cloud entities (sphere, helix, random cluster, animated wave)
/// - GPU picking (click a point to select its entity)
/// - ImGui controls for point size, colour, min screen size, add/remove clouds
/// - Orbit camera with keyboard (WASD) fallback
/// </summary>
public sealed class NB3DPointsDemo : IDisposable
{
    private static readonly ILogger _logger = LogManager.Create<NB3DPointsDemo>();
    private const string ViewportTextureName = "ViewportTexture";

    private readonly IContext _context;
    private Engine? _engine;
    private RenderContext? _renderContext;
    private WorldDataProvider? _worldDataProvider;
    private Node? _root;

    // Camera
    private Camera _camera = new PerspectiveCamera();
    private OrbitCameraController? _orbitController;
    private long _lastTimestamp;

    private Size _viewportSize = new(1, 1);

    // Scene entities
    private readonly List<PointCloudEntry> _pointClouds = [];
    private Entity _selectedEntity = Entity.Null;
    private int _pickedEntityId;
    private uint _pickedInstanceIdx;

    // Global tunables
    private float _globalPointSize = 0.1f;
    private float _animTime;
    private bool _fixedSize = false;

    private TextureResource? _renderTexture;
    private Size ViewportSize;

    // Custom point material types registered by this demo
    private string[] _materialTypes = [];

    public NB3DPointsDemo()
    {
        var vulkanConfig = new VulkanContextConfig
        {
            TerminateOnValidationError = true, // 调试打开，生产关闭
        };
        _context = VulkanBuilder.CreateHeadless(vulkanConfig);
        _engine = EngineBuilder.Create(_context).WithDefaultNodes(false)
            .WithSMAA()
            .WithBloom()
            .RenderToCustomTarget(Format.RGBA_UN8)
            .Build();
    }

    // ------------------------------------------------------------------
    // Initialization
    // ------------------------------------------------------------------

    public void Initialize(int width, int height)
    {
        _camera = new PerspectiveCamera
        {
            Position = new Vector3(0, 12, -25),
            Target = Vector3.Zero,
            FarPlane = 500,
        };
        _orbitController = new OrbitCameraController(_camera);

        // Register custom point material shaders before building the engine
        RegisterCustomPointMaterials();

        ViewportSize = new Size(width, height);
        //ViewportSize = new Size(1,1);

        // Build the engine with the point rendering node
        _engine = EngineBuilder
            .Create(_context)
            .WithDefaultNodes()
            .WithFPS()
            .RenderToCustomTarget(GraphicsSettings.IntermediateTargetFormat)
            .Build();
        _renderContext = _engine.CreateRenderContext();
        _renderContext.Initialize();
        _renderContext.WindowSize = ViewportSize;
        _renderContext.ResourceSet.AddTexture(
            ViewportTextureName,
            res =>
            {
                _renderTexture = _context.CreateRenderTarget2D(
                    Format.BGRA_UN8,
                    (uint)_renderContext.WindowSize.Width,
                    (uint)_renderContext.WindowSize.Height,
                    debugName: ViewportTextureName
                );
                return _renderTexture;
            }, 
            dependsOnScreenSize:false
        );

        _worldDataProvider = _engine.CreateWorldDataProvider();
        _worldDataProvider.Initialize();

        // Build the scene
        BuildScene();
    }

    private void DownloadRenderedImage()
    {
        if (_renderTexture == null) return;


    }

    // ------------------------------------------------------------------
    // Custom point material registration
    // ------------------------------------------------------------------

    /// <summary>
    /// Registers several custom point material shaders to demonstrate the
    /// <see cref="PointMaterialRegistry"/> extensibility.
    /// </summary>
    private void RegisterCustomPointMaterials()
    {
        var materials = new List<string>();
        // Collect the built-in default first
        materials.Add("Default");

        // 1. Square — no SDF, simple axis-aligned square
        PointMaterialRegistry.Register(
            name: "Square",
            getPointColorImpl: """
                // Flat square – no distance discard, just vertex color
                vec4 color = getColor();
                if (v_textureIndex > 0u) {
                    vec2 texUv = getUV() * 0.5 + 0.5;
                    color *= textureBindless2D(getTextureId(), getSamplerId(), texUv);
                }
                return color;
            """
        );
        materials.Add("Square");

        // 2. Diamond — rotated square clipped to a diamond shape, hue shifts over time
        var diamondId = PointMaterialRegistry.Register(
            name: "Diamond",
            getPointColorImpl: """
                vec2 uv = abs(getUV());
                float d = uv.x + uv.y;
                if (d > 1.0) discard;

                float timeMs = float(getTimeMs() % 10000) / 1000;
                float edgeWidth = 2.0 / max(getPointSize(), 1.0);
                float alpha = 1.0 - smoothstep(1.0 - edgeWidth, 1.0, d);
                vec4 color = getColor();
                // Cycle hue over time: rotate RGB channels
                float shift = fract(timeMs * 0.15);
                color.rgb = mix(color.rgb, color.gbr, shift);
                color.a *= alpha;
                return color;
            """
        );
        materials.Add("Diamond");

        // 3. Ring — hollow circle with time-animated thickness
        var ringId = PointMaterialRegistry.Register(
            name: "Ring",
            getPointColorImpl: """
                float dist = length(getUV());
                if (dist > 1.0) discard;

                float timeMs = float(getTimeMs() % 100000) / 1000;
                // Animate thickness between 0.15 and 0.35
                float thickness = 0.25 + 0.10 * sin(timeMs * 2.0);
                float inner = 1.0 - thickness;
                if (dist < inner) discard;

                float edgeWidth = 2.0 / max(getPointSize(), 1.0);
                float alpha = smoothstep(inner - edgeWidth, inner, dist)
                            * (1.0 - smoothstep(1.0 - edgeWidth, 1.0, dist));
                vec4 color = getColor();
                // Tint toward bright cyan at peak thickness
                float blend = 0.5 + 0.5 * sin(timeMs * 2.0);
                color.rgb = mix(color.rgb, vec3(0.2, 0.9, 1.0), blend * 0.4);
                color.a *= alpha;
                return color;
            """
        );
        materials.Add("Ring");

        // 4. Pulsing — animated pulse using getTimeMs()
        var pulsingId = PointMaterialRegistry.Register(
            name: "Pulsing",
            getPointColorImpl: """
                float dist = dot(getUV(), getUV());
                if (dist > 1.0) discard;

                float timeMs = float(getTimeMs() % 100000) / 1000;
                float pulse = 0.5 + 0.5 * sin(timeMs * 4.0 + dist * 6.0);
                float edgeWidth = 2.0 / max(getPointSize(), 1.0);
                float alpha = 1.0 - smoothstep(1.0 - edgeWidth, 1.0, dist);

                vec4 color = getColor();
                color.rgb *= (0.5 + 0.5 * pulse);
                color.a *= alpha;
                return color;
            """
        );
        materials.Add("Pulsing");

        // 5. Gradient Disc — radial gradient with time-animated center color
        var gradientDiscId = PointMaterialRegistry.Register(
            name: "GradientDisc",
            getPointColorImpl: """
                float dist = length(getUV());
                if (dist > 1.0) discard;

                float timeMs = float(getTimeMs() % 10000) / 1000;
                // Cycle center color through warm tones over time
                vec4 centerColor = vec4(
                    0.8 + 0.2 * sin(timeMs * 1.5),
                    0.6 + 0.4 * sin(timeMs * 1.0 + 1.0),
                    0.3 + 0.3 * sin(timeMs * 0.7 + 2.0),
                    1.0
                );
                vec4 edgeColor = getColor();
                vec4 color = mix(centerColor, edgeColor, dist);

                float edgeWidth = 2.0 / max(getPointSize(), 1.0);
                color.a *= 1.0 - smoothstep(1.0 - edgeWidth, 1.0, dist);
                return color;
            """
        );
        materials.Add("GradientDisc");

        _materialTypes = materials.ToArray();

        _logger.LogInformation(
            "Registered {Count} custom point material types.",
            _materialTypes.Length - 1
        );
    }

    // ------------------------------------------------------------------
    // Scene building helpers
    // ------------------------------------------------------------------

    private void BuildScene()
    {
        var world = _worldDataProvider!.World;
        _root = new Node(world) { Name = "Root" };

        // Add a directional light so the viewport isn't pure black if someone
        // toggles on a mesh later.
        //var lightNode = new Node(world) { Name = "DirectionalLight" };
        //lightNode.Entity.Set(
        //    new DirectionalLightInfo
        //    {
        //        Light = new DirectionalLight
        //        {
        //            Direction = Vector3.Normalize(new Vector3(0.5f, -1f, 0.5f)),
        //            Color = new Vector3(1f, 0.98f, 0.95f),
        //            Intensity = 0.8f,
        //        },
        //    }
        //);
        //_root.AddChild(lightNode);

        //// 1. Sphere point cloud — default circle SDF
        //AddPointCloud(
        //    "Sphere",
        //    GenerateSphere(5_000, 5f, Vector3.Zero),
        //    new Color4(0.2f, 0.7f, 1.0f, 1.0f)
        //);

        //// 2. Helix point cloud — Diamond shader
        //AddPointCloud(
        //    "Helix",
        //    GenerateHelix(3_000, 4f, 10f, 3, new Vector3(15, 0, 0)),
        //    new Color4(1.0f, 0.4f, 0.2f, 1.0f),
        //    "Diamond"
        //);

        //// 3. Random cluster — Ring shader
        //AddPointCloud(
        //    "Random Cluster",
        //    GenerateRandomCluster(8_000, 6f, new Vector3(-15, 3, 0)),
        //    new Color4(0.3f, 1.0f, 0.3f, 1.0f),
        //    "Ring"
        //);

        //// 4. Animated wave — Pulsing shader
        //AddPointCloud(
        //    "Animated Wave",
        //    GenerateWave(4_000, 10f, 10f, 0f, new Vector3(0, -5, 15)),
        //    new Color4(1.0f, 0.9f, 0.2f, 1.0f),
        //    "Pulsing"
        //);
    }

    private void AddPointCloud(
        string name,
        Geometry geo,
        Color4 tint,
        string materialName = "Default"
    )
    {
        var world = _worldDataProvider!.World;
        var node = world.CreatePointCloudNode(name);
        _root!.AddChild(node);

        // Snapshot the original (untinted) colors
        var originalColors = new List<Vector4>(geo.VertexColors.Count);
        for (int i = 0; i < geo.VertexColors.Count; i++)
            originalColors.Add(geo.VertexColors[i]);

        // Apply tint to all points
        for (int i = 0; i < geo.VertexColors.Count; i++)
        {
            var c = originalColors[i];
            geo.VertexColors[i] = new Vector4(
                c.X * tint.Red,
                c.Y * tint.Green,
                c.Z * tint.Blue,
                c.W * tint.Alpha
            );
        }

        node.Geometry = geo;
        node.Color = Color4.White;
        node.FixedSize = _fixedSize;
        node.PointMaterialName = materialName;
        node.Size = _globalPointSize;

        _pointClouds.Add(
            new PointCloudEntry(
                name,
                node,
                geo,
                tint,
                originalColors,
                Array.IndexOf(_materialTypes, materialName)
            )
        );
        _engine!.Add(geo);
    }

    // ------------------------------------------------------------------
    // Point generators
    // ------------------------------------------------------------------

    private Geometry GenerateSphere(int count, float radius, Vector3 center)
    {
        var geo = new Geometry();
        geo.Vertices.Capacity = count;
        geo.VertexColors.Capacity = count;
        var rng = new Random(42);
        for (int i = 0; i < count; i++)
        {
            // Uniform sphere distribution using rejection sampling
            Vector3 p;
            do
            {
                p = new Vector3(
                    (float)(rng.NextDouble() * 2 - 1),
                    (float)(rng.NextDouble() * 2 - 1),
                    (float)(rng.NextDouble() * 2 - 1)
                );
            } while (p.LengthSquared() > 1f || p.LengthSquared() < 0.001f);

            p = Vector3.Normalize(p) * radius;
            float brightness = 0.5f + 0.5f * (p.Y / radius); // gradient by height
            geo.Vertices.Add((center + p).ToVector4(1));
            geo.VertexColors.Add(new Vector4(brightness, brightness, 1f, 1f));
        }
        return geo;
    }

    private Geometry GenerateHelix(int count, float radius, float height, int turns, Vector3 center)
    {
        var geo = new Geometry();
        geo.Vertices.Capacity = count;
        geo.VertexColors.Capacity = count;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float angle = t * turns * MathF.PI * 2f;
            float y = t * height - height * 0.5f;
            float r = radius * (0.5f + 0.5f * MathF.Sin(t * MathF.PI)); // taper at ends
            geo.Vertices.Add(
                (center + new Vector3(MathF.Cos(angle) * r, y, MathF.Sin(angle) * r)).ToVector4(1)
            );
            geo.VertexColors.Add(new Vector4(t, 0.5f, 1f - t, 1f));
        }
        return geo;
    }

    private Geometry GenerateRandomCluster(int count, float extent, Vector3 center)
    {
        var geo = new Geometry();
        var rng = new Random(123);
        for (int i = 0; i < count; i++)
        {
            // Gaussian-ish distribution via sum of uniform randoms
            float Gauss() =>
                (float)(rng.NextDouble() + rng.NextDouble() + rng.NextDouble()) / 3f * 2f - 1f;
            var p = new Vector3(Gauss(), Gauss(), Gauss()) * extent;
            float d = p.Length() / extent;
            geo.Vertices.Add((center + p).ToVector4(1));
            geo.VertexColors.Add(new Vector4(0.5f + d * 0.5f, 1f - d, 0.3f, 1f));
        }
        return geo;
    }

    private Geometry GenerateWave(
        int countSqrt,
        float width,
        float depth,
        float time,
        Vector3 center,
        Geometry? cache = null
    )
    {
        int count = countSqrt;
        // Generate as a grid, sqrt(count) x sqrt(count)
        int side = (int)MathF.Sqrt(count);
        var geo = cache ?? new Geometry(isDynamic: true);
        geo.Vertices.Resize(side * side);
        geo.VertexColors.Resize(side * side);
        for (int iz = 0; iz < side; iz++)
        {
            for (int ix = 0; ix < side; ix++)
            {
                float u = (float)ix / (side - 1) - 0.5f;
                float v = (float)iz / (side - 1) - 0.5f;
                float x = u * width;
                float z = v * depth;
                float dist = MathF.Sqrt(x * x + z * z);
                float y = MathF.Sin(dist * 2f - time * 3f) * 1.5f * MathF.Exp(-dist * 0.15f);
                float hue = (MathF.Sin(dist * 0.5f - time) + 1f) * 0.5f;
                geo.Vertices[iz * side + ix] = ((center + new Vector3(x, y, z)).ToVector4(1));
                geo.VertexColors[iz * side + ix] = (
                    new Vector4(hue, 0.6f + 0.4f * (1f - hue), 1f - hue * 0.5f, 1f)
                );
            }
        }
        geo.MarkDirty(GeometryBufferType.Vertex | GeometryBufferType.VertexColor);
        return geo;
    }

    // ------------------------------------------------------------------
    // Render loop
    // ------------------------------------------------------------------

    public void Render()
    {
        if (_engine is null || _renderContext is null || _worldDataProvider is null)
            return;

        // Delta time
        if (_lastTimestamp == 0)
            _lastTimestamp = Stopwatch.GetTimestamp();
        float dt = (float)(Stopwatch.GetTimestamp() - _lastTimestamp) / Stopwatch.Frequency;
        _lastTimestamp = Stopwatch.GetTimestamp();
        _animTime += dt;

        //_orbitController?.Update(dt);

        // Animate the wave point cloud
        //UpdateWave();

        _renderContext.Update(_camera);

        // 3D render (offscreen)
        _engine.BeginFrame();

        var cmdBuf = _engine.RenderOffscreen(
            _renderContext,
            _worldDataProvider,
            ViewportTextureName
        );

        _engine.Submit(cmdBuf, TextureHandle.Null);
        _engine.WaitForIdle();
    }

    private void UpdateWave()
    {
        // The "Animated Wave" is the 4th point cloud (index 3)
        if (_pointClouds.Count < 4)
            return;
        var entry = _pointClouds[3];
        var newPts = GenerateWave(
            entry.Points.Vertices.Count,
            10f,
            10f,
            _animTime,
            new Vector3(0, -5, 15),
            entry.Points
        );

        // Update the stored original colors from the freshly generated wave
        var origColors = entry.OriginalColors;
        if (origColors.Count != newPts.VertexColors.Count)
        {
            origColors.Clear();
            origColors.Capacity = newPts.VertexColors.Count;
            for (int i = 0; i < newPts.VertexColors.Count; i++)
                origColors.Add(newPts.VertexColors[i]);
        }
        else
        {
            for (int i = 0; i < newPts.VertexColors.Count; i++)
                origColors[i] = newPts.VertexColors[i];
        }

        // Apply tint from the original colors
        for (int i = 0; i < newPts.VertexColors.Count; i++)
        {
            var orig = origColors[i];
            newPts.VertexColors[i] = new Vector4(
                orig.X * entry.Tint.Red,
                orig.Y * entry.Tint.Green,
                orig.Z * entry.Tint.Blue,
                orig.W * entry.Tint.Alpha
            );
        }
        entry.Points = newPts;
    }

    // ------------------------------------------------------------------
    // GPU Picking
    // ------------------------------------------------------------------

    private void Pick(int x, int y)
    {
        if (_renderContext?.ResourceSet is null || _worldDataProvider is null)
            return;
        _engine!.CreatePickingRequest(
            _renderContext,
            new Vector2(x, y),
            response =>
            {
                // Deselect previous
                if (_selectedEntity.Valid)
                    _selectedEntity.Remove<BorderHighlightOverlay>();
                if (response.TryGetPickingResult(out var result))
                {
                    _pickedEntityId = (int)result.Entity.Id;
                    _pickedInstanceIdx = result.InstanceId;

                    Debug.Assert(
                        _worldDataProvider.World.Id == result.Entity.WorldId,
                        "Picked world ID does not match current world"
                    );
                    _selectedEntity = result.Entity;

                    if (_selectedEntity.Valid)
                    {
                        _selectedEntity.Set(BorderHighlightOverlay.Default);
                    }
                }
            }
        );
    }

    //private void DrawUI(TextureHandle offscreenTex, float displayW, float displayH)
    //{
    //}

    private void ApplyGlobalPointSize()
    {
        foreach (var entry in _pointClouds)
        {
            entry.Node.Entity.Update<PointDrawInfo>(
                (ref PointDrawInfo x) =>
                {
                    x.FixedSize = _fixedSize;
                    x.PointSize = _globalPointSize;
                }
            );
        }
    }

    private void ApplyTint(PointCloudEntry entry)
    {
        // Re-derive tinted colors from the stored originals
        for (int i = 0; i < entry.Points.VertexColors.Count; i++)
        {
            var orig = entry.OriginalColors[i];
            entry.Points.VertexColors[i] = new Vector4(
                orig.X * entry.Tint.Red,
                orig.Y * entry.Tint.Green,
                orig.Z * entry.Tint.Blue,
                orig.W * entry.Tint.Alpha
            );
        }
        entry.Points.MarkDirty(GeometryBufferType.VertexColor);
    }

    // ------------------------------------------------------------------
    // Camera input forwarding
    // ------------------------------------------------------------------

    public void OnKeyboardInput(bool w, bool s, bool a, bool d, bool space, bool ctrl, bool shift)
    {
        // Orbit controller doesn't use keyboard, but reserve for future FP mode
    }

    // ------------------------------------------------------------------
    // Dispose
    // ------------------------------------------------------------------

    private bool _disposed;

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _worldDataProvider?.Dispose();
            _renderContext?.Teardown();
            _engine?.Dispose();
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
        var demo = new NB3DPointsDemo();
        demo.Initialize(300, 400);
        demo.Render();
    }
}

/// <summary>
/// Tracks a single point cloud entity for the demo UI.
/// </summary>
internal sealed class PointCloudEntry
{
    public string Name { get; }
    public Node Node { get; }
    public Geometry Points { get; set; }
    public Color4 Tint { get; set; }

    public int MaterialNameIndex;

    /// <summary>
    /// Stores the untinted (original) vertex colors so that tint can be
    /// re-applied non-destructively any number of times.
    /// </summary>
    public List<Vector4> OriginalColors { get; set; }

    public PointCloudEntry(
        string name,
        Node node,
        Geometry points,
        Color4 tint,
        List<Vector4> originalColors,
        int materialNameIndex
    )
    {
        Name = name;
        Node = node;
        Points = points;
        Tint = tint;
        OriginalColors = originalColors;
        MaterialNameIndex = materialNameIndex;
    }
}
