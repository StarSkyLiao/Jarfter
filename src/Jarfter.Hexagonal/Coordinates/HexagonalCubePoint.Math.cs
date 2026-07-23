using Jarfter.Core.Numerics;

namespace Jarfter.Hexagonal.Coordinates;

public readonly partial record struct HexagonalCubePoint
{
    /// <summary>
    /// 获取当前坐标到另一个坐标的六边形距离.
    /// </summary>
    /// <param name="other">另一个六边形坐标.</param>
    /// <returns>两个坐标之间的最短步数.</returns>
    public int DistanceTo(HexagonalCubePoint other)
    {
        int q = Q - other.Q;
        int r = R - other.R;
        return (q.Abs() + r.Abs() + (q + r).Abs()) / 2;
    }
}
