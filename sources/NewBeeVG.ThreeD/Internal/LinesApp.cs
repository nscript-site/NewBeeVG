using HelixToolkit.Nex.Geometries;
using HelixToolkit.Nex.Maths;
using HelixToolkit.Nex.Scene;
using SkiaSharp;
using System.Numerics;
using static NewBeeVG.Methods3D;

namespace NewBeeVG.ThreeD.Internal;

public class LinesApp : BaseEngineApp
{
    public LinesApp(int width, int height) : base(width, height)
    {
    }

    float thickness = 5;

    Geometry? lineX;
    Geometry? lineY;
    Geometry? lineZ;

    protected override void BuildScene()
    {
        float len = 5;
        lineX = AddLine3D(Vec3(0, 0, 0), Vec3(0, 0, len), SKColors.Red, thickness);
        lineY = AddLine3D(Vec3(0, 0, 0), Vec3(0, len, 0), SKColors.Blue, thickness);
        lineZ = AddLine3D(Vec3(0, 0, 0), Vec3(len, 0, 0), SKColors.Green, thickness);
    }

    Geometry AddLine3D(Vector3 start, Vector3 end, SKColor color, float thickness)
    {
        var geo = BuildGeometry(start, end, color);
        Engine.AddLineSet(geo, color.ToColor4(), thickness);
        return geo;
    }

    Geometry BuildGeometry(Vector3 start, Vector3 end, SKColor color, Geometry? cache = null)
    {
        bool isCache = cache != null;
        var c = color.ToVector4();
        var geo = cache ?? new Geometry(isDynamic: true);
        AddSegment(geo, start, end, c, c);
        if(isCache) 
            geo.MarkDirty(GeometryBufferType.Vertex | GeometryBufferType.VertexColor);
        return geo;
    }

    private void UpdateLength(float len)
    {
        BuildGeometry(Vec3(0, 0, 0), Vec3(0, 0, len), SKColors.Red, lineX);
        BuildGeometry(Vec3(0, 0, 0), Vec3(0, len, 0), SKColors.Blue, lineY);
        BuildGeometry(Vec3(0, 0, 0), Vec3(len, 0, 0), SKColors.Green, lineZ);
    }

    private static void AddSegment(Geometry geo, Vector3 a, Vector3 b, Vector4 ca, Vector4 cb)
    {
        geo.Vertices.Resize(2);
        geo.VertexColors.Resize(2);
        geo.Vertices[0] = a.ToVector4(1);
        geo.Vertices[1] = b.ToVector4(1);
        geo.VertexColors[0] = (ca);
        geo.VertexColors[1] = (cb);
    }

    public static void Run()
    {
        var demo = new LinesApp(800, 800);
        demo.UpdateLength(3);
        demo.Render();
        demo.Engine.Save("output_lines_internal_3.bmp");
        demo.UpdateLength(10);
        demo.Render();
        demo.Engine.Save("output_lines_internal_10.bmp");
    }
}