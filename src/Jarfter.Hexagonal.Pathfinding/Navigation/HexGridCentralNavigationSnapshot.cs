using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.MapProvider;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 表示从中心六边形稠密地图复制得到的不可变导航快照.
/// 快照保存连续数组副本, 适用于中小规模、半径已知的有限地图.
/// </summary>
public sealed class HexGridCentralNavigationSnapshot : IHexNavigationSnapshot
{
    private readonly HexNavigationCell[] m_Cells;

    /// <summary>
    /// 从指定中心稠密地图创建导航快照.
    /// 源地图后续的写入不会影响此快照.
    /// </summary>
    /// <param name="map">要复制的中心六边形导航地图.</param>
    /// <param name="version">创建此快照时的导航地图版本, 必须为非负数.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="map"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="version"/> 为负数时抛出.</exception>
    public HexGridCentralNavigationSnapshot(HexGridCentralProvider<HexNavigationCell> map, long version)
        : this(map, version, CreateBake(map))
    {
    }

    internal HexGridCentralNavigationSnapshot(
        HexGridCentralProvider<HexNavigationCell> map,
        long version,
        HexGridCentralNavigationBake bake)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentOutOfRangeException.ThrowIfNegative(version);
        ArgumentNullException.ThrowIfNull(bake);

        if (map.Radius != bake.Radius)
        {
            throw new ArgumentException("烘焙地图半径必须与快照源地图一致.", nameof(bake));
        }

        Radius = map.Radius;
        Version = version;
        Bake = bake;
        m_Cells = map.Elements.ToArray();
        MaximumObstacleApothemScale = GetMaximumObstacleApothemScale(m_Cells);
        MinimumTraversalMultiplier = GetMinimumTraversalMultiplier(m_Cells);
    }

    /// <summary>
    /// 获取中心六边形区域的半径.
    /// </summary>
    public int Radius { get; }

    /// <summary>
    /// 获取快照中包含的格子数量.
    /// </summary>
    public int Count => m_Cells.Length;

    /// <summary>
    /// 获取此快照共享的不可变稠密拓扑烘焙数据.
    /// </summary>
    public HexGridCentralNavigationBake Bake { get; }

    /// <inheritdoc />
    public long Version { get; }

    /// <inheritdoc />
    public double MaximumObstacleApothemScale { get; }

    /// <inheritdoc />
    public double MinimumTraversalMultiplier { get; }

    /// <inheritdoc />
    public bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell)
    {
        if (HexagonalCubePoint.Zero.DistanceTo(point) > Radius)
        {
            cell = default;
            return false;
        }

        cell = m_Cells[HexGridCentralProvider<HexNavigationCell>.ToIndex(point)];
        return true;
    }

    private static double GetMaximumObstacleApothemScale(ReadOnlySpan<HexNavigationCell> cells)
    {
        double maximum = 0;

        foreach (HexNavigationCell cell in cells)
        {
            maximum = Math.Max(maximum, cell.ObstacleApothemScale);
        }

        return maximum;
    }

    private static HexGridCentralNavigationBake CreateBake(HexGridCentralProvider<HexNavigationCell> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return new HexGridCentralNavigationBake(map.Radius);
    }

    private static double GetMinimumTraversalMultiplier(ReadOnlySpan<HexNavigationCell> cells)
    {
        double minimum = double.PositiveInfinity;

        foreach (HexNavigationCell cell in cells)
        {
            if (!cell.HasObstacle)
            {
                minimum = Math.Min(minimum, cell.TraversalMultiplier);
            }
        }

        return double.IsPositiveInfinity(minimum) ? 1 : minimum;
    }
}
