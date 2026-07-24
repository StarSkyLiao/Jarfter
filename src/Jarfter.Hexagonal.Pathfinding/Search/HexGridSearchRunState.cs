using System.Diagnostics;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 保存一次范围扩张请求跨阶段共用的时间、节点预算、统计和直视缓存.
/// 该状态仅在启用范围策略时创建, 避免为常规单阶段搜索增加额外分配.
/// </summary>
internal sealed class HexGridSearchRunState
{
    private int m_ExpandedNodeCount;

    /// <summary>
    /// 初始化一次范围扩张请求的累计运行状态.
    /// </summary>
    /// <param name="mode">当前使用的格心连接规则.</param>
    /// <param name="requestOptions">本次搜索的运行选项.</param>
    /// <param name="usesStatelessLineOfSightCache">是否由稀疏搜索后端持有直视缓存.</param>
    internal HexGridSearchRunState(
        HexGridSearchMode mode,
        HexPathfindingRequestOptions requestOptions,
        bool usesStatelessLineOfSightCache)
    {
        StartTimestamp = Stopwatch.GetTimestamp();
        StatisticsCollector = requestOptions.CollectStatistics ? new HexPathfindingStatisticsCollector() : null;
        LineOfSightCache = usesStatelessLineOfSightCache && HexGridSearchRuntime.ShouldEnableLineOfSightCache(mode, requestOptions)
            ? new Dictionary<HexGridSearchTypes.LineOfSightCacheKey, HexGridSearchTypes.LineOfSightCacheEntry>()
            : null;
    }

    /// <summary>
    /// 获取整个范围扩张请求开始时的高精度时间戳.
    /// </summary>
    internal long StartTimestamp { get; }

    /// <summary>
    /// 获取跨所有阶段累计的可选统计收集器.
    /// </summary>
    internal HexPathfindingStatisticsCollector? StatisticsCollector { get; }

    /// <summary>
    /// 获取稀疏搜索后端跨阶段复用的直视缓存.
    /// </summary>
    internal Dictionary<HexGridSearchTypes.LineOfSightCacheKey, HexGridSearchTypes.LineOfSightCacheEntry>? LineOfSightCache { get; }

    /// <summary>
    /// 获取工作区后端是否已初始化至少一个搜索阶段.
    /// </summary>
    internal bool HasStarted { get; private set; }

    /// <summary>
    /// 标记工作区后端已完成本次请求的首个阶段初始化.
    /// </summary>
    internal void MarkStarted()
    {
        HasStarted = true;
    }

    /// <summary>
    /// 尝试为一个将要展开的节点消耗全局节点预算.
    /// </summary>
    /// <param name="maximumExpandedNodeCount">值为 0 时表示不限制的节点预算.</param>
    /// <returns>当仍可展开节点时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    internal bool TryConsumeExpansionBudget(int maximumExpandedNodeCount)
    {
        if (maximumExpandedNodeCount > 0 && m_ExpandedNodeCount >= maximumExpandedNodeCount)
        {
            return false;
        }

        m_ExpandedNodeCount++;
        StatisticsCollector?.AddExpandedNode();
        return true;
    }
}
