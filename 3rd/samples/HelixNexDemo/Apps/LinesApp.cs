using HelixToolkit.Nex;
using HelixToolkit.Nex.Engine.Components;
using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Material;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Scene;
using HelixToolkit.Nex.Shaders;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace HelixNexDemo;

public class LinesApp : BaseEngineApp
{
    private static readonly ILogger _logger = LogManager.Create<PointsApp>();

    private float _globalLineWidth = 2.0f;
    private float _animTime;

    public LinesApp(int width, int height) : base(width,height)
    {
    }


    /// <summary>
    /// Registers several custom line material shaders to demonstrate the
    /// <see cref="LineMaterialRegistry"/> extensibility.
    /// <para>
    /// Each body REPLACES the template's outputColor(); the built-in "Default" does NOT
    /// feather or premultiply, so every custom body does its own edge feathering across the
    /// line width (v_uv.y in [-1,1]) and premultiplied-alpha output.
    /// </para>
    /// </summary>
    protected override void RegisterCustomMaterials()
    {
        // 1. Gradient — colorize along the segment using v_uv.x (in [-1,1] -> [0,1]).
        LineMaterialRegistry.Register(
            name: "Gradient",
            getLineColorImpl: """
                float t = clamp(getUV().x * 0.5 + 0.5, 0.0, 1.0);
                vec4 c = getColor();
                // Blend from the base color toward a complementary hue along the segment.
                vec3 grad = mix(c.rgb, c.gbr, t);
                // Feather across the line width and premultiply.
                float edge = clamp(1.0 - abs(getUV().y), 0.0, 1.0);
                float feather = (getLineWidth() > 0.0)
                    ? clamp(1.0 / max(getLineWidth() * 0.5, 1e-6), 0.0, 1.0) : 0.0;
                float a = (feather <= 0.0) ? step(0.0, edge) : smoothstep(0.0, feather, edge);
                a *= c.a;
                return vec4(grad * a, a);
            """
        );

        // 2. Dashed — discard fragments based on an animated dash pattern along v_uv.x.
        LineMaterialRegistry.Register(
            name: "Dashed",
            getLineColorImpl: """
                float t = getUV().x * 0.5 + 0.5;                 // [0,1] along the segment
                float timeMs = float(getTimeMs() % 100000) / 1000.0;
                float pattern = fract(t * 16.0 - timeMs * 2.0);  // scrolling dashes
                if (pattern > 0.5) discard;                       // 50% duty cycle gaps
                vec4 c = getColor();
                // Feather across the line width and premultiply.
                float edge = clamp(1.0 - abs(getUV().y), 0.0, 1.0);
                float feather = (getLineWidth() > 0.0)
                    ? clamp(1.0 / max(getLineWidth() * 0.5, 1e-6), 0.0, 1.0) : 0.0;
                float a = (feather <= 0.0) ? step(0.0, edge) : smoothstep(0.0, feather, edge);
                a *= c.a;
                return vec4(c.rgb * a, a);
            """
        );

        // 3. Glow — boost brightness toward the segment center (small |v_uv.y|).
        LineMaterialRegistry.Register(
            name: "Glow",
            getLineColorImpl: """
                vec4 c = getColor();
                float center = 1.0 - clamp(abs(getUV().y), 0.0, 1.0);
                float glow = pow(center, 2.0);
                vec3 rgb = c.rgb * (1.0 + 1.5 * glow);
                // Feather across the line width and premultiply.
                float edge = clamp(1.0 - abs(getUV().y), 0.0, 1.0);
                float feather = (getLineWidth() > 0.0)
                    ? clamp(1.0 / max(getLineWidth() * 0.5, 1e-6), 0.0, 1.0) : 0.0;
                float a = (feather <= 0.0) ? step(0.0, edge) : smoothstep(0.0, feather, edge);
                a *= c.a;
                return vec4(rgb * a, a);
            """
        );

        _logger.LogInformation(
            "Registered {Count} custom line material types.",
            _materialTypes.Length - 1
        );
    }

