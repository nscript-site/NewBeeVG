using System.Numerics;

namespace NewBeeVG.ThreeD.Curves;

/// <summary>
/// 圆柱形螺旋线，实现IPolyline3D接口，输出离散采样点构成的3D多段线
/// </summary>
public class CylindricalHelix : IPolyline3D
{
    /// <summary>螺旋半径 r</summary>
    public double Radius { get; }

    /// <summary>每单位i旋转圈数</summary>
    public double TurnRate { get; }

    /// <summary>Z方向缩放系数（局部坐标系）</summary>
    public double ZScale { get; }

    /// <summary>参数i起始整数</summary>
    public int IStart { get; }

    /// <summary>参数i终止整数（包含该端点）</summary>
    public int IEnd { get; }

    /// <summary>螺旋世界坐标原点（螺旋轴线起点）</summary>
    public Vector3 Origin { get; }

    /// <summary>螺旋中心轴线方向向量（会自动归一化）</summary>
    public Vector3 Axis { get; }

    /// <summary>
    /// 构造圆柱形螺旋线
    /// </summary>
    /// <param name="radius">螺旋半径</param>
    /// <param name="turnRate">每单位i旋转圈数，i每增加10旋转完整一圈</param>
    /// <param name="zScale">Z轴缩放系数，控制螺旋在局部轴线方向拉伸程度</param>
    /// <param name="iStart">参数i起始整数</param>
    /// <param name="iEnd">参数i终止整数（包含该点）</param>
    /// <param name="origin">螺旋轴线起点（世界坐标原点）</param>
    /// <param name="axis">螺旋中心轴线方向向量，默认沿Z轴(0,0,1)</param>
    public CylindricalHelix(double radius, double turnRate, double zScale, int iStart, int iEnd, Vector3 origin, Vector3 axis)
    {
        if (iEnd < iStart)
            throw new ArgumentOutOfRangeException(nameof(iEnd), "iEnd不能小于iStart");
        if (radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "半径必须大于0");

        Radius = radius;
        TurnRate = turnRate;
        ZScale = zScale;
        IStart = iStart;
        IEnd = iEnd;
        Origin = origin;

        // 归一化轴线
        float len = axis.Length();
        if (len < 1e-6f)
            throw new ArgumentException("轴线向量不能是零向量", nameof(axis));
        Axis = axis / len;
    }

    /// <summary>
    /// 获取螺旋线离散采样点集合，可直接作为3D多段线Polyline顶点
    /// </summary>
    /// <returns>3D顶点列表（世界坐标）</returns>
    public IList<Vector3> GetPoints()
    {
        int pointCount = IEnd - IStart + 1;
        var points = new List<Vector3>(pointCount);

        // 构建局部坐标系到世界坐标系的旋转矩阵
        Matrix4x4 rot = BuildRotationToAxis(Axis);

        for (int i = IStart; i <= IEnd; i++)
        {
            double theta = 2 * Math.PI * TurnRate * i;
            double xLocal = Radius * Math.Cos(theta);
            double yLocal = Radius * Math.Sin(theta);
            double zLocal = ZScale * i;

            // 局部点
            Vector3 localPt = new Vector3((float)xLocal, (float)yLocal, (float)zLocal);
            // 旋转到目标轴线，再平移到原点
            Vector3 worldPt = Vector3.Transform(localPt, rot) + Origin;
            points.Add(worldPt);
        }
        return points;
    }

    /// <summary>
    /// 构造旋转矩阵：把局部Z轴(0,0,1)旋转到目标axis方向
    /// </summary>
    private static Matrix4x4 BuildRotationToAxis(Vector3 targetAxis)
    {
        Vector3 src = new Vector3(0, 0, 1);
        const float eps = 1e-6f;

        // 近似相等判断，替代Vector3.NearlyEqual
        if (Vector3.Dot(src, targetAxis) > 1 - eps)
        {
            return Matrix4x4.Identity;
        }
        // 判断反向平行
        if (Vector3.Dot(src, targetAxis) < -(1 - eps))
        {
            return Matrix4x4.CreateRotationX((float)Math.PI);
        }

        Vector3 cross = Vector3.Cross(src, targetAxis);
        float dot = Vector3.Dot(src, targetAxis);
        float angle = (float)Math.Acos(dot);
        return Matrix4x4.CreateFromAxisAngle(cross, angle);
    }
}