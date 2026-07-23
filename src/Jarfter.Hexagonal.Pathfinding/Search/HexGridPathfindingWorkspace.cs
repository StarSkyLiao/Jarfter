using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 为同一份 <see cref="HexGridCentralNavigationBake"/> 复用格心搜索状态.
/// 工作区预分配节点记录、关闭标记、二叉堆和可选直视缓存, 从而避免每次寻路创建字典与队列.
/// 一个实例在任意时刻只能由一个同步寻路调用使用; 并行寻路应为每个工作线程或任务租用独立实例.
/// </summary>
public sealed class HexGridPathfindingWorkspace
{
    private readonly double[] m_Costs;
    private readonly int[] m_Parents;
    private readonly int[] m_RecordGenerations;
    private readonly int[] m_ClosedGenerations;
    private readonly int[] m_OpenGenerations;
    private readonly int[] m_HeapPositions;
    private readonly int[] m_HeapNodes;
    private readonly double[] m_HeapPriorities;
    private readonly Dictionary<LineOfSightCacheKey, LineOfSightCacheEntry> m_LineOfSightCache;
    private int m_Generation;
    private int m_HeapCount;

    /// <summary>
    /// 为指定烘焙地图创建可复用的搜索工作区.
    /// </summary>
    /// <param name="bake">定义工作区容量与稠密索引的不可变烘焙地图.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="bake"/> 为 <see langword="null"/> 时抛出.</exception>
    public HexGridPathfindingWorkspace(HexGridCentralNavigationBake bake)
    {
        ArgumentNullException.ThrowIfNull(bake);

        Bake = bake;
        m_Costs = new double[bake.Count];
        m_Parents = new int[bake.Count];
        m_RecordGenerations = new int[bake.Count];
        m_ClosedGenerations = new int[bake.Count];
        m_OpenGenerations = new int[bake.Count];
        m_HeapPositions = new int[bake.Count];
        m_HeapNodes = new int[bake.Count];
        m_HeapPriorities = new double[bake.Count];
        m_LineOfSightCache = new Dictionary<LineOfSightCacheKey, LineOfSightCacheEntry>();
    }

    /// <summary>
    /// 获取此工作区适用的不可变稠密拓扑烘焙数据.
    /// </summary>
    public HexGridCentralNavigationBake Bake { get; }

    internal bool UsesLineOfSightCache { get; private set; }

    internal void BeginSearch(bool usesLineOfSightCache)
    {
        // 代际标记避免为每次搜索清空与地图同等大小的状态数组.
        if (m_Generation == int.MaxValue)
        {
            Array.Clear(m_RecordGenerations);
            Array.Clear(m_ClosedGenerations);
            Array.Clear(m_OpenGenerations);
            m_Generation = 1;
        }
        else
        {
            m_Generation++;
        }

        m_HeapCount = 0;
        UsesLineOfSightCache = usesLineOfSightCache;

        if (usesLineOfSightCache)
        {
            m_LineOfSightCache.Clear();
        }
    }

    internal bool IsClosed(int index) => m_ClosedGenerations[index] == m_Generation;

    internal void Close(int index)
    {
        m_ClosedGenerations[index] = m_Generation;
    }

    internal bool TryGetRecord(int index, out double cost, out int parentIndex)
    {
        if (m_RecordGenerations[index] == m_Generation)
        {
            cost = m_Costs[index];
            parentIndex = m_Parents[index];
            return true;
        }

        cost = 0;
        parentIndex = -1;
        return false;
    }

    internal void SetRecord(int index, double cost, int parentIndex)
    {
        m_RecordGenerations[index] = m_Generation;
        m_Costs[index] = cost;
        m_Parents[index] = parentIndex;
    }

