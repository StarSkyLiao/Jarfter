using System.Diagnostics;

namespace Jarfter.HexCube.Numerics;

/// <summary>
/// 表示六边形网格中的轴向坐标.
/// </summary>
/// <param name="Q">轴向坐标的 q 分量.</param>
/// <param name="R">轴向坐标的 r 分量.</param>
[DebuggerDisplay("Q = {Q}, R = {R}, S = {S}")]
public readonly partial record struct HexCubePoint(double Q, double R)
{
    /// <summary>
    /// 获取隐式立方坐标的 s 分量, 且始终满足 q + r + s = 0.
    /// </summary>
    public double S => -Q - R;

    /// <summary>
    /// 获取原点坐标.
    /// </summary>
    public static HexCubePoint Zero => new HexCubePoint(0, 0);

    /// <summary>
    /// 根据立方坐标创建六边形坐标.
    /// </summary>
    /// <param name="q">立方坐标的 q 分量.</param>
    /// <param name="r">立方坐标的 r 分量.</param>
    /// <param name="s">立方坐标的 s 分量.</param>
    /// <returns>与给定立方坐标等价的轴向坐标.</returns>
    /// <exception cref="ArgumentException">当 q + r + s 不等于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当 q + r + s 的计算结果超出 T 的表示范围时抛出.</exception>
    public static HexCubePoint FromCube(double q, double r, double s)
    {
        if (q + r + s == 0d) return new HexCubePoint(q, r);
        throw new ArgumentException("Cube coordinates must satisfy q + r + s = 0.");
    }

    /// <inheritdoc />
    public override string ToString() => $"(Q = {Q}, R = {R}, S = {S})";
}
