using System.Diagnostics;

namespace Jarfter.HexCube.Numerics;

/// <summary>
/// 表示六边形网格中的整数轴向坐标.
/// 该类型只用于网格单元和拓扑运算, 不应承载线段相交等几何计算产生的分数坐标.
/// </summary>
/// <param name="Q">轴向坐标的 q 分量.</param>
/// <param name="R">轴向坐标的 r 分量.</param>
[DebuggerDisplay("Q = {Q}, R = {R}, S = {S}")]
public readonly partial record struct HexCubeGridPoint(int Q, int R)
{
    /// <summary>
    /// 获取隐式立方坐标的 s 分量, 且始终满足 q + r + s = 0.
    /// </summary>
    public int S => -Q - R;

    /// <summary>
    /// 获取原点坐标.
    /// </summary>
    public static HexCubeGridPoint Zero => new HexCubeGridPoint(0, 0);

    /// <summary>
    /// 根据立方坐标创建整数网格坐标.
    /// </summary>
    /// <param name="q">立方坐标的 q 分量.</param>
    /// <param name="r">立方坐标的 r 分量.</param>
    /// <param name="s">立方坐标的 s 分量.</param>
    /// <returns>与给定立方坐标等价的整数网格坐标.</returns>
    /// <exception cref="ArgumentException">当 q + r + s 不等于 0 时抛出.</exception>
    public static HexCubeGridPoint FromCube(int q, int r, int s)
    {
        if ((long)q + r + s == 0) return new HexCubeGridPoint(q, r);
        throw new ArgumentException("Cube coordinates must satisfy q + r + s = 0.");
    }

    /// <summary>
    /// 将整数网格坐标转换为几何坐标.
    /// </summary>
    /// <param name="point">要转换的整数网格坐标.</param>
    /// <returns>与整数网格坐标位置相同的几何坐标.</returns>
    public static implicit operator HexCubePoint(HexCubeGridPoint point) => new HexCubePoint(point.Q, point.R);

    /// <summary>
    /// 将几何坐标转换为整数网格坐标.
    /// 转换只接受 q 和 r 分量均为整数的网格中心坐标, 不会进行舍入或截断.
    /// </summary>
    /// <param name="point">要转换的几何坐标.</param>
    /// <returns>与几何坐标位置相同的整数网格坐标.</returns>
    /// <exception cref="ArgumentException">当几何坐标不是网格中心坐标时抛出.</exception>
    /// <exception cref="OverflowException">当几何坐标超出 <see cref="int"/> 的表示范围时抛出.</exception>
    public static explicit operator HexCubeGridPoint(HexCubePoint point)
    {
        int q = checked((int)point.Q);
        int r = checked((int)point.R);

        if (point.Q != q || point.R != r)
        {
            throw new ArgumentException("HexCubePoint must be positioned at a grid center.", nameof(point));
        }

        return new HexCubeGridPoint(q, r);
    }

    /// <inheritdoc />
    public override string ToString() => $"(Q = {Q}, R = {R}, S = {S})";
}
