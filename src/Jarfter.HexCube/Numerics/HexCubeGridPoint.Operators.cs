using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubeGridPoint :
    IUnaryPlusOperators<HexCubeGridPoint, HexCubeGridPoint>,
    IUnaryNegationOperators<HexCubeGridPoint, HexCubeGridPoint>,
    IAdditionOperators<HexCubeGridPoint, HexCubeGridPoint, HexCubeGridPoint>,
    ISubtractionOperators<HexCubeGridPoint, HexCubeGridPoint, HexCubeGridPoint>,
    IMultiplyOperators<HexCubeGridPoint, int, HexCubeGridPoint>
{
    /// <summary>
    /// 返回坐标自身.
    /// </summary>
    /// <param name="value">坐标.</param>
    /// <returns>未改变的坐标.</returns>
    public static HexCubeGridPoint operator +(HexCubeGridPoint value) => value;

    /// <summary>
    /// 返回相对于原点的反向坐标.
    /// </summary>
    /// <param name="point">坐标.</param>
    /// <returns>反向坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubeGridPoint operator -(HexCubeGridPoint point) => new HexCubeGridPoint(-point.Q, -point.R);

    /// <summary>
    /// 对两个坐标的分量执行加法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相加后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubeGridPoint operator +(HexCubeGridPoint left, HexCubeGridPoint right) => new HexCubeGridPoint(left.Q + right.Q, left.R + right.R);

    /// <summary>
    /// 对两个坐标的分量执行减法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相减后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubeGridPoint operator -(HexCubeGridPoint left, HexCubeGridPoint right) => new HexCubeGridPoint(left.Q - right.Q, left.R - right.R);

    /// <summary>
    /// 使用整数标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="point">坐标.</param>
    /// <param name="factor">缩放因子.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubeGridPoint operator *(HexCubeGridPoint point, int factor) => new HexCubeGridPoint(point.Q * factor, point.R * factor);

    /// <summary>
    /// 使用整数标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="factor">缩放因子.</param>
    /// <param name="point">坐标.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubeGridPoint operator *(int factor, HexCubeGridPoint point) => point * factor;
}
