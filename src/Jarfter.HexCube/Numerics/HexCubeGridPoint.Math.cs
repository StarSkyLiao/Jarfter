using Jarfter.Core.Numerics;

namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubeGridPoint
{
    /// <summary>
    /// 获取当前坐标到另一个整数网格坐标的六边形距离.
    /// </summary>
    /// <param name="other">另一个整数网格坐标.</param>
    /// <returns>两个坐标之间的最短步数.</returns>
    public int HexDistanceTo(HexCubeGridPoint other)
    {
        int q = Q - other.Q;
        int r = R - other.R;
        return (q.Abs() + r.Abs() + (q + r).Abs()) / 2;
    }
}
