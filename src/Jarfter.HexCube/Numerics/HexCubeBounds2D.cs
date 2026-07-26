namespace Jarfter.HexCube.Numerics;

/// <summary>
/// 表示以 Cube 坐标三个轴的取值范围描述的二维包围盒.
/// 该类型仅用于连续六边形几何的快速粗筛, 不能替代精确的六边形相交判断.
/// </summary>
/// <param name="MinimumQ">Q 轴的最小值.</param>
/// <param name="MaximumQ">Q 轴的最大值.</param>
/// <param name="MinimumR">R 轴的最小值.</param>
/// <param name="MaximumR">R 轴的最大值.</param>
/// <param name="MinimumS">S 轴的最小值.</param>
/// <param name="MaximumS">S 轴的最大值.</param>
public readonly record struct HexCubeBounds2D(
    double MinimumQ,
    double MaximumQ,
    double MinimumR,
    double MaximumR,
    double MinimumS,
    double MaximumS)
{
    /// <summary>
    /// 根据指定的六边形区域创建包围盒.
    /// </summary>
    /// <param name="area">要转换的六边形区域.</param>
    /// <returns>完全包含 <paramref name="area"/> 的包围盒.</returns>
    public static HexCubeBounds2D FromArea(HexCubeArea2D area)
    {
        double radius = area.RadiusScale;
        HexCubePoint position = area.Position;
        return new HexCubeBounds2D(
            position.Q - radius, position.Q + radius,
            position.R - radius, position.R + radius,
            position.S - radius, position.S + radius);
    }

    /// <summary>
    /// 根据指定线段创建包围盒.
    /// </summary>
    /// <param name="line">要转换的线段.</param>
    /// <returns>完全包含 <paramref name="line"/> 的包围盒.</returns>
    public static HexCubeBounds2D FromLine(HexCubeLine2D line)
    {
        return new HexCubeBounds2D(
            Math.Min(line.Start.Q, line.End.Q), Math.Max(line.Start.Q, line.End.Q),
            Math.Min(line.Start.R, line.End.R), Math.Max(line.Start.R, line.End.R),
            Math.Min(line.Start.S, line.End.S), Math.Max(line.Start.S, line.End.S));
    }

    /// <summary>
    /// 获取同时包含当前包围盒和指定包围盒的最小包围盒.
    /// </summary>
    /// <param name="other">要合并的另一个包围盒.</param>
    /// <returns>合并后的包围盒.</returns>
    public HexCubeBounds2D Union(HexCubeBounds2D other)
    {
        return new HexCubeBounds2D(
            Math.Min(MinimumQ, other.MinimumQ), Math.Max(MaximumQ, other.MaximumQ),
            Math.Min(MinimumR, other.MinimumR), Math.Max(MaximumR, other.MaximumR),
            Math.Min(MinimumS, other.MinimumS), Math.Max(MaximumS, other.MaximumS));
    }

    /// <summary>
    /// 创建在各个 Cube 坐标轴方向均扩大指定距离后的包围盒.
    /// </summary>
    /// <param name="amount">要增加的距离. 必须为有限非负数.</param>
    /// <returns>扩大后的包围盒.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="amount"/> 不是有限非负数时抛出.</exception>
    public HexCubeBounds2D Expand(double amount)
    {
        if (!(amount >= 0) || !double.IsFinite(amount))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Expansion amount must be a finite non-negative number.");
        }

        return new HexCubeBounds2D(
            MinimumQ - amount, MaximumQ + amount,
            MinimumR - amount, MaximumR + amount,
            MinimumS - amount, MaximumS + amount);
    }

    /// <summary>
    /// 判断当前包围盒是否与指定包围盒相交或接触.
    /// </summary>
    /// <param name="other">要判断的另一个包围盒.</param>
    /// <returns>两个包围盒存在重叠或接触时返回 true, 否则返回 false.</returns>
    public bool Intersects(HexCubeBounds2D other)
    {
        return MinimumQ <= other.MaximumQ && MaximumQ >= other.MinimumQ &&
               MinimumR <= other.MaximumR && MaximumR >= other.MinimumR &&
               MinimumS <= other.MaximumS && MaximumS >= other.MinimumS;
    }
}
