namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubeArea2D
{
    /// <summary>
    /// 获取当前六边形区域的 Cube 坐标包围盒.
    /// </summary>
    public HexCubeBounds2D Bounds => HexCubeBounds2D.FromArea(this);

    /// <summary>
    /// 创建在各个方向均扩大指定距离后的六边形区域.
    /// 平行正六边形的 Minkowski 和仍为正六边形, 因此可用于将具有半径的移动单位转换为点单位导航问题.
    /// </summary>
    /// <param name="amount">要增加的边长比例. 必须为有限非负数.</param>
    /// <returns>扩大后的六边形区域.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="amount"/> 不是有限非负数, 或扩大后的半径不是有限非负数时抛出.</exception>
    public HexCubeArea2D Inflate(double amount)
    {
        if (!(amount >= 0) || !double.IsFinite(amount))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Inflation amount must be a finite non-negative number.");
        }

        double radius = RadiusScale + amount;

        if (!(radius >= 0) || !double.IsFinite(radius))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Inflated radius must be a finite non-negative number.");
        }

        return new HexCubeArea2D(Position, radius);
    }

    /// <summary>
    /// 获取指定索引处的六边形顶点.
    /// 顶点按顺时针顺序排列, 索引范围为 [0, 5].
    /// </summary>
    /// <param name="index">顶点索引.</param>
    /// <returns>指定顶点的连续坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 不在 [0, 5] 范围内时抛出.</exception>
    public HexCubePoint GetVertex(int index)
    {
        double radius = RadiusScale;

        return index switch
        {
            0 => Position + new HexCubePoint(radius, 0),
            1 => Position + new HexCubePoint(radius, -radius),
            2 => Position + new HexCubePoint(0, -radius),
            3 => Position + new HexCubePoint(-radius, 0),
            4 => Position + new HexCubePoint(-radius, radius),
            5 => Position + new HexCubePoint(0, radius),
            _ => throw new ArgumentOutOfRangeException(nameof(index), index, "Vertex index must be in the range [0, 5].")
        };
    }
}
