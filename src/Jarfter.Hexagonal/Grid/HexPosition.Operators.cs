using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jarfter.Hexagonal.Grid;

public partial record struct HexagonalGrid<T> :
    IUnaryPlusOperators<HexagonalGrid<T>, HexagonalGrid<T>>,
    IUnaryNegationOperators<HexagonalGrid<T>, HexagonalGrid<T>>,
    IAdditionOperators<HexagonalGrid<T>, HexagonalGrid<T>, HexagonalGrid<T>>,
    ISubtractionOperators<HexagonalGrid<T>, HexagonalGrid<T>, HexagonalGrid<T>>,
    IMultiplyOperators<HexagonalGrid<T>, T, HexagonalGrid<T>>,
    IDivisionOperators<HexagonalGrid<T>, T, HexagonalGrid<T>>
{
    /// <summary>
    /// 返回坐标自身.
    /// </summary>
    /// <param name="value">坐标.</param>
    /// <returns>未改变的坐标.</returns>
    public static HexagonalGrid<T> operator +(HexagonalGrid<T> value) => value;

    /// <summary>
    /// 返回相对于原点的反向坐标.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <returns>反向坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalGrid<T> operator -(HexagonalGrid<T> cell)
    {
        return new HexagonalGrid<T>(-cell.Q, -cell.R);
    }

    /// <summary>
    /// 对两个坐标的分量执行加法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相加后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalGrid<T> operator +(HexagonalGrid<T> left, HexagonalGrid<T> right)
    {
        return new HexagonalGrid<T>(left.Q + right.Q, left.R + right.R);
    }

    /// <summary>
    /// 对两个坐标的分量执行减法.
    /// </summary>
    /// <param name="left">左操作数.</param>
    /// <param name="right">右操作数.</param>
    /// <returns>分量相减后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalGrid<T> operator -(HexagonalGrid<T> left, HexagonalGrid<T> right)
    {
        return new HexagonalGrid<T>(left.Q - right.Q, left.R - right.R);
    }

    /// <summary>
    /// 使用标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <param name="factor">缩放因子.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalGrid<T> operator *(HexagonalGrid<T> cell, T factor)
    {
        return new HexagonalGrid<T>(cell.Q * factor, cell.R * factor);
    }

    /// <summary>
    /// 使用标量缩放坐标的各个分量.
    /// </summary>
    /// <param name="factor">缩放因子.</param>
    /// <param name="cell">坐标.</param>
    /// <returns>缩放后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalGrid<T> operator *(T factor, HexagonalGrid<T> cell)
    {
        return new HexagonalGrid<T>(cell.Q * factor, cell.R * factor);
    }

    /// <summary>
    /// 使用标量除以坐标的各个分量.
    /// </summary>
    /// <param name="cell">坐标.</param>
    /// <param name="factor">除数.</param>
    /// <returns>相除后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static HexagonalGrid<T> operator /(HexagonalGrid<T> cell, T factor)
    {
        return new HexagonalGrid<T>(cell.Q / factor, cell.R / factor);
    }

}
