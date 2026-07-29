using System.Collections;
using System.Diagnostics;

namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示可重复元素的跳表.
/// 跳表按比较器维持元素有序, 并通过跨度信息支持按索引访问.
/// 插入, 删除, 查找和索引访问的平均时间复杂度均为 O(log N). 此类型不保证线程安全.
/// </summary>
public class SkipList<T> : IList<T>, ICollection where T : IComparable<T>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Random m_Random = new Random();
    private static readonly SkipListNode[] s_UpdateArray = new SkipListNode[MaxLevel];

    /// <summary>
    /// 获取用于排序和比较元素的比较器.
    /// </summary>
    internal Comparer<T> LocalComparer { get; } = Comparer<T>.Default;

    #region IEnumerable 成员

    /// <summary>
    /// 返回循环访问跳表的枚举器.
    /// </summary>
    /// <returns>跳表的枚举器.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        SkipListNode? current = Head.Forward[0].Next;
        while (current != null)
        {
            yield return current.Value;
            current = current.Forward[0].Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region ICollection 成员

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

    #endregion

    #region IList 成员

    /// <summary>
    /// 获取指定索引处的元素. 不支持设置元素.
    /// </summary>
    /// <param name="index">要获取元素的从零开始索引.</param>
    /// <returns>指定索引处的元素.</returns>
    public T this[int index]
    {
        set => throw new NotSupportedException();
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count, nameof(index));
            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));

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

    #endregion

    #region 内部查找

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

    #endregion

    #region SkipList 成员

    /// <summary>
    /// 获取第一个与指定元素比较相等的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? FirstEqual(T item)
    {
        SkipListNode? node = FirstEqual_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取最后一个与指定元素比较相等的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? LastEqual(T item)
    {
        SkipListNode? node = LastEqual_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取第一个大于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? FirstGreaterThan(T item)
    {
        SkipListNode? node = FirstGreaterThan_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取第一个大于或等于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? FirstGreaterThanOrEqual(T item)
    {
        SkipListNode? node = FirstGreaterThanOrEqual_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取最后一个小于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T LastLessThan(T item) => LastLessThan_Internal(item).Value;

    /// <summary>
    /// 获取最后一个小于或等于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T LastLessThanOrEqual(T item) => LastLessThanOrEqual_Internal(item).Value;

    /// <summary>
    /// 返回范围内的所有元素.
    /// 参数 lower 与参数 upper 都是闭区间.
    /// </summary>
    /// <param name="lower">范围下限.</param>
    /// <param name="upper">范围上限.</param>
    /// <returns>位于指定闭区间内的元素序列.</returns>
    public IEnumerable<T> Range(T lower, T upper)
    {
        SkipListNode? current = FirstGreaterThanOrEqual_Internal(lower);
        if (current == null) yield break;
        while (LocalComparer.Compare(current.Value, upper) <= 0)
        {
            yield return current.Value;
            current = current.Forward[0].Next;
            if (current == null) yield break;
        }
    }

    #endregion

    #region 帮助成员

    private static int GetRandomLevel()
    {
        int level = 1;
        while (level < MaxLevel &&
               (m_Random.Next(0, short.MaxValue) & ushort.MaxValue) < Probability * ushort.MaxValue
              ) ++level;
        return level;
    }

    /// <summary>
    /// 表示跳表中的节点, 其中包含各层的后继链接和跨度信息.
    /// </summary>
    [DebuggerDisplay("Node [{Value}]")]
    public class SkipListNode
    {
        /// <summary>
        /// 初始化 <see cref="SkipListNode"/> 的新实例.
        /// </summary>
        /// <param name="level">节点的层数.</param>
        /// <param name="value">节点存储的元素.</param>
        public SkipListNode(int level, T value)
        {
            Forward = new (SkipListNode? Next, int Span)[level];
            Value = value;
        }

        /// <summary>
        /// 获取各层的后继节点及跨度信息.
        /// </summary>
        public readonly (SkipListNode? Next, int Span)[] Forward;

        /// <summary>
        /// 获取或设置节点存储的元素.
        /// </summary>
        public T Value { get; internal set; }
    }

    #endregion

    #region 字段

    private const int MaxLevel = 32;
    private const float Probability = 0.25f;
    private int m_CurrLevel;
    /// <summary>
    /// 获取跳表的头节点.
    /// </summary>
    internal SkipListNode Head { get; } = new SkipListNode(MaxLevel, default!);

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用默认比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    public SkipList()
    {
    }

    /// <summary>
    /// 使用指定比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    /// <param name="comparer">用于排序和比较元素的比较器.</param>
    public SkipList(Comparer<T> comparer) => LocalComparer = comparer;

    /// <summary>
    /// 使用指定集合中的元素和默认比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    /// <param name="collection">要复制到跳表中的元素.</param>
    public SkipList(IEnumerable<T> collection)
    {
        T[] array = collection.ToArray();
        Array.Sort(array, LocalComparer);
        foreach (T item in array) Add(item);
    }

    /// <summary>
    /// 使用指定集合中的元素和比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    /// <param name="collection">要复制到跳表中的元素.</param>
    /// <param name="comparer">用于排序和比较元素的比较器.</param>
    public SkipList(IEnumerable<T> collection, Comparer<T> comparer)
    {
        T[] array = collection.ToArray();
        Array.Sort(array, LocalComparer = comparer);
        foreach (T item in array) Add(item);
    }

    #endregion

}
