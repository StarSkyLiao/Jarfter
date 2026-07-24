namespace Jarfter.Hexagonal.Pathfinding.Grid.Requests;

/// <summary>
/// 定义一次寻路调用的运行限制与性能策略.
/// 选项不会保存在路径查找器实例中, 以便不同单位的请求可以独立取消、设置预算或选择性能策略.
/// </summary>
public sealed class HexPathfindingRequestOptions()
{
    /// <summary>
    /// 获取或初始化允许扩展的最大搜索节点数. 值为 0 时不限制节点数.
    /// 达到限制时寻路返回 <see langword="null"/> 且不生成路径.
    /// 启用局部范围扩张时, 所有搜索阶段共享这一总预算.
    /// </summary>
    public int MaximumExpandedNodeCount { get; init; }

    /// <summary>
    /// 获取或初始化格心搜索允许使用的最长时间. 默认的无限时长表示不设置超时限制.
    /// 超时时寻路返回 <see langword="null"/> 且不生成路径.
    /// 启用局部范围扩张时, 所有搜索阶段共享这一总时限.
    /// </summary>
    public TimeSpan Timeout { get; init; } = System.Threading.Timeout.InfiniteTimeSpan;

    /// <summary>
    /// 获取或初始化用于取消此次寻路调用的令牌.
    /// 令牌被取消时寻路抛出 <see cref="OperationCanceledException"/>.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// 获取或初始化是否收集成功路径的搜索工作量统计.
    /// 默认不收集, 以避免在热路径中增加诊断计数开销.
    /// 启用局部范围扩张时, 成功路径附带的统计会累计所有已执行阶段的工作量.
    /// </summary>
    public bool CollectStatistics { get; init; }

    /// <summary>
    /// 获取或初始化单次搜索中缓存格心间直视检测结果与成本的策略.
    /// 默认自动策略会为 Theta* 启用缓存, 为 A* 关闭缓存.
    /// </summary>
    public HexLineOfSightCacheMode LineOfSightCacheMode { get; init; } = HexLineOfSightCacheMode.Automatic;

    /// <summary>
    /// 获取或初始化是否在格心搜索中为中心稠密导航快照的直视检测启用障碍块粗筛.
    /// 默认启用; 关闭后仍使用相同的精确六边形碰撞模型, 主要用于诊断或性能对比.
    /// </summary>
    public bool UseObstacleChunkAcceleration { get; init; } = true;

    /// <summary>
    /// 获取或初始化局部范围扩张策略. 为 <see langword="null"/> 时执行无限制搜索并保持全局最低成本语义.
    /// 指定策略后, 寻路会在首个成功范围内返回最低成本路径, 该路径不保证是全图最低成本路径.
    /// </summary>
    public IHexPathSearchScopeStrategy? SearchScopeStrategy { get; init; }

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(MaximumExpandedNodeCount);

        if (Timeout != System.Threading.Timeout.InfiniteTimeSpan && Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout));
        }

        if (!Enum.IsDefined(LineOfSightCacheMode))
        {
            throw new ArgumentOutOfRangeException(nameof(LineOfSightCacheMode));
        }
    }
}
