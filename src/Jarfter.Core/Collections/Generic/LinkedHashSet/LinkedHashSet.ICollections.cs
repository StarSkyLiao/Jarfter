namespace Jarfter.Core.Collections.Generic;

public partial class LinkedHashSet<T> : ICollection<T>
{
    /// <summary>
    /// 获取集合中的元素数量.
    /// </summary>
    public int Count => m_LinkedList.Count;

    /// <summary>
    /// 获取集合的结构版本号.
    /// 每次调用 <see cref="Add"/> 或 <see cref="Clear"/>, 以及每次成功移除元素后递增.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// 获取一个值, 指示集合是否为只读.
    /// </summary>
    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// 添加元素, 并将该元素移动到集合首部.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    public void Add(T item) => _ = AddAndGetEvicted(item);

    /// <summary>
    /// 尝试添加元素, 并将该元素移动到集合首部.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    /// <returns>元素此前不存在时返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryAdd(T item)
    {
        bool added = !m_Dictionary.ContainsKey(item);
        _ = AddAndGetEvicted(item);
        return added;
    }

    /// <summary>
    /// 添加元素, 并返回因超出容量而从尾部淘汰的元素.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    /// <returns>被淘汰的元素; 未发生淘汰时返回 <see langword="default"/>.</returns>
    public T AddAndGetEvicted(T item)
    {
        ++Version;
        if (m_Dictionary.TryGetValue(item, out LinkedListNode<T>? node))
        {
            // 重新链接已有节点, 使最近添加的元素保持在首部.
            m_LinkedList.Remove(node);
            m_LinkedList.AddFirst(node);
            return default!;
        }

        node = m_LinkedList.AddFirst(item);
        m_Dictionary.Add(item, node);

        // 新元素只会使集合超出一个容量单位, 因此最多淘汰一个尾部元素.
        if (m_LinkedList.Count <= m_Capacity) return default!;

        node = m_LinkedList.Last!;
        T evicted = node.Value;
        m_Dictionary.Remove(evicted);
        m_LinkedList.RemoveLast();
        return evicted;
    }

    /// <summary>
    /// 尝试查看集合首部的元素.
    /// </summary>
    /// <param name="item">输出的首部元素.</param>
    /// <returns>集合非空时返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryPeekFirst(out T item)
    {
        LinkedListNode<T>? node = m_LinkedList.First;
        if (node is null)
        {
            item = default!;
            return false;
        }

        item = node.Value;
        return true;
    }

    /// <summary>
    /// 尝试查看集合尾部的元素.
    /// </summary>
    /// <param name="item">输出的尾部元素.</param>
    /// <returns>集合非空时返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryPeekLast(out T item)
    {
        LinkedListNode<T>? node = m_LinkedList.Last;
        if (node is null)
        {
            item = default!;
            return false;
        }

        item = node.Value;
        return true;
    }

    /// <summary>
    /// 尝试移除并返回集合首部的元素.
    /// </summary>
    /// <param name="item">输出的首部元素.</param>
    /// <returns>集合非空并成功移除元素时返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryRemoveFirst(out T item)
    {
        LinkedListNode<T>? node = m_LinkedList.First;
        if (node is null)
        {
            item = default!;
            return false;
        }

        item = node.Value;
        m_LinkedList.RemoveFirst();
        m_Dictionary.Remove(item);
        ++Version;
        return true;
    }

    /// <summary>
    /// 尝试移除并返回集合尾部的元素.
    /// </summary>
    /// <param name="item">输出的尾部元素.</param>
    /// <returns>集合非空并成功移除元素时返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryRemoveLast(out T item)
    {
        LinkedListNode<T>? node = m_LinkedList.Last;
        if (node is null)
        {
            item = default!;
            return false;
        }

        item = node.Value;
        m_LinkedList.RemoveLast();
        m_Dictionary.Remove(item);
        ++Version;
        return true;
    }

    /// <summary>
    /// 移除集合中的所有元素.
    /// </summary>
    public void Clear()
    {
        ++Version;
        m_LinkedList.Clear();
        m_Dictionary.Clear();
    }

    /// <summary>
    /// 确定集合是否包含指定元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>包含元素时返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool Contains(T item) => m_Dictionary.ContainsKey(item);

    /// <summary>
    /// 将集合中的元素复制到目标数组.
    /// </summary>
    /// <param name="array">目标数组.</param>
    public void CopyTo(T[] array) => CopyTo(array, 0);

    /// <summary>
    /// 从目标数组的指定索引开始复制集合中的元素.
    /// </summary>
    /// <param name="array">目标数组.</param>
    /// <param name="arrayIndex">目标数组中的起始索引.</param>
    public void CopyTo(T[] array, int arrayIndex) => m_LinkedList.CopyTo(array, arrayIndex);

    /// <summary>
    /// 尝试移除指定元素.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>成功移除元素时返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool Remove(T item)
    {
        if (!m_Dictionary.TryGetValue(item, out LinkedListNode<T>? node)) return false;
        m_LinkedList.Remove(node);
        bool removed = m_Dictionary.Remove(item);
        if (removed) ++Version;
        return removed;
    }

}
