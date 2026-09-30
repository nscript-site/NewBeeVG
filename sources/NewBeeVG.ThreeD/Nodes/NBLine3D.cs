using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Scene;
using SkiaSharp;
using System.Numerics;

namespace NewBeeVG.ThreeD;

public class NBLine3D : NBNode3D
{
    public SKColor Color { get; set; }
    public float Thickness { get; set; } = 1;
    public Vector3 Start { get; set; }
    public Vector3 End { get; set; }
    
    private LineNode? Node { get; set; }

    private Geometry? Geo { get; set; }

    public Action<NBTypedNode3DUpdateEvent<NBLine3D>>? OnFrameUpdated_T { get; set; }

    public override void Build(NBEngine engine)
    {
        Geo = BuildGeometry(Geo);
        Node = engine.AddLineSet(Geo, Color.ToColor4(), Thickness);
    }

    protected override void Update()
    {
        if (Node == null) return;
        BuildGeometry(Geo);
    }

    private Geometry BuildGeometry(Geometry? cache = null)
    {
        bool isCache = cache != null;
        var c = Color.ToVector4();
        var geo = cache ?? new Geometry(isDynamic: true);
        SetLineSegment(geo, Start, End, c, c);
        if (isCache)
            geo.MarkDirty(GeometryBufferType.Vertex | GeometryBufferType.VertexColor);
        return geo;
    }

    public override void FireOnFrameUpdated(NBFrameUpdateEvent e)
    {
        base.FireOnFrameUpdated(e);
        if(OnFrameUpdated_T != null)
        {
            var ne = new NBTypedNode3DUpdateEvent<NBLine3D>(e.Ctx);
            ne.Sender = this;
            OnFrameUpdated_T.Invoke(ne);
            Update();
        }
    }
}

public static partial class NBExtentions_Three3D
{
    public static T OnFrameT<T>(this T self, Action<NBTypedNode3DUpdateEvent<NBLine3D>> onFrameUpdate) where T : NBLine3D
    {
        self.OnFrameUpdated_T += onFrameUpdate;
        return self;
    }
}