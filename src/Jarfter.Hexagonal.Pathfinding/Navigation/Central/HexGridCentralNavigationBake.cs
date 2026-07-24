using System.Runtime.CompilerServices;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Grid;
using Jarfter.Hexagonal.Pathfinding.Navigation.Model;

namespace Jarfter.Hexagonal.Pathfinding.Navigation.Central;

/// <summary>
/// 保存中心六边形稠密导航地图中不随地形或障碍变化的拓扑数据.
/// 一个实例可由同尺寸地图的多个快照共享, 用于将格子坐标转换为紧凑索引并快速访问六邻居.
/// </summary>
internal sealed class HexGridCentralNavigationBake
{
    // 障碍块边长固定为 8, 因此算术右移可等价于向负无穷取整的除法.
    private const int ObstacleChunkShift = 3;

    /// <summary>
    /// 获取轴向空间索引中每个障碍块包含的格子边长.
    /// </summary>
    internal const int ObstacleChunkSize = 8;

    private readonly HexagonalCubePoint[] m_Points;
    private readonly int[] m_AxialIndexes;
    private readonly int m_AxialIndexWidth;
    private readonly int[] m_NeighborIndexes;
    private readonly int[] m_ObstacleChunkIndexes;

    /// <summary>
    /// 使用指定中心六边形半径创建烘焙拓扑数据.
    /// </summary>
    /// <param name="radius">中心六边形区域的非负半径.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="radius"/> 为负数时抛出.</exception>
    internal HexGridCentralNavigationBake(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        Radius = radius;
        Count = 1 + 3 * radius + 3 * radius * radius;
        m_Points = new HexagonalCubePoint[Count];
        m_AxialIndexWidth = checked((2 * radius) + 1);
        m_AxialIndexes = new int[checked(m_AxialIndexWidth * m_AxialIndexWidth)];
        // 方形包围盒中不属于中心六边形的坐标保留 -1.
        Array.Fill(m_AxialIndexes, -1);
        m_NeighborIndexes = new int[checked(Count * 6)];
        ObstacleChunkMinimumQ = -radius >> ObstacleChunkShift;
        ObstacleChunkMinimumR = -radius >> ObstacleChunkShift;
        ObstacleChunkCountQ = (radius >> ObstacleChunkShift) - ObstacleChunkMinimumQ + 1;
        ObstacleChunkCountR = (radius >> ObstacleChunkShift) - ObstacleChunkMinimumR + 1;
        ObstacleChunkCount = checked(ObstacleChunkCountQ * ObstacleChunkCountR);
        m_ObstacleChunkIndexes = new int[Count];

        for (int index = 0; index < Count; index++)
        {
            m_Points[index] = HexGridCentral<HexNavigationCell>.FromIndex(index);
            m_AxialIndexes[GetAxialIndexUnchecked(m_Points[index].Q, m_Points[index].R)] = index;
            m_ObstacleChunkIndexes[index] = GetObstacleChunkIndexUnchecked(m_Points[index].Q, m_Points[index].R);
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
    internal int Radius { get; }

    /// <summary>
    /// 获取烘焙地图包含的格子数量.
    /// </summary>
    internal int Count { get; }

    /// <summary>
    /// 获取障碍块在 Q 轴上的最小块坐标.
    /// </summary>
    internal int ObstacleChunkMinimumQ { get; }

    /// <summary>
    /// 获取障碍块在 R 轴上的最小块坐标.
    /// </summary>
    internal int ObstacleChunkMinimumR { get; }

    /// <summary>
    /// 获取障碍块在 Q 轴上的数量.
    /// </summary>
    internal int ObstacleChunkCountQ { get; }

    /// <summary>
    /// 获取障碍块在 R 轴上的数量.
    /// </summary>
    internal int ObstacleChunkCountR { get; }

    /// <summary>
    /// 获取障碍块总数.
    /// </summary>
    internal int ObstacleChunkCount { get; }

    /// <summary>
    /// 获取指定稠密索引对应的六边形格子坐标.
    /// </summary>
    /// <param name="index">位于 <c>[0, Count)</c> 范围内的稠密索引.</param>
    /// <returns>对应的格子坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 不在有效范围内时抛出.</exception>
    internal HexagonalCubePoint GetPoint(int index)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryGetIndex(HexagonalCubePoint point, out int index)
    {
        int qOffset = point.Q + Radius;
        int rOffset = point.R + Radius;

        if ((uint)qOffset >= (uint)m_AxialIndexWidth || (uint)rOffset >= (uint)m_AxialIndexWidth)
        {
            index = -1;
            return false;
        }

        index = m_AxialIndexes[(qOffset * m_AxialIndexWidth) + rOffset];
        return index >= 0;
    }

    /// <summary>
    /// 获取指定格子在给定方向上的相邻格子稠密索引.
    /// </summary>
    /// <param name="index">源格子的稠密索引.</param>
    /// <param name="direction">六边形方向索引, 范围为 <c>[0, 6)</c>.</param>
    /// <returns>相邻格子的稠密索引; 当相邻格子越出地图范围时返回 -1.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="index"/> 或 <paramref name="direction"/> 无效时抛出.</exception>
    internal int GetNeighborIndex(int index, int direction)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        ArgumentOutOfRangeException.ThrowIfNegative(direction);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(direction, 6);

        return m_NeighborIndexes[index * 6 + direction];
    }

    internal int GetObstacleChunkIndex(int cellIndex)
    {
        return m_ObstacleChunkIndexes[cellIndex];
    }

    internal int GetObstacleChunkIndexUnchecked(int q, int r)
    {
        return (GetObstacleChunkQUnchecked(q) * ObstacleChunkCountR) + GetObstacleChunkRUnchecked(r);
    }

    internal int GetObstacleChunkQUnchecked(int q)
    {
        return (q >> ObstacleChunkShift) - ObstacleChunkMinimumQ;
    }

    internal int GetObstacleChunkRUnchecked(int r)
    {
        return (r >> ObstacleChunkShift) - ObstacleChunkMinimumR;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetAxialIndexUnchecked(int q, int r)
    {
        return (((q + Radius) * m_AxialIndexWidth) + r) + Radius;
    }

}
