using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Scene;
using System.Numerics;

namespace NewBeeVG.ThreeD.Nodes;

public class NBPolyline3D : NBNode3D
{
    public IList<Vector3> Points { get; init; }
    public IList<Vector4> Colors { get; init; }
    public float Thickness { get; set; } = 1;
    private LineNode? Node { get; set; }
    private Geometry? Geo { get; set; }

    public Action<NBTypedNode3DUpdateEvent<NBPolyline3D>>? OnFrameUpdated_T { get; set; }

    public override void Build(NBEngine engine)
    {
        Geo = BuildGeometry();
        var color = Colors.Count > 0 ? Colors[0]: new Vector4();
        Node = engine.AddLineSet(Geo, color.ToColor4(), Thickness);
    }

    private Geometry BuildGeometry()
    {
        var geo = new Geometry(isDynamic: true);
        if(Colors.Count < Points.Count)
        {
            var c = Colors.Count > 0 ? Colors[Colors.Count - 1] : new Vector4();
            for(int i = Colors.Count; i < Points.Count; i++)
            {
                Colors.Add(c);
            }
        }

        for (int i = 0; i < Points.Count - 1; i++)
        {
            var v0 = Points[i];
            var v1 = Points[i + 1];
            var c1 = Colors[i];
            var c2 = Colors[i + 1];
            AddLineSegment(geo, v0, v1, c1, c2);
        }

        return geo;
    }

    public override void FireOnFrameUpdated(NBFrameUpdateEvent e)
    {
        base.FireOnFrameUpdated(e);
        if (OnFrameUpdated_T != null)
        {
            var ne = new NBTypedNode3DUpdateEvent<NBPolyline3D>(e.Ctx);
            ne.Sender = this;
            OnFrameUpdated_T.Invoke(ne);
            Update();
        }
    }
}

public static partial class NBPolyline3D_Three3D
{
    public static T OnFrame<T>(this T self, Action<NBNode3DUpdateEvent> onFrameUpdate) where T : NBPolyline3D
    {
        self.OnFrameUpdated += onFrameUpdate;
        return self;
    }
}