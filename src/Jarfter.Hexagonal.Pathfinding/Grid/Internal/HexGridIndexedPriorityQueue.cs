namespace Jarfter.Hexagonal.Pathfinding.Grid;

/// <summary>
/// 为稠密节点索引提供可原地降低优先级的最小二叉堆.
/// 通过调用方提供的代际编号区分不同搜索, 因而重置时只需清空堆计数而无需遍历全部容量.
/// </summary>
internal sealed class HexGridIndexedPriorityQueue
{
    private readonly int[] m_Generations;
    private readonly int[] m_Positions;
    private readonly int[] m_Nodes;
    private readonly double[] m_Priorities;
    private int m_Count;

    /// <summary>
    /// 创建指定节点容量的索引最小堆.
    /// </summary>
    internal HexGridIndexedPriorityQueue(int capacity)
    {
        m_Generations = new int[capacity];
        m_Positions = new int[capacity];
        m_Nodes = new int[capacity];
        m_Priorities = new double[capacity];
    }

    /// <summary>
    /// 为新的搜索阶段重置堆内容.
    /// </summary>
    internal void Reset()
    {
        m_Count = 0;
    }

    /// <summary>
    /// 清除代际标记, 用于调用方代际编号溢出后的恢复.
    /// </summary>
    internal void ClearGenerations()
    {
        Array.Clear(m_Generations);
    }

    /// <summary>
    /// 插入节点或降低已在当前代际中入堆节点的优先级.
    /// </summary>
    internal void EnqueueOrDecreasePriority(int nodeIndex, double priority, int generation)
    {
        if (m_Generations[nodeIndex] == generation)
        {
            int heapIndex = m_Positions[nodeIndex];
            m_Priorities[heapIndex] = priority;
            BubbleUp(heapIndex);
            return;
        }

        int newHeapIndex = m_Count++;
        m_Generations[nodeIndex] = generation;
        m_Nodes[newHeapIndex] = nodeIndex;
        m_Priorities[newHeapIndex] = priority;
        m_Positions[nodeIndex] = newHeapIndex;
        BubbleUp(newHeapIndex);
    }

    /// <summary>
    /// 尝试移除并返回最小优先级节点.
    /// </summary>
    internal bool TryDequeue(out int nodeIndex)
    {
        if (m_Count == 0)
        {
            nodeIndex = -1;
            return false;
        }

        nodeIndex = m_Nodes[0];
        m_Generations[nodeIndex] = 0;
        m_Positions[nodeIndex] = -1;
        m_Count--;

        if (m_Count > 0)
        {
            m_Nodes[0] = m_Nodes[m_Count];
            m_Priorities[0] = m_Priorities[m_Count];
            m_Positions[m_Nodes[0]] = 0;
            BubbleDown(0);
        }

        return true;
    }

    private void BubbleUp(int heapIndex)
    {
        while (heapIndex > 0)
        {
            int parentIndex = (heapIndex - 1) / 2;

            if (m_Priorities[parentIndex] <= m_Priorities[heapIndex])
            {
                return;
            }

            SwapEntries(parentIndex, heapIndex);
            heapIndex = parentIndex;
        }
    }

    private void BubbleDown(int heapIndex)
    {
        while (true)
        {
            int leftChildIndex = heapIndex * 2 + 1;
            if (leftChildIndex >= m_Count)
            {
                return;
            }

            int rightChildIndex = leftChildIndex + 1;
            int smallestChildIndex = rightChildIndex < m_Count
                && m_Priorities[rightChildIndex] < m_Priorities[leftChildIndex]
                ? rightChildIndex
                : leftChildIndex;

            if (m_Priorities[heapIndex] <= m_Priorities[smallestChildIndex])
            {
                return;
            }

            SwapEntries(heapIndex, smallestChildIndex);
            heapIndex = smallestChildIndex;
        }
    }

    private void SwapEntries(int leftIndex, int rightIndex)
    {
        (m_Nodes[leftIndex], m_Nodes[rightIndex]) = (m_Nodes[rightIndex], m_Nodes[leftIndex]);
        (m_Priorities[leftIndex], m_Priorities[rightIndex]) = (m_Priorities[rightIndex], m_Priorities[leftIndex]);
        m_Positions[m_Nodes[leftIndex]] = leftIndex;
        m_Positions[m_Nodes[rightIndex]] = rightIndex;
    }
}
