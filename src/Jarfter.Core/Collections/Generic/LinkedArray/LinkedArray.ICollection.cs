using System.Collections;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.Generic;

public sealed partial class LinkedArray<T>
{
    /// <summary>
    /// 获取一个值, 指示集合是否为只读.
    /// </summary>
    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// 获取一个值, 指示访问集合是否为线程安全.
    /// </summary>
    bool ICollection.IsSynchronized => false;

    /// <summary>
    /// 获取用于同步访问集合的对象.
    /// </summary>
    object ICollection.SyncRoot => this;

    /// <summary>
    /// 向尾部添加元素.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    public void Add(T item)
    {
        int tail = Items[0].Front;
        InsertBetween(tail, 0, item);
    }

    /// <summary>
    /// 清空容器.
    /// </summary>
    public void Clear()
    {
        if (Count > 0 && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(Items, 1, Count);
        }

        Items[0].Front = 0;
        Items[0].Next = 0;
        Count = 0;
        Version++;
    }

    /// <summary>
    /// 判断是否包含指定值.
    /// </summary>
    /// <param name="item">待查找元素.</param>
    /// <returns>存在返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool Contains(T item) => FindNodeIndex(item) >= 0;

    /// <summary>
    /// 将元素复制到目标数组.
    /// </summary>
    /// <param name="array">目标数组.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> 为 <see langword="null"/> 时抛出.</exception>
    public void CopyTo(T[] array) => CopyTo(array, 0);

    /// <summary>
    /// 从指定索引开始将元素复制到目标数组.
    /// </summary>
    /// <param name="array">目标数组.</param>
    /// <param name="arrayIndex">目标起始索引.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> 小于 0 时抛出.</exception>
    /// <exception cref="ArgumentException">目标空间不足时抛出.</exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Destination array is not long enough.", nameof(array));
        }

        int node = Items[0].Next;
        while (node != 0)
        {
            array[arrayIndex++] = Items[node].Value;
            node = Items[node].Next;
        }
    }

    /// <summary>
    /// 尝试移除首个匹配元素.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool Remove(T item)
    {
        int node = FindNodeIndex(item);
        if (node < 0) return false;
        RemoveNodeIndex(node);
        return true;
    }

    /// <summary>
    /// 将元素复制到非泛型数组.
    /// </summary>
    /// <param name="array">目标数组.</param>
    /// <param name="index">目标起始索引.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> 小于 0 时抛出.</exception>
    /// <exception cref="ArgumentException">数组类型不兼容、维度不合法或空间不足时抛出.</exception>
    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (array.Rank != 1)
        {
            throw new ArgumentException("Only single dimension arrays are supported.", nameof(array));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (array.Length - index < Count)
        {
            throw new ArgumentException("Destination array is not long enough.", nameof(array));
        }

        if (array is T[] values)
        {
            CopyTo(values, index);
            return;
        }

        if (array is object?[] objects)
        {
            int node = Items[0].Next;
            while (node != 0)
            {
                objects[index++] = Items[node].Value;
                node = Items[node].Next;
            }

            return;
        }

        throw new ArgumentException("Array type is not compatible with this collection.", nameof(array));
    }
}
