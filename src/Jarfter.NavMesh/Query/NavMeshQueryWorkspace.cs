using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Query;

/// <summary>
/// 可由单一调用线程复用的路径查询工作区.
/// </summary>
public sealed class NavMeshQueryWorkspace
{
    internal double[] Costs = [];
    internal int[] Parents = [];
    internal int[] ParentJumps = [];
    internal NavMeshPoint[] Positions = [];
    internal PriorityQueue<int, double> Open = new PriorityQueue<int, double>();
    internal double[] JumpHeuristicCosts = [];
    internal bool[] JumpHeuristicClosed = [];
    private int[] m_QueryGenerations = [];
    private byte[] m_States = [];
    private int m_QueryGeneration;

    internal void Reset(int triangleCount)
    {
        if (Costs.Length < triangleCount)
        {
            Costs = new double[triangleCount];
            Parents = new int[triangleCount];
            ParentJumps = new int[triangleCount];
            Positions = new NavMeshPoint[triangleCount];
            m_QueryGenerations = new int[triangleCount];
            m_States = new byte[triangleCount];
        }

        if (m_QueryGeneration == int.MaxValue)
        {
            Array.Clear(m_QueryGenerations);
            m_QueryGeneration = 1;
        }
        else
        {
            m_QueryGeneration++;
        }

        Open.Clear();
    }

    internal double GetCost(int triangleIndex)
    {
        return m_QueryGenerations[triangleIndex] == m_QueryGeneration
            ? Costs[triangleIndex]
            : double.PositiveInfinity;
    }

    internal void SetOpen(int triangleIndex, double cost, int parent, int parentJump = -1)
    {
        m_QueryGenerations[triangleIndex] = m_QueryGeneration;
        m_States[triangleIndex] = 1;
        Costs[triangleIndex] = cost;
        Parents[triangleIndex] = parent;
        ParentJumps[triangleIndex] = parentJump;
    }

    internal bool TryClose(int triangleIndex)
    {
        if (m_QueryGenerations[triangleIndex] != m_QueryGeneration || m_States[triangleIndex] == 2) return false;
        m_States[triangleIndex] = 2;
        return true;
    }

    internal bool IsClosed(int triangleIndex)
    {
        return m_QueryGenerations[triangleIndex] == m_QueryGeneration && m_States[triangleIndex] == 2;
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
