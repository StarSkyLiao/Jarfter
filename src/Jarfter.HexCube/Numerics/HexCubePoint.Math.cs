using Jarfter.Core.Numerics;

namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubePoint
{
    /// <summary>
    /// 获取当前坐标到另一个坐标的六边形距离.
    /// </summary>
    /// <param name="other">另一个六边形坐标.</param>
    /// <returns>两个坐标之间的最短步数.</returns>
    public double HexDistanceTo(HexCubePoint other)
    {
        double q = Q - other.Q;
        double r = R - other.R;
        return (q.Abs() + r.Abs() + (q + r).Abs()) / 2;
    }

    /// <summary>
    /// 计算两个 Cube 坐标点之间的直线距离.
    /// </summary>
    public double DistanceTo(HexCubePoint other)
    {
        double dq = other.Q - Q;
        double dr = other.R - R;
        double ds = other.S - S;

        return Math.Sqrt((dq * dq + dr * dr + ds * ds) / 2);
    }

    /// <summary>
    /// 计算两个 Cube 坐标点之间的直线距离.
    /// </summary>
    public double DistanceSquaredTo(HexCubePoint other)
    {
        double dq = other.Q - Q;
        double dr = other.R - R;
        double ds = other.S - S;

        return (dq * dq + dr * dr + ds * ds) / 2;
    }
}
