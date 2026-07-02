using System.Buffers;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.Generic;

public ref partial struct SpanList<T>
{
    /// <summary>
    /// 向末尾添加一个元素.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    /// <exception cref="InvalidOperationException">容量不足时抛出.</exception>
    public void Add(T item)
    {
        EnsureCapacityFor(checked(Count + 1));
        m_Buffer[Count++] = item;
    }

    /// <summary>
    /// 尝试向末尾添加一个元素.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    /// <returns>添加成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryAdd(T item)
    {
        if (Count >= m_Buffer.Length) return false;
        m_Buffer[Count++] = item;
        return true;
    }

    /// <summary>
    /// 向末尾批量添加元素.
    /// </summary>
    /// <param name="items">要添加的元素切片.</param>
    /// <exception cref="InvalidOperationException">容量不足时抛出.</exception>
    public void AddRange(scoped ReadOnlySpan<T> items)
    {
        if (items.IsEmpty) return;

        int newCount = checked(Count + items.Length);
        EnsureCapacityFor(newCount);
        items.CopyTo(m_Buffer[Count..]);
        Count = newCount;
    }

    /// <summary>
    /// 尝试向末尾批量添加元素.
    /// </summary>
    /// <param name="items">要添加的元素切片.</param>
    /// <returns>添加成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryAddRange(scoped ReadOnlySpan<T> items)
    {
        if (items.IsEmpty) return true;

        int newCount = checked(Count + items.Length);
        if (newCount > m_Buffer.Length) return false;

        items.CopyTo(m_Buffer[Count..]);
        Count = newCount;
        return true;
    }

    /// <summary>
    /// 在指定索引处插入一个元素.
    /// </summary>
    /// <param name="index">插入位置.</param>
    /// <param name="item">要插入的元素.</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出有效范围时抛出.</exception>
    /// <exception cref="InvalidOperationException">容量不足时抛出.</exception>
    public void Insert(int index, T item)
    {
        ValidateInsertIndex(index);
        EnsureCapacityFor(checked(Count + 1));

        // 先右移尾部区间, 再写入目标位置, 避免覆盖原数据.
        m_Buffer[index..Count].CopyTo(m_Buffer[(index + 1)..]);
        m_Buffer[index] = item;
        Count++;
    }

    /// <summary>
    /// 尝试在指定索引处插入一个元素.
    /// </summary>
    /// <param name="index">插入位置.</param>
    /// <param name="item">要插入的元素.</param>
    /// <returns>插入成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">索引超出有效范围时抛出.</exception>
    public bool TryInsert(int index, T item)
    {
        ValidateInsertIndex(index);
        if (Count >= m_Buffer.Length) return false;

        // 先右移尾部区间, 再写入目标位置, 避免覆盖原数据.
        m_Buffer[index..Count].CopyTo(m_Buffer[(index + 1)..]);
        m_Buffer[index] = item;
        Count++;
        return true;
    }

    /// <summary>
    /// 在指定索引处批量插入元素.
    /// </summary>
    /// <param name="index">插入位置.</param>
    /// <param name="items">要插入的元素切片.</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出有效范围时抛出.</exception>
    /// <exception cref="InvalidOperationException">容量不足时抛出.</exception>
    public void InsertRange(int index, scoped ReadOnlySpan<T> items)
    {
        ValidateInsertIndex(index);
        if (items.IsEmpty) return;

        int newCount = checked(Count + items.Length);
        EnsureCapacityFor(newCount);
        InsertRangeCore(index, items, newCount);
    }

    /// <summary>
    /// 尝试在指定索引处批量插入元素.
    /// </summary>
    /// <param name="index">插入位置.</param>
    /// <param name="items">要插入的元素切片.</param>
    /// <returns>插入成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">索引超出有效范围时抛出.</exception>
    public bool TryInsertRange(int index, scoped ReadOnlySpan<T> items)
    {
        ValidateInsertIndex(index);
        if (items.IsEmpty) return true;

        int newCount = checked(Count + items.Length);
        if (newCount > m_Buffer.Length) return false;
        InsertRangeCore(index, items, newCount);
        return true;
    }

    private void InsertRangeCore(int index, scoped ReadOnlySpan<T> items, int newCount)
    {
        if (!items.Overlaps(m_Buffer))
        {
            // 先整体右移尾部区间, 再写入待插入区间, 保证顺序正确.
            m_Buffer[index..Count].CopyTo(m_Buffer[(index + items.Length)..]);
            items.CopyTo(m_Buffer[index..]);
            Count = newCount;
            return;
        }

        T[] rented = ArrayPool<T>.Shared.Rent(items.Length);
        Span<T> snapshot = rented.AsSpan(0, items.Length);

        try
        {
            // 当插入源与内部缓冲区重叠时, 先拍快照再移动尾段, 避免源数据被覆盖污染.
            items.CopyTo(snapshot);
            m_Buffer[index..Count].CopyTo(m_Buffer[(index + items.Length)..]);
            snapshot.CopyTo(m_Buffer[index..]);
            Count = newCount;
        }
        finally
        {
            ArrayPool<T>.Shared.Return(rented, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }
}
