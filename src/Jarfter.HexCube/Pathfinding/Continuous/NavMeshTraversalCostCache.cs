using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 缓存同一不可变导航快照中 NavMesh 有向相邻边的精确通行代价.
/// 缓存仅复用完全相同的有向线段, 因此不会改变浮点舍入顺序或寻路结果.
/// </summary>
internal sealed class NavMeshTraversalCostCache
{
    private const long UncomputedCostBits = long.MinValue;

    private readonly IContinuousNavigationSnapshot m_Snapshot;
    private readonly int m_EdgeCount;
    private long[]? m_CostBits;

    /// <summary>
    /// 使用指定快照和 NavMesh 单元数量创建有向边代价缓存.
    /// 具体缓存数组会在首次读取代价时延迟分配.
    /// </summary>
    /// <param name="snapshot">对应的不可变导航快照.</param>
    /// <param name="cellCount">NavMesh 中的可通行单元数量.</param>
    internal NavMeshTraversalCostCache(IContinuousNavigationSnapshot snapshot, int cellCount)
    {
        m_Snapshot = snapshot;
        m_EdgeCount = checked(cellCount * 6);
    }

    /// <summary>
    /// 获取指定单元沿指定方向通向相邻单元的精确通行代价.
    /// </summary>
    /// <param name="cellIndex">起始单元索引.</param>
    /// <param name="direction">六边形相邻方向, 范围为 [0, 5].</param>
    /// <param name="start">有向边起点.</param>
    /// <param name="end">有向边终点.</param>
    /// <returns>由导航快照计算的精确通行代价.</returns>
    internal double GetCost(int cellIndex, int direction, HexCubePoint start, HexCubePoint end)
    {
        long[] costBits = GetOrCreateCostBits();
        int edgeIndex = cellIndex * 6 + direction;
        long cachedBits = Volatile.Read(ref costBits[edgeIndex]);

        if (cachedBits != UncomputedCostBits)
        {
            return BitConverter.Int64BitsToDouble(cachedBits);
        }

        double cost = m_Snapshot.GetLineCost(new HexCubeLine2D(start, end));
        long costBitsValue = BitConverter.DoubleToInt64Bits(cost);
        Interlocked.CompareExchange(ref costBits[edgeIndex], costBitsValue, UncomputedCostBits);
        return cost;
    }

    private long[] GetOrCreateCostBits()
    {
        long[]? costBits = Volatile.Read(ref m_CostBits);

        if (costBits is not null)
        {
            return costBits;
        }

        long[] createdCostBits = new long[m_EdgeCount];
        Array.Fill(createdCostBits, UncomputedCostBits);
        return Interlocked.CompareExchange(ref m_CostBits, createdCostBits, null) ?? createdCostBits;
    }
}
