using HelixToolkit.Nex;
using HelixToolkit.Nex.ECS;
using HelixToolkit.Nex.Engine;
using HelixToolkit.Nex.Engine.Cameras;
using HelixToolkit.Nex.Engine.Scene;
using HelixToolkit.Nex.Material;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Rendering;
using HelixToolkit.Nex.Rendering.ComputeNodes;
using HelixToolkit.Nex.Rendering.SDF;
using HelixToolkit.Nex.Scene;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace HelixNexDemo;

public class BillboardApp : BaseEngineApp
{
    private static readonly ILogger _logger = LogManager.Create<BillboardApp>();
    // SDF font atlas
    private SDFFontAtlas? _atlas;

    // Global tunables
    private float _globalFontSize = 0.5f;
    private float _minScreenSize = 1f;
    private bool _fixedSize;

    // New text input state
    private string _newText = "New Text";
    private Vector3 _newPosition = new(0, -3, 0);
    private Vector4 _newColor = new(1f, 1f, 1f, 1f);

    private BillboardCullNode? _billboardCullNode;

    // Registered SDF material variant IDs
    private MaterialTypeId _outlineMaterialId;
    private MaterialTypeId _shadowMaterialId;

    // Solar system scene
    private BillboardShowcaseScene? _scene;
    private Node? _solarSystemRoot;

    public BillboardApp(int width, int height) : base(width, height, new PerspectiveCamera
    {
        Position = new Vector3(0, 80, -220),
        Target = new Vector3(100f, 0f, 0f),
        FarPlane = 2000,
    })
    {
        _billboardCullNode = Engine.GetRenderNode<BillboardCullNode>();
        _billboardCullNode!.MinScreenSize = _minScreenSize;
    }

    protected override void RegisterCustomMaterials()
    {
        _scene = new BillboardShowcaseScene();
        _scene.RegisterMaterials();

        // Register an outlined SDF font material variant
        _outlineMaterialId = SDFFontMaterialConfig.RegisterVariant(
            "SDFFont_Outlined",
            new SDFFontMaterialConfig
            {
                OutlineColor = new Color4(1f, 0f, 0f, 1f),
                OutlineWidth = 0.3f,
            }
        );

        // Register a drop-shadow SDF font material variant
        _shadowMaterialId = SDFFontMaterialConfig.RegisterVariant(
            "SDFFont_Shadow",
            new SDFFontMaterialConfig
            {
                ShadowColor = new Color4(0f, 0f, 0f, 0.6f),
                ShadowOffset = new System.Numerics.Vector2(0.003f, -0.003f),
                ShadowSoftness = 0.05f,
            }
        );

        _logger.LogInformation(
            "Registered SDF material variants: SDFFont_Outlined (ID={OutlineId}), SDFFont_Shadow (ID={ShadowId})",
            _outlineMaterialId.Id,
            _shadowMaterialId.Id
        );
    }

    // ------------------------------------------------------------------
    // Font atlas loading
    // ------------------------------------------------------------------

    private void LoadFontAtlas()
    {
        var fontAtlasRepo = Engine.ResourceManager.FontAtlasRepository;
        _atlas = fontAtlasRepo.GetOrCreateBuiltIn(
            BuildinFontAtlas.GoogleSansRegular,
            Engine.ResourceManager.TextureRepository,
            Engine.ResourceManager.SamplerRepository
        );

        _logger.LogInformation(
            "Loaded SDF font atlas: {W}x{H}, texture={T}, sampler={S}",
            _atlas.TextureWidth,
            _atlas.TextureHeight,
            _atlas.Texture,
            _atlas.Sampler
        );
    }

    protected override void BuildScene()
    {
        // Load the SDF font atlas
        LoadFontAtlas();

        var world = Engine.World;
        var root = Engine.Root;

        // Build the billboard showcase scene (_scene and its materials were already registered
        // before the engine was built in Initialize())
        _solarSystemRoot = ((IScene)_scene!).Build(
            Engine.ResourceManager,
            Engine.WorldDataProvider!
        );
        root.AddChild(_solarSystemRoot);
    }

    public static void Run()
    {
        var demo = new BillboardApp(800, 800);
        demo.Render();
        demo.Engine.Save("output_billboard.bmp");
    }
}

/// <summary>
/// Tracks a single text entry in the billboard demo.
/// Each text entry now uses a single Node with a BillboardGeometry
/// containing all glyphs, instead of one entity per glyph.
/// </summary>
internal sealed class TextEntry
{
    public string Name { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public Vector3 Position { get; set; }
    public Color4 Color { get; set; }
    public float FontSize { get; set; }
    public string MaterialName { get; set; } = "SDFFont";
    public Node? Node { get; set; }
    public bool Editable { get; set; }
    public bool Enabled { get; set; } = true;

    public BuildinFontAtlas FontType { set; get; }
}