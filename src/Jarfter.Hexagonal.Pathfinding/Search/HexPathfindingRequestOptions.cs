namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 定义一次寻路调用的运行限制.
/// 限制不会保存在路径查找器实例中, 以便不同单位的请求可以独立取消或设置不同预算.
/// </summary>
public sealed class HexPathfindingRequestOptions
{
    /// <summary>
    /// 初始化 <see cref="HexPathfindingRequestOptions"/> 的新实例.
    /// </summary>
    public HexPathfindingRequestOptions()
    {
    }

    /// <summary>
    /// 获取或初始化允许扩展的最大搜索节点数. 值为 0 时不限制节点数.
    /// 达到限制时寻路返回 <see langword="false"/> 且不生成路径.
    /// </summary>
    public int MaximumExpandedNodeCount { get; init; }

    /// <summary>
    /// 获取或初始化格心搜索允许使用的最长时间. 默认的无限时长表示不设置超时限制.
    /// 超时时寻路返回 <see langword="false"/> 且不生成路径.
    /// </summary>
    public TimeSpan Timeout { get; init; } = System.Threading.Timeout.InfiniteTimeSpan;

    /// <summary>
    /// 获取或初始化用于取消此次寻路调用的令牌.
    /// 令牌被取消时寻路抛出 <see cref="OperationCanceledException"/>.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegative(MaximumExpandedNodeCount);

        if (Timeout != System.Threading.Timeout.InfiniteTimeSpan && Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout));
        }
    }
}
