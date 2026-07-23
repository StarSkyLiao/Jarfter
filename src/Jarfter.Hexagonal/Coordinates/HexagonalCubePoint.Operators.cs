using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jarfter.Hexagonal.Coordinates;

public readonly partial record struct HexagonalCubePoint :
    IUnaryPlusOperators<HexagonalCubePoint, HexagonalCubePoint>,
    IUnaryNegationOperators<HexagonalCubePoint, HexagonalCubePoint>,
    IAdditionOperators<HexagonalCubePoint, HexagonalCubePoint, HexagonalCubePoint>,
    ISubtractionOperators<HexagonalCubePoint, HexagonalCubePoint, HexagonalCubePoint>,
    IMultiplyOperators<HexagonalCubePoint, int, HexagonalCubePoint>,
    IDivisionOperators<HexagonalCubePoint, int, HexagonalCubePoint>
{
    /// <summary>
    /// 返回坐标自身.
    /// </summary>
    /// <param name="value">坐标.</param>
    /// <returns>未改变的坐标.</returns>
    public static HexagonalCubePoint operator +(HexagonalCubePoint value) => value;

    /// <summary>
    /// 返回相对于原点的反向坐标.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <returns>反向坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalCubePoint operator -(HexagonalCubePoint cell)
    {
        return new HexagonalCubePoint(-cell.Q, -cell.R);
    }

    /// <summary>
    /// 对两个坐标的分量执行加法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相加后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalCubePoint operator +(HexagonalCubePoint left, HexagonalCubePoint right)
    {
        return new HexagonalCubePoint(left.Q + right.Q, left.R + right.R);
    }

    /// <summary>
    /// 对两个坐标的分量执行减法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相减后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalCubePoint operator -(HexagonalCubePoint left, HexagonalCubePoint right)
    {
        return new HexagonalCubePoint(left.Q - right.Q, left.R - right.R);
    }

    /// <summary>
    /// 使用标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <param name="factor">缩放因子.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalCubePoint operator *(HexagonalCubePoint cell, int factor)
    {
        return new HexagonalCubePoint(cell.Q * factor, cell.R * factor);
    }

    /// <summary>
    /// 使用标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="factor">缩放因子.</param>
    /// <param name="cell">坐标.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalCubePoint operator *(int factor, HexagonalCubePoint cell)
    {
        return new HexagonalCubePoint(cell.Q * factor, cell.R * factor);
    }

    /// <summary>
    /// 使用标量除以坐标的各个分量.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <param name="factor">除数.</param>
    /// <returns>相除后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalCubePoint operator /(HexagonalCubePoint cell, int factor)
    {
        return new HexagonalCubePoint(cell.Q / factor, cell.R / factor);
    }

}
