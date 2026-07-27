using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Tiles;

/// <summary>
/// 可由单一调用线程复用的跨 tile polygon 查询工作区.
/// 工作区使用查询代次延迟初始化节点状态, 因而不会在每次查询时清空全部组合 polygon 数组.
/// </summary>
public sealed class TiledNavMeshQueryWorkspace
{
    internal double[] Costs = [];
    internal int[] Parents = [];
    internal TiledNavMeshTransition[] ParentTransitions = [];
    internal PriorityQueue<int, double> Open = new PriorityQueue<int, double>();
    internal double[] JumpHeuristicCosts = [];
    internal bool[] JumpHeuristicClosed = [];
    private int[] m_Generations = [];
    private byte[] m_States = [];
    private int m_Generation;

    /// <summary>
    /// 初始化跨 tile 查询工作区.
    /// </summary>
    public TiledNavMeshQueryWorkspace()
    {
    }

    internal void Reset(int nodeCount)
    {
        if (Costs.Length < nodeCount)
        {
            Costs = new double[nodeCount];
            Parents = new int[nodeCount];
            ParentTransitions = new TiledNavMeshTransition[nodeCount];
            m_Generations = new int[nodeCount];
            m_States = new byte[nodeCount];
        }

        if (m_Generation == int.MaxValue)
        {
            Array.Clear(m_Generations);
            m_Generation = 1;
        }
        else
        {
            m_Generation++;
        }

        Open.Clear();
    }

    internal double GetCost(int node)
    {
        return m_Generations[node] == m_Generation ? Costs[node] : double.PositiveInfinity;
    }

    internal void SetOpen(int node, double cost, int parent, TiledNavMeshTransition transition = default)
    {
        m_Generations[node] = m_Generation;
        m_States[node] = 1;
        Costs[node] = cost;
        Parents[node] = parent;
        ParentTransitions[node] = transition;
    }

    internal bool TryClose(int node)
    {
        if (m_Generations[node] != m_Generation || m_States[node] == 2) return false;
        m_States[node] = 2;
        return true;
    }

    internal void ResetJumpHeuristic(int jumpCount)
    {
        if (JumpHeuristicCosts.Length < jumpCount)
        {
            JumpHeuristicCosts = new double[jumpCount];
            JumpHeuristicClosed = new bool[jumpCount];
        }

        Array.Clear(JumpHeuristicClosed, 0, jumpCount);
    }
}

/// <summary>
/// 表示组合 polygon 搜索父链中的一条几何过渡.
/// </summary>
internal readonly record struct TiledNavMeshTransition(
    TiledNavMeshTransitionKind Kind,
    NavMeshPoint Left,
    NavMeshPoint Right,
    NavMeshPoint JumpStart,
    NavMeshPoint JumpEnd,
    double JumpFixedCost);

/// <summary>
/// 指定组合 polygon 搜索过渡的类型.
/// </summary>
internal enum TiledNavMeshTransitionKind : byte
{
    None,
    Portal,
    Jump
}
