using System.Numerics;

namespace Jarfter.Hexagonal.Grid;

/// <summary>
/// 提供六边形网格的整数计数工具.
/// </summary>
public static class HexagonalGridMetrics
{
    /// <summary>
    /// 获取指定半径环上的坐标数量.
    /// </summary>
    /// <typeparam name="TInteger">半径和返回值的整数类型.</typeparam>
    /// <param name="radius">环半径.</param>
    /// <returns>指定半径环上的坐标数量.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当结果超出 TInteger 可表示范围时抛出.</exception>
    public static TInteger CountInRing<TInteger>(TInteger radius)
        where TInteger : IBinaryInteger<TInteger>
    {
        ThrowIfNegative(radius);
        return radius == TInteger.Zero ? TInteger.One : checked(radius * TInteger.CreateChecked(6));
    }

    /// <summary>
    /// 获取指定半径范围内的坐标数量.
    /// </summary>
    /// <typeparam name="TInteger">半径和返回值的整数类型.</typeparam>
    /// <param name="radius">最大半径.</param>
    /// <returns>半径不超过指定值的坐标数量.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当结果超出 TInteger 可表示范围时抛出.</exception>
    public static TInteger CountInRange<TInteger>(TInteger radius)
        where TInteger : IBinaryInteger<TInteger>
    {
        ThrowIfNegative(radius);
        TInteger three = TInteger.CreateChecked(3);
        return checked(TInteger.One + three * radius * (radius + TInteger.One));
    }

    private static void ThrowIfNegative<TInteger>(TInteger value) where TInteger : IBinaryInteger<TInteger>
    {
        if (value >= TInteger.Zero) return;
        throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be greater than or equal to 0.");
    }
}
