using System.Collections;

namespace Jarfter.Core.Collections.Generic;

public partial class SkipList<T> : ICollection
{
    /// <summary>
    /// 获取跳表中的元素数.
    /// </summary>
    public int Count { get; private set; }

    bool ICollection<T>.IsReadOnly => false;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;

    /// <summary>
    /// 从指定索引开始, 将所有元素复制到指定数组.
    /// </summary>
    /// <param name="array">元素的目标数组.</param>
    /// <param name="index">数组中复制起始位置的从零开始索引.</param>
    public void CopyTo(Array array, int index)
    {
        if (array is T[] tArray) CopyTo(tArray, index);
        else throw new ArgumentException("Generic type do not match!", nameof(array));
    }

    /// <summary>
    /// 将元素添加到跳表.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    public void Add(T item)
    {
        unsafe
        {
            // rank 存储每层中插入位置前驱节点的排名.
            Span<int> rank = stackalloc int[MaxLevel];
            // 在各层查找插入位置.
            SkipListNode curr = Head;
            // 从高层到低层寻找新节点的前驱节点. 插入后, 前驱节点的后继即为新节点.
            for (int i = m_CurrLevel - 1; i >= 0; i--)
            {
                // 顶层尚未跨过节点, 排名为 0. 其他层沿用上层找到的同一前驱节点排名.
                rank[i] = i == m_CurrLevel - 1 ? 0 : rank[i + 1];
                // 沿前进指针遍历跳表, 相等元素排在已有元素之后.
                SkipListNode? next = curr.Forward[i].Next;
                while (next != null && LocalComparer.Compare(next.Value, item) <= 0)
                {
                    // 记录沿途跨越的节点数.
                    rank[i] += curr.Forward[i].Span;
                    // 移动到下一个节点.
                    curr = next;
                    next = curr.Forward[i].Next;
                }

                // 记录当前层的前驱节点, 供后续插入使用.
                s_UpdateArray[i] = curr;
                // 当前层查找结束后, 从 curr 的下一层继续查找.
            }

            // 随机确定新节点的层数.
            int level = GetRandomLevel();
            // 新节点层数超过当前最高层时, 头节点是新增层的前驱节点.
            if (level > m_CurrLevel)
            {
                for (int i = m_CurrLevel; i < level; i++)
                {
                    rank[i] = 0;
                    s_UpdateArray[i] = Head;
                    s_UpdateArray[i].Forward[i].Span = Count;
                }

                // 更新跳表当前最高层数.
                m_CurrLevel = level;
            }

            // 创建新节点.
            curr = new SkipListNode(level, item);
            // 根据已记录的前驱节点接入新节点, 同时维护跨度.
            for (int i = 0; i < level; i++)
            {
                // 设置新节点的前进指针.
                curr.Forward[i].Next = s_UpdateArray[i].Forward[i].Next;
                // 将各层前驱节点的前进指针指向新节点.
                s_UpdateArray[i].Forward[i].Next = curr;

                // 计算新节点跨越的节点数.
                curr.Forward[i].Span = s_UpdateArray[i].Forward[i].Span - (rank[0] - rank[i]);
                // 新节点插入后, 前驱节点的跨度需加 1.
                s_UpdateArray[i].Forward[i].Span = rank[0] - rank[i] + 1;
            }

            // 高层节点跨过新节点, 跨度加 1.
            for (int i = level; i < m_CurrLevel; i++) s_UpdateArray[i].Forward[i].Span++;
            // 跳表节点计数加 1.
            Count++;
        }
    }

    /// <summary>
    /// 确定跳表是否包含与指定元素比较相等的元素.
    /// </summary>
    /// <param name="item">要定位的元素.</param>
    /// <returns>跳表包含元素时为 <c>true</c>; 否则为 <c>false</c>.</returns>
    public bool Contains(T item) => FirstEqual_Internal(item) != null;

    /// <summary>
    /// 移除首个与 item 比较值为 0 的元素.
    /// 关于移除完全等同元素的方法, 另请参阅: <see cref="RemoveItem"/>.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>成功移除元素时为 <c>true</c>; 否则为 <c>false</c>.</returns>
    public bool Remove(T item)
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

            s_UpdateArray[i] = curr;
        }

        SkipListNode? node = curr.Forward[0].Next;
        if (node == null || LocalComparer.Compare(node.Value, item) != 0) return false;
        RemoveNode_Internal(node);
        return true;
    }

    /// <summary>
    /// 移除首个完全等同于 item 的元素.
    /// 关于移除比较值为 0 元素的方法, 另请参阅: <see cref="Remove"/>.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>成功移除元素时为 <c>true</c>; 否则为 <c>false</c>.</returns>
    public bool RemoveItem(T item)
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

            s_UpdateArray[i] = curr;
        }

        SkipListNode? node = curr.Forward[0].Next;
        while (node != null)
        {
            if (!Equals(node.Value, item))
            {
                node = node.Forward[0].Next;
            }
            else
            {
                RemoveNode_Internal(node);
                return true;
            }
        }

        return false;
    }

    private void RemoveNode_Internal(SkipListNode temp)
    {
        // 将目标节点从各层前驱节点的链接中移除, 并维护跨度.
        for (int i = 0; i < m_CurrLevel; i++)
            if (s_UpdateArray[i].Forward[i].Next == temp)
            {
                s_UpdateArray[i].Forward[i].Span += temp.Forward[i].Span - 1;
                s_UpdateArray[i].Forward[i].Next = temp.Forward[i].Next;
            }
            else
            {
                s_UpdateArray[i].Forward[i].Span -= 1;
            }

        while (m_CurrLevel > 1 && Head.Forward[m_CurrLevel - 1].Next == null) m_CurrLevel--;
        Count--;
    }

    /// <summary>
    /// 移除跳表中的所有元素.
    /// </summary>
    public void Clear()
    {
        Array.Clear(Head.Forward, 0, Head.Forward.Length);
        Count = m_CurrLevel = 0;
    }

    /// <summary>
    /// 从指定索引开始, 将所有元素复制到指定数组.
    /// </summary>
    /// <param name="array">元素的目标数组.</param>
    /// <param name="arrayIndex">数组中复制起始位置的从零开始索引.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        SkipListNode? current = Head.Forward[0].Item1;
        while (current != null)
        {
            array[arrayIndex++] = current.Value;
            current = current.Forward[0].Item1;
        }
    }

}
