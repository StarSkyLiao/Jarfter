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
    private readonly bool[] m_ObstacleChunks;

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
        m_ObstacleChunks = CreateObstacleChunks(m_Cells, bake);
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

    /// <summary>
    /// 判断指定轴向矩形覆盖的障碍块中是否至少存在一个障碍格.
    /// 该方法仅用于直视检测的保守粗筛; 返回 <see langword="true"/> 不代表矩形内必然存在与线段相交的障碍.
    /// </summary>
    /// <param name="minimumQ">轴向矩形的最小 Q 坐标.</param>
    /// <param name="maximumQ">轴向矩形的最大 Q 坐标.</param>
    /// <param name="minimumR">轴向矩形的最小 R 坐标.</param>
    /// <param name="maximumR">轴向矩形的最大 R 坐标.</param>
    /// <returns>当至少一个相交障碍块包含障碍格时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    internal bool HasObstacleInChunkRange(int minimumQ, int maximumQ, int minimumR, int maximumR)
    {
        if (maximumQ < -Radius || minimumQ > Radius || maximumR < -Radius || minimumR > Radius)
        {
            return false;
        }

        int clampedMinimumQ = Math.Max(minimumQ, -Radius);
        int clampedMaximumQ = Math.Min(maximumQ, Radius);
        int clampedMinimumR = Math.Max(minimumR, -Radius);
        int clampedMaximumR = Math.Min(maximumR, Radius);
        int minimumChunkQ = Bake.GetObstacleChunkIndexUnchecked(clampedMinimumQ, 0) / Bake.ObstacleChunkCountR;
        int maximumChunkQ = Bake.GetObstacleChunkIndexUnchecked(clampedMaximumQ, 0) / Bake.ObstacleChunkCountR;
        int minimumChunkR = Bake.GetObstacleChunkIndexUnchecked(0, clampedMinimumR) % Bake.ObstacleChunkCountR;
        int maximumChunkR = Bake.GetObstacleChunkIndexUnchecked(0, clampedMaximumR) % Bake.ObstacleChunkCountR;

        for (int chunkQ = minimumChunkQ; chunkQ <= maximumChunkQ; chunkQ++)
        {
            int rowStart = chunkQ * Bake.ObstacleChunkCountR;

            for (int chunkR = minimumChunkR; chunkR <= maximumChunkR; chunkR++)
            {
                if (m_ObstacleChunks[rowStart + chunkR])
                {
                    return true;
                }
            }
        }

        return false;
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

    private static bool[] CreateObstacleChunks(
        ReadOnlySpan<HexNavigationCell> cells,
        HexGridCentralNavigationBake bake)
    {
        bool[] chunks = new bool[bake.ObstacleChunkCount];

        for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
        {
            if (cells[cellIndex].HasObstacle)
            {
                chunks[bake.GetObstacleChunkIndex(cellIndex)] = true;
            }
        }

        return chunks;
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
