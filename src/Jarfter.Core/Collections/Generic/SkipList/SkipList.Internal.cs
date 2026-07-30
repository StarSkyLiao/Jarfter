namespace Jarfter.Core.Collections.Generic;

public partial class SkipList<T>
{
    /// <summary>
    /// 查找第一个与指定元素比较相等的节点.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的第一个节点; 未找到时为 <c>null</c>.</returns>
    internal SkipListNode? FirstEqual_Internal(T item)
    {
        SkipListNode curr = Head;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;
            while (next != null && LocalComparer.Compare(next.Value, item) < 0)
            {
                curr = next;
                next = curr.Forward[i].Next;
            }
        }

        SkipListNode? node = curr.Forward[0].Next;
        if (node == null) return null;
        return LocalComparer.Compare(node.Value, item) == 0 ? node : null;
    }

    /// <summary>
    /// 查找最后一个与指定元素比较相等的节点.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的最后一个节点; 未找到时为 <c>null</c>.</returns>
    internal SkipListNode? LastEqual_Internal(T item)
    {
        SkipListNode curr = Head;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;
            while (next != null && LocalComparer.Compare(next.Value, item) <= 0)
            {
                curr = next;
                next = curr.Forward[i].Next;
            }
        }

        return LocalComparer.Compare(curr.Value, item) == 0 ? curr : null;
    }

    /// <summary>
    /// 查找第一个大于指定元素的节点.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的节点; 未找到时为 <c>null</c>.</returns>
    internal SkipListNode? FirstGreaterThan_Internal(T item)
    {
        SkipListNode curr = Head;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;
            while (next != null && LocalComparer.Compare(next.Value, item) <= 0)
            {
                curr = next;
                next = curr.Forward[i].Next;
            }
        }

        return curr.Forward[0].Next;
    }

    /// <summary>
    /// 查找第一个大于或等于指定元素的节点.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的节点; 未找到时为 <c>null</c>.</returns>
    internal SkipListNode? FirstGreaterThanOrEqual_Internal(T item)
    {
        SkipListNode curr = Head;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;
            while (next != null && LocalComparer.Compare(next.Value, item) < 0)
            {
                curr = next;
                next = curr.Forward[i].Next;
            }
        }

        return curr.Forward[0].Next;
    }

    /// <summary>
    /// 查找最后一个小于指定元素的节点.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的节点; 未找到时返回头节点.</returns>
    internal SkipListNode LastLessThan_Internal(T item)
    {
        SkipListNode curr = Head;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;
            while (next != null && LocalComparer.Compare(next.Value, item) <= 0)
            {
                curr = next;
                next = curr.Forward[i].Next;
            }
        }

        return curr;
    }

    /// <summary>
    /// 查找最后一个小于或等于指定元素的节点.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的节点; 未找到时返回头节点.</returns>
    internal SkipListNode LastLessThanOrEqual_Internal(T item)
    {
        SkipListNode curr = Head;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = curr.Forward[i].Next;
            while (next != null && LocalComparer.Compare(next.Value, item) < 0)
            {
                curr = next;
                next = curr.Forward[i].Next;
            }
        }

        return curr;
    }

    private int GetBoundIndex(T item, bool skipEqualValues)
    {
        SkipListNode current = Head;
        int index = 0;

        for (int i = m_CurrLevel - 1; i >= 0; i--)
        {
            SkipListNode? next = current.Forward[i].Next;
            while (next != null)
            {
                int comparison = LocalComparer.Compare(next.Value, item);
                if (comparison > 0 || (comparison == 0 && !skipEqualValues)) break;

                index += current.Forward[i].Span;
                current = next;
                next = current.Forward[i].Next;
            }
        }

        return index;
    }
}
