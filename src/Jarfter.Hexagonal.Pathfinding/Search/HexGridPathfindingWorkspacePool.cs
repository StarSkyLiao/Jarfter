using System.Collections.Concurrent;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 为同一份烘焙地图并发租用独占的格心寻路工作区.
/// 池只复用工作区的内部数组和直视缓存; 每次寻路仍由调用方显式传入当前导航快照.
/// </summary>
public sealed class HexGridPathfindingWorkspacePool
{
    private readonly ConcurrentBag<HexGridPathfindingWorkspace> m_AvailableWorkspaces = new ConcurrentBag<HexGridPathfindingWorkspace>();
    private int m_RetainedWorkspaceCount;

    /// <summary>
    /// 为指定烘焙地图创建工作区池, 默认最多保留与逻辑处理器数量相同的空闲工作区.
    /// </summary>
    /// <param name="bake">池中所有工作区必须使用的不可变烘焙地图.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="bake"/> 为 <see langword="null"/> 时抛出.</exception>
    public HexGridPathfindingWorkspacePool(HexGridCentralNavigationBake bake)
        : this(bake, Environment.ProcessorCount)
    {
    }

    /// <summary>
    /// 为指定烘焙地图创建工作区池, 并限制可保留的空闲工作区数量.
    /// </summary>
    /// <param name="bake">池中所有工作区必须使用的不可变烘焙地图.</param>
    /// <param name="maximumRetainedWorkspaceCount">允许保留的最大空闲工作区数. 0 表示归还后立即释放引用.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="bake"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="maximumRetainedWorkspaceCount"/> 为负数时抛出.</exception>
    public HexGridPathfindingWorkspacePool(
        HexGridCentralNavigationBake bake,
        int maximumRetainedWorkspaceCount)
    {
        ArgumentNullException.ThrowIfNull(bake);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumRetainedWorkspaceCount);

        Bake = bake;
        MaximumRetainedWorkspaceCount = maximumRetainedWorkspaceCount;
    }

    /// <summary>
    /// 获取池中工作区适用的不可变烘焙地图.
    /// </summary>
    public HexGridCentralNavigationBake Bake { get; }

    /// <summary>
    /// 获取池允许保留的最大空闲工作区数量.
    /// </summary>
    public int MaximumRetainedWorkspaceCount { get; }

    /// <summary>
    /// 租用一个可供当前同步寻路独占使用的工作区.
    /// 返回的租约必须在寻路结束后调用 <see cref="IDisposable.Dispose"/> 归还.
    /// </summary>
    /// <returns>持有独占工作区的租约.</returns>
    public HexGridPathfindingWorkspaceLease Rent()
    {
        if (m_AvailableWorkspaces.TryTake(out HexGridPathfindingWorkspace? workspace))
        {
            Interlocked.Decrement(ref m_RetainedWorkspaceCount);
            return new HexGridPathfindingWorkspaceLease(this, workspace);
        }

        return new HexGridPathfindingWorkspaceLease(this, new HexGridPathfindingWorkspace(Bake));
    }

    internal void Return(HexGridPathfindingWorkspace workspace)
    {
        if (Interlocked.Increment(ref m_RetainedWorkspaceCount) > MaximumRetainedWorkspaceCount)
        {
            Interlocked.Decrement(ref m_RetainedWorkspaceCount);
            return;
        }

        m_AvailableWorkspaces.Add(workspace);
    }
}
