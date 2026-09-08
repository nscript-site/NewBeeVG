using HelixToolkit.Nex.Engine.Cameras;
using NewBeeVG.ThreeD;
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