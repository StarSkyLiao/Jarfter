namespace Jarfter.Core.Collections.Generic;

public sealed partial class LinkedArray<T>
{
    /// <summary>
    /// 获取或设置指定逻辑索引处的元素.
    /// </summary>
    /// <param name="index">逻辑索引.</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出范围时抛出.</exception>
    public T this[int index]
    {
        get
        {
            int node = GetNodeIndexByListIndex(index);
            return Items[node].Value;
        }
        set
        {
            int node = GetNodeIndexByListIndex(index);
            Items[node].Value = value;
            Version++;
        }
    }

    /// <summary>
    /// 查找指定元素的逻辑索引.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到返回索引, 否则返回 -1.</returns>
    public int IndexOf(T item)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int index = 0;
        int node = Items[0].Next;

        while (node != 0)
        {
            if (comparer.Equals(Items[node].Value, item)) return index;
            index++;
            node = Items[node].Next;
        }

        return -1;
    }

    /// <summary>
    /// 在指定逻辑索引处插入元素.
    /// </summary>
    /// <param name="index">插入位置.</param>
    /// <param name="item">要插入的元素.</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出范围时抛出.</exception>
    public void Insert(int index, T item)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

        if (index == Count)
        {
            Add(item);
            return;
        }

        int next = GetNodeIndexByListIndex(index);
        InsertBetween(Items[next].Front, next, item);
    }

    /// <summary>
    /// 移除指定逻辑索引处的元素.
    /// </summary>
    /// <param name="index">逻辑索引.</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出范围时抛出.</exception>
    public void RemoveAt(int index)
    {
        int node = GetNodeIndexByListIndex(index);
        RemoveNodeIndex(node);
    }
}
