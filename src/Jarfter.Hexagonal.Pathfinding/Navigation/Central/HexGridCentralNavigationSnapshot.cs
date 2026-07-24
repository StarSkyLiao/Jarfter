using System.Runtime.CompilerServices;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 表示从中心六边形稠密地图复制得到的不可变导航快照.
/// 快照保存连续数组副本, 适用于中小规模、半径已知的有限地图.
/// </summary>
public sealed class HexGridCentralNavigationSnapshot : IHexNavigationSnapshot
{
    private const int MaximumStackAllocatedObstacleChunkCount = 256;

    private readonly HexNavigationCell[] m_Cells;
    private readonly int[] m_ObstacleChunkPrefixSums;

    /// <summary>
    /// 从指定中心稠密地图创建导航快照.
    /// 源地图后续的写入不会影响此快照.
    /// </summary>
    /// <param name="map">要复制的中心六边形导航地图.</param>
    /// <param name="version">创建此快照时的导航地图版本, 必须为非负数.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="map"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="version"/> 为负数时抛出.</exception>
    public HexGridCentralNavigationSnapshot(HexGridCentral<HexNavigationCell> map, long version)
        : this(map, version, CreateBake(map))
    {
    }

    internal HexGridCentralNavigationSnapshot(
        HexGridCentral<HexNavigationCell> map,
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
        Span<bool> obstacleChunks = bake.ObstacleChunkCount <= MaximumStackAllocatedObstacleChunkCount
            ? stackalloc bool[bake.ObstacleChunkCount]
            : new bool[bake.ObstacleChunkCount];
        obstacleChunks.Clear();
        PopulateObstacleChunks(m_Cells, obstacleChunks, bake);
        m_ObstacleChunkPrefixSums = CreateObstacleChunkPrefixSums(obstacleChunks, bake);
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
    /// 此实现细节仅供中心稠密导航与工作区协作使用.
    /// </summary>
    internal HexGridCentralNavigationBake Bake { get; }

    /// <inheritdoc />
    public long Version { get; }

    /// <inheritdoc />
    public double MaximumObstacleApothemScale { get; }

    /// <inheritdoc />
    public double MinimumTraversalMultiplier { get; }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell)
    {
        if (!Bake.TryGetIndex(point, out int index))
        {
            cell = default;
            return false;
        }

        cell = m_Cells[index];
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
        int minimumChunkQ = Bake.GetObstacleChunkQUnchecked(clampedMinimumQ);
        int maximumChunkQ = Bake.GetObstacleChunkQUnchecked(clampedMaximumQ);
        int minimumChunkR = Bake.GetObstacleChunkRUnchecked(clampedMinimumR);
        int maximumChunkR = Bake.GetObstacleChunkRUnchecked(clampedMaximumR);
        int prefixSumRowLength = Bake.ObstacleChunkCountR + 1;
        int maximumChunkQExclusive = maximumChunkQ + 1;
        int maximumChunkRExclusive = maximumChunkR + 1;
        int obstacleCount = m_ObstacleChunkPrefixSums[(maximumChunkQExclusive * prefixSumRowLength) + maximumChunkRExclusive]
            - m_ObstacleChunkPrefixSums[(minimumChunkQ * prefixSumRowLength) + maximumChunkRExclusive]
            - m_ObstacleChunkPrefixSums[(maximumChunkQExclusive * prefixSumRowLength) + minimumChunkR]
            + m_ObstacleChunkPrefixSums[(minimumChunkQ * prefixSumRowLength) + minimumChunkR];
        return obstacleCount > 0;
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

    private static void PopulateObstacleChunks(
        ReadOnlySpan<HexNavigationCell> cells,
        Span<bool> chunks,
        HexGridCentralNavigationBake bake)
    {
        for (int cellIndex = 0; cellIndex < cells.Length; cellIndex++)
        {
            if (cells[cellIndex].HasObstacle)
            {
                chunks[bake.GetObstacleChunkIndex(cellIndex)] = true;
            }
        }
    }

    private static int[] CreateObstacleChunkPrefixSums(
        ReadOnlySpan<bool> chunks,
        HexGridCentralNavigationBake bake)
    {
        int prefixSumRowLength = bake.ObstacleChunkCountR + 1;
        int[] prefixSums = new int[checked((bake.ObstacleChunkCountQ + 1) * prefixSumRowLength)];

        for (int chunkQ = 1; chunkQ <= bake.ObstacleChunkCountQ; chunkQ++)
        {
            int obstacleCountInRow = 0;
            int chunkRowStart = (chunkQ - 1) * bake.ObstacleChunkCountR;
            int prefixSumRowStart = chunkQ * prefixSumRowLength;
            int previousPrefixSumRowStart = (chunkQ - 1) * prefixSumRowLength;

            for (int chunkR = 1; chunkR <= bake.ObstacleChunkCountR; chunkR++)
            {
                if (chunks[chunkRowStart + chunkR - 1])
                {
                    obstacleCountInRow++;
                }

                prefixSums[prefixSumRowStart + chunkR] = prefixSums[previousPrefixSumRowStart + chunkR] + obstacleCountInRow;
            }
        }

        return prefixSums;
    }

    private static HexGridCentralNavigationBake CreateBake(HexGridCentral<HexNavigationCell> map)
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
