using NewBeeVG.ThreeD.Curves;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace NewBeeVG.ThreeD;

public static class Curves3D
{
    /// <summary>
    /// 创建圆柱形螺旋线实例（静态工厂方法，带默认参数）
    /// 默认原点(0,0,0)，朝向Z轴正方向
    /// </summary>
    /// <param name="radius">螺旋半径</param>
    /// <param name="turnRate">每单位i旋转圈数，默认0.1；i每增加10旋转完整一圈</param>
    /// <param name="zScale">Z轴缩放系数，控制螺旋在Z方向的拉伸程度，默认0.05</param>
    /// <param name="iStart">参数i起始整数，默认0</param>
    /// <param name="iEnd">参数i终止整数（包含该点），默认100</param>
    /// <param name="origin">螺旋轴线起点，默认(0,0,0)</param>
    /// <param name="axis">螺旋中心轴线方向，默认(0,0,1)沿Z向上</param>
    /// <returns>圆柱形螺旋线对象</returns>
    public static CylindricalHelix CylindricalHelix(
        double radius,
        double turnRate = 0.01,
        double zScale = 0.05,
        int iStart = 0,
        int iEnd = 100,
        Vector3? origin = null,
        Vector3? axis = null)
    {
        return new CylindricalHelix(
            radius,
            turnRate,
            zScale,
            iStart,
            iEnd,
            origin ?? Vector3.Zero,
            axis ?? new Vector3(0, 0, 1));
    }
}
