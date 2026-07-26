namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubePoint
{
    /// <summary>
    /// 将当前几何坐标的 q 和 r 分量向下取整为整数网格坐标.
    /// </summary>
    /// <returns>向下取整后的整数网格坐标.</returns>
    /// <exception cref="OverflowException">当取整结果超出 <see cref="int"/> 的表示范围时抛出.</exception>
    public HexCubeGridPoint AsFloor() => new HexCubeGridPoint(checked((int)Math.Floor(Q)), checked((int)Math.Floor(R)));

    /// <summary>
    /// 将当前几何坐标的 q 和 r 分量向上取整为整数网格坐标.
    /// </summary>
    /// <returns>向上取整后的整数网格坐标.</returns>
    /// <exception cref="OverflowException">当取整结果超出 <see cref="int"/> 的表示范围时抛出.</exception>
    public HexCubeGridPoint AsCeil() => new HexCubeGridPoint(checked((int)Math.Ceiling(Q)), checked((int)Math.Ceiling(R)));

    /// <summary>
    /// 将当前几何坐标舍入为最近的整数六边形网格坐标.
    /// </summary>
    /// <returns>与当前几何坐标距离最近的整数网格坐标.</returns>
    /// <exception cref="OverflowException">当舍入结果超出 <see cref="int"/> 的表示范围时抛出.</exception>
    public HexCubeGridPoint AsRound()
    {
        double q = Math.Round(Q);
        double r = Math.Round(R);
        double s = Math.Round(S);
        double qDifference = Math.Abs(q - Q);
        double rDifference = Math.Abs(r - R);
        double sDifference = Math.Abs(s - S);

        // 独立舍入可能破坏 q + r + s = 0, 因此修正误差最大的分量.
        if (qDifference > rDifference && qDifference > sDifference)
        {
            q = -r - s;
        }
        else if (rDifference > sDifference)
        {
            r = -q - s;
        }

        return new HexCubeGridPoint(checked((int)q), checked((int)r));
    }
}