    // ------------------------------------------------------------------
    // Scene building
    // ------------------------------------------------------------------
    protected override void BuildScene()
    {
        var world = Engine.World;
        var root = Engine.Root;

        // Directional light so any future meshes are lit.
        var lightNode = new Node(world) { Name = "DirectionalLight" };
        lightNode.Entity.Set(
            new DirectionalLightInfo
            {
                Light = new DirectionalLight
                {
                    Direction = Vector3.Normalize(new Vector3(0.5f, -1f, 0.5f)),
                    Color = new Vector3(1f, 0.98f, 0.95f),
                    Intensity = 0.8f,
                },
            }
        );
        root.AddChild(lightNode);

        // 1. Axes — 3 colored segments from origin.
        AddLineSet("Axes", GenerateAxes(10f), new Color4(1f, 1f, 1f, 1f), "Default", thickness: 3f);

        // 2. Grid — ground grid on the XZ plane.
        AddLineSet(
            "Grid",
            GenerateGrid(20, 1f, new Color4(0.4f, 0.4f, 0.45f, 1f)),
            new Color4(0.4f, 0.4f, 0.45f, 1f),
            "Default",
            thickness: 1f
        );

        // 3. Helix — connected disjoint segments with per-vertex gradient colors.
        AddLineSet(
            "Helix",
            GenerateHelix(256, 4f, 12f, 4, new Vector3(15, 0, 0)),
            new Color4(1f, 0.5f, 0.2f, 1f),
            "Gradient",
            thickness: 3f
        );

        // 4. Wireframe Box — 12 edges of a cube as 12 segments.
        AddLineSet(
            "Wireframe Box",
            GenerateWireframeBox(new Vector3(-15, 3, 0), 6f, new Color4(0.3f, 1f, 0.5f, 1f)),
            new Color4(0.3f, 1f, 0.5f, 1f),
            "Glow",
            thickness: 4f
        );

        // 5. Animated Wave — dynamic segments whose endpoints animate over time.
        AddLineSet(
            "Animated Wave",
            GenerateWave(48, 16f, 0f, new Vector3(0, -5, 15), null),
            new Color4(0.9f, 0.85f, 0.2f, 1f),
            "Dashed",
            thickness: 2f
        );
    }

    private void AddLineSet(
        string name,
        Geometry geo,
        Color4 color,
        string materialName,
        float thickness
    )
    {
        var world = Engine.World;
        var root = Engine.Root;
        var node = world.CreateLineNode(name);
        root!.AddChild(node);

        node.Geometry = geo;
        node.LineColor = color;
        node.LineThickness = thickness;
        node.LineMaterialName = materialName;
        node.Hitable = true;

        Engine.Add(geo);
    }

    // ------------------------------------------------------------------
    // Line geometry generators (all as disjoint 2-vertex segments)
    // ------------------------------------------------------------------

    private static void AddSegment(Geometry geo, Vector3 a, Vector3 b, Vector4 ca, Vector4 cb)
    {
        geo.Vertices.Add(a.ToVector4(1));
        geo.Vertices.Add(b.ToVector4(1));
        geo.VertexColors.Add(ca);
        geo.VertexColors.Add(cb);
    }

    private Geometry GenerateAxes(float length)
    {
        var geo = new Geometry();
        var red = new Vector4(1f, 0.1f, 0.1f, 1f);
        var green = new Vector4(0.1f, 1f, 0.1f, 1f);
        var blue = new Vector4(0.2f, 0.4f, 1f, 1f);
        AddSegment(geo, Vector3.Zero, new Vector3(length, 0, 0), red, red);
        AddSegment(geo, Vector3.Zero, new Vector3(0, length, 0), green, green);
        AddSegment(geo, Vector3.Zero, new Vector3(0, 0, length), blue, blue);
        return geo;
    }

    private Geometry GenerateGrid(int halfLines, float spacing, Color4 color)
    {
        var geo = new Geometry();
        var c = new Vector4(color.Red, color.Green, color.Blue, color.Alpha);
        float extent = halfLines * spacing;
        for (int i = -halfLines; i <= halfLines; i++)
        {
            float p = i * spacing;
            // Lines parallel to X axis (vary Z)
            AddSegment(geo, new Vector3(-extent, 0, p), new Vector3(extent, 0, p), c, c);
            // Lines parallel to Z axis (vary X)
            AddSegment(geo, new Vector3(p, 0, -extent), new Vector3(p, 0, extent), c, c);
        }
        return geo;
    }

