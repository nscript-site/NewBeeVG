using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Scene;
using SkiaSharp;
using System.Numerics;

namespace NewBeeVG.ThreeD;

public class NBGroundGrid : NBNode3D
{
    public SKColor Color { get; set; }
    public float Thickness { get; set; } = 1;
    public int HalfLines { get; set { field = Math.Max(0, value); } }
    public float Spacing { get; set { field = Math.Max(0.000001f, value); } }

    private LineNode? Node { get; set; }

    private Geometry? Geo { get; set; }

    public Action<NBTypedNode3DUpdateEvent<NBGroundGrid>>? OnFrameUpdated_T { get; set; }

    public override void Build(NBEngine engine)
    {
        Geo = GenerateGrid(HalfLines, Spacing, Color.ToColor4(), Geo);
        Node = engine.AddLineSet(Geo, Color.ToColor4(), Thickness);
    }

    protected override void Update()
    {
        if (Node == null) return;
        GenerateGrid(HalfLines, Spacing, Color.ToColor4(), Geo);
    }

    private Geometry GenerateGrid(int halfLines, float spacing, Color4 color, Geometry? cache = null)
    {
        bool isCache = cache != null;
        var geo = cache ?? new Geometry(isDynamic: true);
        var c = new Vector4(color.Red, color.Green, color.Blue, color.Alpha);
        float extent = halfLines * spacing;
        int count = halfLines * 2 + 1;
        if(count > 1)
        {
            geo.Vertices.Resize(count * 4);
            geo.VertexColors.Resize(count * 4);
            int idx = 0;
            for (int i = -halfLines; i <= halfLines; i++)
            {
                float p = i * spacing;
                AddLineSegment(geo, idx * 4, new Vector3(-extent, 0, p), new Vector3(extent, 0, p), c, c);
                AddLineSegment(geo, idx * 4 + 2, new Vector3(p, 0, -extent), new Vector3(p, 0, extent), c, c);
                idx++;
            }
        }
        if (isCache)
            geo.MarkDirty(GeometryBufferType.Vertex | GeometryBufferType.VertexColor);
        return geo;
    }

    public override void FireOnFrameUpdated(NBFrameUpdateEvent e)
    {
        base.FireOnFrameUpdated(e);
        if (OnFrameUpdated_T != null)
        {
            var ne = new NBTypedNode3DUpdateEvent<NBGroundGrid>(e.Ctx);
            ne.Sender = this;
            OnFrameUpdated_T.Invoke(ne);
            Update();
        }
    }
}
