using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 表示一次成功格心寻路的工作量统计.
/// 仅当 <see cref="HexPathfindingRequestOptions.CollectStatistics"/> 为 <see langword="true"/> 时附加到路径结果.
/// </summary>
public sealed class HexPathfindingStatistics
{
    internal HexPathfindingStatistics(
        long expandedNodeCount,
        long lineOfSightQueryCount,
        long parentLineOfSightQueryCount,
        long successfulParentLineOfSightQueryCount,
        long lineOfSightCacheHitCount,
        long lineOfSightCacheMissCount,
        long traversedCellCount,
        long nearbyCellQueryCount,
        long obstacleIntersectionTestCount)
    {
        ExpandedNodeCount = expandedNodeCount;
        LineOfSightQueryCount = lineOfSightQueryCount;
        ParentLineOfSightQueryCount = parentLineOfSightQueryCount;
        SuccessfulParentLineOfSightQueryCount = successfulParentLineOfSightQueryCount;
        LineOfSightCacheHitCount = lineOfSightCacheHitCount;
        LineOfSightCacheMissCount = lineOfSightCacheMissCount;
        TraversedCellCount = traversedCellCount;
        NearbyCellQueryCount = nearbyCellQueryCount;
        ObstacleIntersectionTestCount = obstacleIntersectionTestCount;
    }

    /// <summary>
    /// 获取实际展开的搜索节点数量.
    /// </summary>
    public long ExpandedNodeCount { get; }

    /// <summary>
    /// 获取所有连接候选执行的直视检测数量.
    /// </summary>
    public long LineOfSightQueryCount { get; }

    /// <summary>
    /// 获取 Theta* 尝试父节点直接连接的直视检测数量.
    /// A* 中该值始终为 0.
    /// </summary>
    public long ParentLineOfSightQueryCount { get; }

    /// <summary>
    /// 获取 Theta* 父节点直接连接成功的次数.
    /// A* 中该值始终为 0.
    /// </summary>
    public long SuccessfulParentLineOfSightQueryCount { get; }

    /// <summary>
    /// 获取从单次搜索直视缓存读取结果的次数.
    /// 未启用缓存时该值为 0.
    /// </summary>
    public long LineOfSightCacheHitCount { get; }

    /// <summary>
    /// 获取未命中直视缓存并实际执行检测的次数.
    /// 未启用缓存时该值为 0.
    /// </summary>
    public long LineOfSightCacheMissCount { get; }

    /// <summary>
    /// 获取所有直视检测累计遍历的主穿格数量.
    /// </summary>
    public long TraversedCellCount { get; }

    /// <summary>
    /// 获取所有直视检测累计查询的附近格子数量.
    /// </summary>
    public long NearbyCellQueryCount { get; }

    /// <summary>
    /// 获取所有直视检测累计执行的膨胀障碍相交测试数量.
    /// </summary>
    public long ObstacleIntersectionTestCount { get; }
}

/// <summary>
/// 在单次搜索中累加工作量并创建不可变统计结果.
/// </summary>
internal sealed class HexPathfindingStatisticsCollector
{
    private readonly HexLineOfSightMetrics m_LineOfSightMetrics = new HexLineOfSightMetrics();

    /// <summary>
    /// 获取可供直视检测更新的底层统计器.
    /// </summary>
    internal HexLineOfSightMetrics LineOfSightMetrics => m_LineOfSightMetrics;

    /// <summary>
    /// 获取实际展开的搜索节点数量.
    /// </summary>
    internal long ExpandedNodeCount { get; private set; }

    /// <summary>
    /// 获取所有连接候选执行的直视检测数量.
    /// </summary>
    internal long LineOfSightQueryCount { get; private set; }

    /// <summary>
    /// 获取父节点直接连接的直视检测数量.
    /// </summary>
    internal long ParentLineOfSightQueryCount { get; private set; }

    /// <summary>
    /// 获取成功的父节点直接连接数量.
    /// </summary>
    internal long SuccessfulParentLineOfSightQueryCount { get; private set; }

    /// <summary>
    /// 获取直视缓存命中次数.
    /// </summary>
    internal long LineOfSightCacheHitCount { get; private set; }

    /// <summary>
    /// 获取直视缓存未命中次数.
    /// </summary>
    internal long LineOfSightCacheMissCount { get; private set; }

    /// <summary>
    /// 记录一个搜索节点展开.
    /// </summary>
    internal void AddExpandedNode()
    {
        ExpandedNodeCount++;
    }

    /// <summary>
    /// 记录一次直视检测.
    /// </summary>
    /// <param name="isParentConnection">该检测是否尝试父节点直接连接.</param>
    internal void AddLineOfSightQuery(bool isParentConnection)
    {
        LineOfSightQueryCount++;

        if (isParentConnection)
        {
            ParentLineOfSightQueryCount++;
        }
    }

    /// <summary>
    /// 记录一次成功的父节点直接连接.
    /// </summary>
    internal void AddSuccessfulParentLineOfSightQuery()
    {
        SuccessfulParentLineOfSightQueryCount++;
    }

    /// <summary>
    /// 记录一次直视缓存命中.
    /// </summary>
    internal void AddLineOfSightCacheHit()
    {
        LineOfSightCacheHitCount++;
    }

    /// <summary>
    /// 记录一次直视缓存未命中.
    /// </summary>
    internal void AddLineOfSightCacheMiss()
    {
        LineOfSightCacheMissCount++;
    }

    /// <summary>
    /// 创建当前累计值的不可变统计结果.
    /// </summary>
    /// <returns>当前累计值对应的统计结果.</returns>
    internal HexPathfindingStatistics CreateStatistics()
    {
        return new HexPathfindingStatistics(
            ExpandedNodeCount,
            LineOfSightQueryCount,
            ParentLineOfSightQueryCount,
            SuccessfulParentLineOfSightQueryCount,
            LineOfSightCacheHitCount,
            LineOfSightCacheMissCount,
            m_LineOfSightMetrics.TraversedCellCount,
            m_LineOfSightMetrics.NearbyCellQueryCount,
            m_LineOfSightMetrics.ObstacleIntersectionTestCount);
    }
}
