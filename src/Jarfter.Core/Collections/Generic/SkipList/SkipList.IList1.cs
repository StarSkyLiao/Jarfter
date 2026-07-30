namespace Jarfter.Core.Collections.Generic;

public partial class SkipList<T> : IList<T>
{
    /// <summary>
    /// 获取指定索引处的元素. 不支持设置元素.
    /// </summary>
    /// <param name="index">要获取元素的从零开始索引.</param>
    /// <returns>指定索引处的元素.</returns>
    public T this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            SkipListNode curr = Head;
            int currentIndex = -1;

            for (int i = m_CurrLevel - 1; i >= 0; i--)
            {
                SkipListNode? next = curr.Forward[i].Next;
                while (next != null && currentIndex + curr.Forward[i].Span <= index)
                {
                    currentIndex += curr.Forward[i].Span;
                    curr = next;
                    next = curr.Forward[i].Next;
                }

                if (currentIndex == index) return curr.Value;
            }

            return default!;
        }
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// 查找与指定元素比较相等的第一个元素的索引.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素的从零开始索引; 未找到时为 -1.</returns>
    public int IndexOf(T item)
    {
        SkipListNode curr = Head;
        int currIndex = -1;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;

            while (next != null && LocalComparer.Compare(next.Value, item) < 0)
            {
                currIndex += curr.Forward[i].Span;
                curr = next;
                next = curr.Forward[i].Next;
            }

            if (next == null || LocalComparer.Compare(next.Value, item) != 0) continue;

            return currIndex + curr.Forward[i].Span;
        }

        return -1;
    }

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

    /// <summary>
    /// 移除指定索引处的元素.
    /// </summary>
    /// <param name="index">要移除元素的从零开始索引.</param>
    public void RemoveAt(int index)
    {
        // 索引无效时不执行删除.
        if (index < 0 || index >= Count) return;

        SkipListNode curr = Head;
        SkipListNode? nodeToRemove = null;
        int currIndex = -1;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;
            while (next != null && currIndex + curr.Forward[i].Span < index)
            {
                currIndex += curr.Forward[i].Span;
                curr = next;
                next = curr.Forward[i].Next;
            }

            s_UpdateArray[i] = curr;

            if (currIndex == index) continue;
            nodeToRemove = curr.Forward[0].Next;
        }

        if (nodeToRemove != null) RemoveNode_Internal(nodeToRemove);
    }

    /// <summary>
    /// 移除指定连续索引范围内的所有元素.
    /// </summary>
    /// <param name="index">范围起始位置的从零开始索引.</param>
    /// <param name="count">要移除的元素数量.</param>
    public void RemoveIndexRange(int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Count - index);
        if (count == 0) return;

        Span<int> startRanks = stackalloc int[MaxLevel];
        FindPredecessorsByIndex(index, startRanks);

        int endExclusive = index + count;
        int endRank = 0;
        SkipListNode endPredecessor = Head;

        // 两次索引搜索分别定位范围两端. 每层直接跨接两端节点, 避免逐个删除导致 O(count * log N).
        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = endPredecessor.Forward[i].Next;
            while (next != null && endRank + endPredecessor.Forward[i].Span <= endExclusive)
            {
                endRank += endPredecessor.Forward[i].Span;
                endPredecessor = next;
                next = endPredecessor.Forward[i].Next;
            }

            SkipListNode startPredecessor = s_UpdateArray[i];
            int successorRank = endRank + endPredecessor.Forward[i].Span;
            startPredecessor.Forward[i].Next = next;
            startPredecessor.Forward[i].Span = successorRank - startRanks[i] - count;
        }

        Count -= count;
        while (m_CurrLevel > 1 && Head.Forward[m_CurrLevel - 1].Next == null) m_CurrLevel--;
    }

    private void FindPredecessorsByIndex(int index, Span<int> ranks)
    {
        SkipListNode current = Head;
        int rank = 0;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = current.Forward[i].Next;
            while (next != null && rank + current.Forward[i].Span <= index)
            {
                rank += current.Forward[i].Span;
                current = next;
                next = current.Forward[i].Next;
            }

            s_UpdateArray[i] = current;
            ranks[i] = rank;
        }
    }
}