    internal void EnqueueOrDecreasePriority(int index, double priority)
    {
        if (m_OpenGenerations[index] == m_Generation)
        {
            int heapIndex = m_HeapPositions[index];
            m_HeapPriorities[heapIndex] = priority;
            BubbleUp(heapIndex);
            return;
        }

        int newHeapIndex = m_HeapCount++;
        m_OpenGenerations[index] = m_Generation;
        m_HeapNodes[newHeapIndex] = index;
        m_HeapPriorities[newHeapIndex] = priority;
        m_HeapPositions[index] = newHeapIndex;
        BubbleUp(newHeapIndex);
    }

    internal bool TryDequeue(out int index)
    {
        if (m_HeapCount == 0)
        {
            index = -1;
            return false;
        }

        index = m_HeapNodes[0];
        m_OpenGenerations[index] = 0;
        m_HeapPositions[index] = -1;
        m_HeapCount--;

        if (m_HeapCount > 0)
        {
            m_HeapNodes[0] = m_HeapNodes[m_HeapCount];
            m_HeapPriorities[0] = m_HeapPriorities[m_HeapCount];
            m_HeapPositions[m_HeapNodes[0]] = 0;
            BubbleDown(0);
        }

        return true;
    }

    internal bool TryGetLineOfSightCache(
        HexagonalCubePoint start,
        HexagonalCubePoint end,
        out bool isTraversable,
        out double cost)
    {
        if (UsesLineOfSightCache
            && m_LineOfSightCache.TryGetValue(new LineOfSightCacheKey(start, end), out LineOfSightCacheEntry entry))
        {
            isTraversable = entry.IsTraversable;
            cost = entry.Cost;
            return true;
        }

        isTraversable = false;
        cost = 0;
        return false;
    }

    internal void SetLineOfSightCache(
        HexagonalCubePoint start,
        HexagonalCubePoint end,
        bool isTraversable,
        double cost)
    {
        if (UsesLineOfSightCache)
        {
            m_LineOfSightCache.Add(new LineOfSightCacheKey(start, end), new LineOfSightCacheEntry(isTraversable, cost));
        }
    }

    private void BubbleUp(int heapIndex)
    {
        while (heapIndex > 0)
        {
            int parentIndex = (heapIndex - 1) / 2;

            if (m_HeapPriorities[parentIndex] <= m_HeapPriorities[heapIndex])
            {
                return;
            }

            SwapHeapEntries(parentIndex, heapIndex);
            heapIndex = parentIndex;
        }
    }

    private void BubbleDown(int heapIndex)
    {
        while (true)
        {
            int leftChildIndex = heapIndex * 2 + 1;
            if (leftChildIndex >= m_HeapCount)
            {
                return;
            }

            int rightChildIndex = leftChildIndex + 1;
            int smallestChildIndex = rightChildIndex < m_HeapCount
                && m_HeapPriorities[rightChildIndex] < m_HeapPriorities[leftChildIndex]
                ? rightChildIndex
                : leftChildIndex;

            if (m_HeapPriorities[heapIndex] <= m_HeapPriorities[smallestChildIndex])
            {
                return;
            }

            SwapHeapEntries(heapIndex, smallestChildIndex);
            heapIndex = smallestChildIndex;
        }
    }

    private void SwapHeapEntries(int leftIndex, int rightIndex)
    {
        (m_HeapNodes[leftIndex], m_HeapNodes[rightIndex]) = (m_HeapNodes[rightIndex], m_HeapNodes[leftIndex]);
        (m_HeapPriorities[leftIndex], m_HeapPriorities[rightIndex]) = (m_HeapPriorities[rightIndex], m_HeapPriorities[leftIndex]);
        m_HeapPositions[m_HeapNodes[leftIndex]] = leftIndex;
        m_HeapPositions[m_HeapNodes[rightIndex]] = rightIndex;
    }

    private readonly record struct LineOfSightCacheKey(HexagonalCubePoint Start, HexagonalCubePoint End);

    private readonly record struct LineOfSightCacheEntry(bool IsTraversable, double Cost);
}
