using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.Generic;

public ref partial struct SpanList<T>
{
    /// <summary>
    /// 尝试移除第一个匹配元素.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryRemove(T item)
    {
        int index = IndexOf(item);
        if (index < 0) return false;

        RemoveAtCore(index);
        return true;
    }

    /// <summary>
    /// 移除第一个匹配元素.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool Remove(T item) => TryRemove(item);

    /// <summary>
    /// 移除指定索引处的元素.
    /// </summary>
    /// <param name="index">要移除的索引.</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出有效范围时抛出.</exception>
    public void RemoveAt(int index)
    {
        ValidateIndex(index);
        RemoveAtCore(index);
    }

    /// <summary>
    /// 尝试移除指定索引处的元素.
    /// </summary>
    /// <param name="index">要移除的索引.</param>
    /// <returns>移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryRemoveAt(int index)
    {
        if ((uint)index >= (uint)Count) return false;
        RemoveAtCore(index);
        return true;
    }

    /// <summary>
    /// 移除指定区间的元素.
    /// </summary>
    /// <param name="index">起始索引.</param>
    /// <param name="count">移除数量.</param>
    /// <exception cref="ArgumentOutOfRangeException">参数非法时抛出.</exception>
    public void RemoveRange(int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count - count);
        if (count == 0) return;

        int newCount = Count - count;

        // 通过左移尾部区间覆盖被移除段, 保证有效区间连续.
        if (index < newCount) m_Buffer[(index + count)..Count].CopyTo(m_Buffer[index..]);

        Count = newCount;
        ClearFreedSlots(newCount, count);
    }

    /// <summary>
    /// 清空当前容器.
    /// </summary>
    public void Clear()
    {
        if (Count == 0) return;
        ClearFreedSlots(0, Count);
        Count = 0;
    }

    private void RemoveAtCore(int index)
    {
        int newCount = Count - 1;
        if (index < newCount) m_Buffer[(index + 1)..Count].CopyTo(m_Buffer[index..]);

        Count = newCount;
        ClearFreedSlots(newCount, 1);
    }

    private void ClearFreedSlots(int start, int length)
    {
        // 仅在包含引用时清理尾槽, 避免无意义的值类型清零开销.
        if (length <= 0 || !RuntimeHelpers.IsReferenceOrContainsReferences<T>()) return;
        m_Buffer.Slice(start, length).Clear();
    }
}
