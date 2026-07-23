using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.MapProvider;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 保存中心六边形稠密导航地图中不随地形或障碍变化的拓扑数据.
/// 一个实例可由同尺寸地图的多个快照共享, 用于将格子坐标转换为紧凑索引并快速访问六邻居.
/// </summary>
public sealed class HexGridCentralNavigationBake
{
    private readonly HexagonalCubePoint[] m_Points;
    private readonly int[] m_NeighborIndexes;

    /// <summary>
    /// 使用指定中心六边形半径创建烘焙拓扑数据.
    /// </summary>
    /// <param name="radius">中心六边形区域的非负半径.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="radius"/> 为负数时抛出.</exception>
    public HexGridCentralNavigationBake(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        Radius = radius;
        Count = 1 + 3 * radius + 3 * radius * radius;
        m_Points = new HexagonalCubePoint[Count];
        m_NeighborIndexes = new int[checked(Count * 6)];

        for (int index = 0; index < Count; index++)
        {
            m_Points[index] = HexGridCentralProvider<HexNavigationCell>.FromIndex(index);
        }

        for (int index = 0; index < Count; index++)
        {
            HexagonalCubePoint point = m_Points[index];

            for (int direction = 0; direction < 6; direction++)
            {
                HexagonalCubePoint neighbor = point.NeighborAtUnchecked(direction);
                m_NeighborIndexes[index * 6 + direction] = TryGetIndex(neighbor, out int neighborIndex)
                    ? neighborIndex
                    : -1;
            }
        }
    }

    /// <summary>
    /// 获取中心六边形区域的半径.
    /// </summary>
    public int Radius { get; }

    /// <summary>
    /// 获取烘焙地图包含的格子数量.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// 获取指定稠密索引对应的六边形格子坐标.
    /// </summary>
    /// <param name="index">位于 <c>[0, Count)</c> 范围内的稠密索引.</param>
    /// <returns>对应的格子坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 不在有效范围内时抛出.</exception>
    public HexagonalCubePoint GetPoint(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return m_Points[index];
    }

    /// <summary>
    /// 尝试获取指定格子坐标的稠密索引.
    /// </summary>
    /// <param name="point">要转换的格子坐标.</param>
    /// <param name="index">转换成功时得到的稠密索引.</param>
    /// <returns>当坐标位于烘焙地图范围内时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    public bool TryGetIndex(HexagonalCubePoint point, out int index)
    {
        if (HexagonalCubePoint.Zero.DistanceTo(point) > Radius)
        {
            index = -1;
            return false;
        }

        index = HexGridCentralProvider<HexNavigationCell>.ToIndex(point);
        return true;
    }

    /// <summary>
    /// 获取指定格子在给定方向上的相邻格子稠密索引.
    /// </summary>
    /// <param name="index">源格子的稠密索引.</param>
    /// <param name="direction">六边形方向索引, 范围为 <c>[0, 6)</c>.</param>
    /// <returns>相邻格子的稠密索引; 当相邻格子越出地图范围时返回 -1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 或 <paramref name="direction"/> 无效时抛出.</exception>
    public int GetNeighborIndex(int index, int direction)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        ArgumentOutOfRangeException.ThrowIfNegative(direction);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(direction, 6);

        return m_NeighborIndexes[index * 6 + direction];
    }
}
