using HelixToolkit.Nex.Engine.Cameras;
using NewBeeVG.ThreeD;
using NewBeeVG.ThreeD.Curves;
using NewBeeVG.ThreeD.Nodes;
using SkiaSharp;
using System.Numerics;

namespace NewBeeVG;

public static class Methods3D
{
    public static NBCanvas3D Canvas3D(int width = 100, int height = 100, SKColor? bg = null)
    {
        return new NBCanvas3D() { Width = width, Height = height, Background = bg };
    }

    public static NBLine3D Line3D(Vector3 start, Vector3 end, SKColor color, float thickness = 1)
    {
        return new NBLine3D() { Start = start, End = end, Color = color, Thickness = thickness };
    }

    public static NBPolyline3D Polyline3D(IList<Vector3> points, IList<Vector4> colors, float thickness = 1)
    {
        return new NBPolyline3D() { Points = points, Colors = colors, Thickness = thickness };
    }

    public static NBPolyline3D Polyline3D(IList<Vector3> points, Vector4 color, float thickness = 1)
    {
        var colors = new List<Vector4>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            colors.Add(color);
        }
        return new NBPolyline3D() { Points = points, Colors = colors, Thickness = thickness };
    }

    public static NBPolyline3D Polyline3D(IList<Vector3> points, IList<SKColor> colors, float thickness = 1)
    {
        var v4s = new List<Vector4>(points.Count);
        foreach (var c in colors)
        {
            v4s.Add(c.ToVector4());
        }
        return new NBPolyline3D() { Points = points, Colors = v4s, Thickness = thickness };
    }

    public static NBPolyline3D Polyline3D(IList<Vector3> points, SKColor color, float thickness = 1)
    {
        var v = color.ToVector4();
        var v4s = new List<Vector4>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            v4s.Add(v);
        }
        return new NBPolyline3D() { Points = points, Colors = v4s, Thickness = thickness };
    }

    public static NBPolyline3D Polyline3D(Func<IList<Vector3>> pointsFunc, SKColor color, float thickness = 1)
    {
        var points = pointsFunc();
        return Polyline3D(points, color, thickness);
    }

    public static NBPolyline3D Polyline3D(Action<List<Vector3>> onList, SKColor color, float thickness = 1)
    {
        var points = new List<Vector3>();
        onList(points);
        return Polyline3D(points, color, thickness);
    }

    public static NBPolyline3D Polyline3D(int count, SKColor color, Func<int, Vector3> funcPoint, float thickness = 1)
    {
        var points = new List<Vector3>();
        for(int i = 0; i < count; i++)
        {
            points.Add(funcPoint(i));
        }
        return Polyline3D(points, color, thickness);
    }

    public static NBPolyline3D Polyline3D(Func<(IList<Vector3>, IList<SKColor>)> func, float thickness = 1)
    {
        var p = func();
        return Polyline3D(p.Item1, p.Item2, thickness);
    }

    public static NBPolyline3D Polyline3D(IPolyline3D model, SKColor color, float thickness = 1)
    {
        return Polyline3D(model.GetPoints(), color, thickness);
    }

    public static NBGroundGrid GroundGrid(int halfLines, float spacing, SKColor color, float thickness = 1)
    {
        return new NBGroundGrid() { HalfLines = halfLines, Spacing = spacing, Color = color, Thickness = thickness };
    }

    public static Vector3 Vec3(float x1, float x2, float x3)
    {
        return new Vector3(x1, x2, x3);
    }

    public static Vector3 Vec3()
    {
        return new Vector3();
    }

    public static Vector3 Vec3(float val)
    {
        return new Vector3(val, val, val);
    }

    public static PerspectiveCamera PerspectiveCamera(Vector3 position, Vector3? target = null, float farPlane = 500)
    {
        return new PerspectiveCamera() { Position = position, Target = target ?? Vector3.Zero, FarPlane = farPlane };
    }
}    