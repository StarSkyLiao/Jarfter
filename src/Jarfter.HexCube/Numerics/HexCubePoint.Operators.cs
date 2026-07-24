using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubePoint :
    IUnaryPlusOperators<HexCubePoint, HexCubePoint>,
    IUnaryNegationOperators<HexCubePoint, HexCubePoint>,
    IAdditionOperators<HexCubePoint, HexCubePoint, HexCubePoint>,
    ISubtractionOperators<HexCubePoint, HexCubePoint, HexCubePoint>,
    IMultiplyOperators<HexCubePoint, double, HexCubePoint>,
    IDivisionOperators<HexCubePoint, double, HexCubePoint>
{
    /// <summary>
    /// 返回坐标自身.
    /// </summary>
    /// <param name="value">坐标.</param>
    /// <returns>未改变的坐标.</returns>
    public static HexCubePoint operator +(HexCubePoint value) => value;

    /// <summary>
    /// 返回相对于原点的反向坐标.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <returns>反向坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubePoint operator -(HexCubePoint cell)
    {
        return new HexCubePoint(-cell.Q, -cell.R);
    }

    /// <summary>
    /// 对两个坐标的分量执行加法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相加后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubePoint operator +(HexCubePoint left, HexCubePoint right)
    {
        return new HexCubePoint(left.Q + right.Q, left.R + right.R);
    }

    /// <summary>
    /// 对两个坐标的分量执行减法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相减后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubePoint operator -(HexCubePoint left, HexCubePoint right)
    {
        return new HexCubePoint(left.Q - right.Q, left.R - right.R);
    }

    /// <summary>
    /// 使用标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <param name="factor">缩放因子.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubePoint operator *(HexCubePoint cell, double factor)
    {
        return new HexCubePoint(cell.Q * factor, cell.R * factor);
    }

    /// <summary>
    /// 使用标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="factor">缩放因子.</param>
    /// <param name="cell">坐标.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubePoint operator *(double factor, HexCubePoint cell)
    {
        return new HexCubePoint(cell.Q * factor, cell.R * factor);
    }

    /// <summary>
    /// 使用标量除以坐标的各个分量.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <param name="factor">除数.</param>
    /// <returns>相除后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexCubePoint operator /(HexCubePoint cell, double factor)
    {
        return new HexCubePoint(cell.Q / factor, cell.R / factor);
    }

}