    private Geometry GenerateHelix(
        int samples,
        float radius,
        float height,
        int turns,
        Vector3 center
    )
    {
        var geo = new Geometry();
        // Sample the helix into points, then emit connected disjoint segments by
        // DUPLICATING shared endpoints so each segment is its own 2-vertex pair.
        var pts = new Vector3[samples];
        var cols = new Vector4[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / (samples - 1);
            float angle = t * turns * MathF.PI * 2f;
            float y = t * height - height * 0.5f;
            float r = radius * (0.5f + 0.5f * MathF.Sin(t * MathF.PI));
            pts[i] = center + new Vector3(MathF.Cos(angle) * r, y, MathF.Sin(angle) * r);
            cols[i] = new Vector4(t, 0.5f, 1f - t, 1f);
        }
        for (int i = 0; i < samples - 1; i++)
            AddSegment(geo, pts[i], pts[i + 1], cols[i], cols[i + 1]);
        return geo;
    }

    private Geometry GenerateWireframeBox(Vector3 center, float size, Color4 color)
    {
        var geo = new Geometry();
        var c = new Vector4(color.Red, color.Green, color.Blue, color.Alpha);
        float h = size * 0.5f;
        // 8 cube corners
        var v = new Vector3[8];
        v[0] = center + new Vector3(-h, -h, -h);
        v[1] = center + new Vector3(h, -h, -h);
        v[2] = center + new Vector3(h, h, -h);
        v[3] = center + new Vector3(-h, h, -h);
        v[4] = center + new Vector3(-h, -h, h);
        v[5] = center + new Vector3(h, -h, h);
        v[6] = center + new Vector3(h, h, h);
        v[7] = center + new Vector3(-h, h, h);

        // 12 edges as 12 disjoint segments
        int[,] edges =
        {
            { 0, 1 },
            { 1, 2 },
            { 2, 3 },
            { 3, 0 }, // back face
            { 4, 5 },
            { 5, 6 },
            { 6, 7 },
            { 7, 4 }, // front face
            { 0, 4 },
            { 1, 5 },
            { 2, 6 },
            { 3, 7 }, // connecting edges
        };
        for (int e = 0; e < 12; e++)
            AddSegment(geo, v[edges[e, 0]], v[edges[e, 1]], c, c);
        return geo;
    }

    private Geometry GenerateWave(
        int side,
        float width,
        float time,
        Vector3 center,
        Geometry? cache
    )
    {
        // A row of animated segments: for each column, a vertical-ish segment whose
        // endpoints rise and fall over time. Rebuilt each frame (dynamic geometry).
        var geo = cache ?? new Geometry(isDynamic: true);
        int segCount = side;
        geo.Vertices.Resize(segCount * 2);
        geo.VertexColors.Resize(segCount * 2);
        for (int i = 0; i < segCount; i++)
        {
            float u = (float)i / (segCount - 1) - 0.5f;
            float x = u * width;
            float phase = u * 12f - time * 3f;
            float y0 = MathF.Sin(phase) * 2.0f;
            float y1 = y0 + 1.5f + 0.5f * MathF.Cos(phase * 1.3f);
            float hue = (MathF.Sin(phase) + 1f) * 0.5f;
            var a = center + new Vector3(x, y0, 0);
            var b = center + new Vector3(x, y1, 0);
            var ca = new Vector4(hue, 0.6f + 0.4f * (1f - hue), 1f - hue * 0.5f, 1f);
            var cb = new Vector4(1f - hue, 0.5f, hue, 1f);
            geo.Vertices[i * 2 + 0] = a.ToVector4(1);
            geo.Vertices[i * 2 + 1] = b.ToVector4(1);
            geo.VertexColors[i * 2 + 0] = ca;
            geo.VertexColors[i * 2 + 1] = cb;
        }
        geo.MarkDirty(GeometryBufferType.Vertex | GeometryBufferType.VertexColor);
        return geo;
    }

    public static void Run()
    {
        var demo = new LinesApp(800, 800);
        demo.Render();
        demo.Engine.Save("output_lines.bmp");
    }
}