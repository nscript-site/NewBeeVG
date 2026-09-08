using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Maths;
using System.Numerics;

namespace NewBeeVG.ThreeD;

public class NBNode3D
{
    public virtual void AddMaterials()
    {
    }

    public virtual void Build(NBEngine engine)
    {
    }

    protected static void SetLineSegment(Geometry geo, Vector3 a, Vector3 b, Vector4 ca, Vector4 cb)
    {
        geo.Vertices.Resize(2);
        geo.VertexColors.Resize(2);
        geo.Vertices[0] = a.ToVector4(1);
        geo.Vertices[1] = b.ToVector4(1);
        geo.VertexColors[0] = (ca);
        geo.VertexColors[1] = (cb);
    }

    protected static void AddLineSegment(Geometry geo, int idx, Vector3 a, Vector3 b, Vector4 ca, Vector4 cb)
    {
        geo.Vertices[idx + 0] = a.ToVector4(1);
        geo.Vertices[idx + 1] = b.ToVector4(1);
        geo.VertexColors[idx + 0] = (ca);
        geo.VertexColors[idx + 1] = (cb);
    }

    public Action<NBNode3DUpdateEvent>? OnFrameUpdated { get; set; }

    public virtual void FireOnFrameUpdated(NBFrameUpdateEvent e)
    {
        if(OnFrameUpdated != null)
        {
            var ne = new NBNode3DUpdateEvent(e.Ctx);
            ne.Sender = this;
            OnFrameUpdated.Invoke(ne);
            Update();
        }
    }

    protected virtual void Update()
    {
    }
}

public static partial class NBExtentions_Three3D
{
    public static T OnFrame<T>(this T self, Action<NBNode3DUpdateEvent> onFrameUpdate) where T : NBNode3D
    {
        self.OnFrameUpdated += onFrameUpdate;
        return self;
    }
}