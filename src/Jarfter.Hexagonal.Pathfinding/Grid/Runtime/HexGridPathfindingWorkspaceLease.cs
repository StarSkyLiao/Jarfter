namespace Jarfter.Hexagonal.Pathfinding.Grid;

/// <summary>
/// 表示从 <see cref="HexGridPathfindingWorkspacePool"/> 租用的独占工作区.
/// 在完成一次同步寻路后应立即释放租约; 重复释放是安全的.
/// </summary>
public sealed class HexGridPathfindingWorkspaceLease : IDisposable
{
    private HexGridPathfindingWorkspacePool? m_Pool;

    internal HexGridPathfindingWorkspaceLease(
        HexGridPathfindingWorkspacePool pool,
        HexGridPathfindingWorkspace workspace)
    {
        m_Pool = pool;
        Workspace = workspace;
    }

    /// <summary>
    /// 获取租约持有的独占工作区.
    /// 在释放租约前, 此实例只能由一个同步寻路调用使用.
    /// </summary>
    public HexGridPathfindingWorkspace Workspace { get; }

    /// <summary>
    /// 将工作区归还到所属池. 重复调用不会重复归还.
    /// </summary>
    public void Dispose()
    {
        HexGridPathfindingWorkspacePool? pool = Interlocked.Exchange(ref m_Pool, null);
        pool?.Return(Workspace);
    }
}
