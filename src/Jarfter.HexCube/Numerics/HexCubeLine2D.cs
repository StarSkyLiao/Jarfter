using System.Diagnostics;

namespace Jarfter.HexCube.Numerics;

/// <summary>
/// 表示六边形网格中的一条线段.
/// </summary>
/// <param name="Start">线段的起点.</param>
/// <param name="End">线段的终点.</param>
[DebuggerDisplay("Start = {Start}, End = {End}")]
public readonly record struct HexCubeLine2D(HexCubePoint Start, HexCubePoint End)
{
    /// <summary>
    /// 获取该线段的长度.
    /// </summary>
    public double Length => Start.DistanceTo(End);

    /// <summary>
    /// 获取该线段的平方长度.
    /// </summary>
    public double LengthSquared => Start.DistanceSquaredTo(End);

    /// <summary>
    /// 获取该线段的向量表示.
    /// </summary>
    public HexCubePoint AsVector => End - Start;

}
