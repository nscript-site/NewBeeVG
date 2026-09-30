using System.Numerics;

namespace NewBeeVG.ThreeD.Curves;

public interface IPolyline3D
{
    public IList<Vector3> GetPoints();
}
