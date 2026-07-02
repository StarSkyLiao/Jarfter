using System.Collections;

namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示支持 O(1) 尾覆盖删除语义的无序动态数组抽象基类.
/// </summary>
/// <typeparam name="T">元素类型.</typeparam>
public abstract class UnorderedArray<T> : IReadOnlyList<T>
{
    /// <summary>
    /// 获取当前元素数量.
    /// </summary>
    public abstract int Count { get; }

    /// <summary>
    /// 获取或设置底层容量.
    /// 设置时不能小于 <see cref="Count"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">设置值小于 <see cref="Count"/> 时抛出.</exception>
    public abstract int Capacity { get; set; }

    /// <summary>
    /// 获取当前实例是否为空.
    /// </summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// 获取或设置指定下标处的元素.
    /// </summary>
    /// <param name="index">元素下标.</param>
    /// <exception cref="ArgumentOutOfRangeException">下标越界时抛出.</exception>
    public abstract T this[int index] { get; set; }

    /// <summary>
    /// 追加一个元素到末尾.
    /// </summary>
    /// <param name="item">要追加的元素.</param>
    public abstract void Add(T item);

    /// <summary>
    /// 批量追加元素.
    /// </summary>
    /// <param name="collection">要追加的集合.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 <see langword="null"/> 时抛出.</exception>
    public abstract void AddRange(IEnumerable<T> collection);

    /// <summary>
    /// 查找并删除一个匹配元素.
    /// 删除时会用尾元素覆盖被删位置, 不保持顺序.
    /// </summary>
    /// <param name="item">要删除的元素.</param>
    /// <returns>删除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public abstract bool Remove(T item);

    /// <summary>
    /// 删除指定下标的元素.
    /// 删除时会用尾元素覆盖被删位置, 不保持顺序.
    /// </summary>
    /// <param name="index">要删除的元素下标.</param>
    /// <exception cref="ArgumentOutOfRangeException">下标越界时抛出.</exception>
    public abstract void RemoveAt(int index);

    /// <summary>
    /// 尝试删除指定下标的元素.
    /// </summary>
    /// <param name="index">要删除的元素下标.</param>
    /// <returns>删除成功返回 <see langword="true"/>, 下标越界返回 <see langword="false"/>.</returns>
    public abstract bool TryRemoveAt(int index);

    /// <summary>
    /// 清空所有元素.
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// 判断是否包含指定元素.
    /// </summary>
    /// <param name="item">待查找元素.</param>
    /// <returns>包含返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public abstract bool Contains(T item);

    /// <summary>
    /// 查找一个匹配元素的下标.
    /// </summary>
    /// <param name="item">待查找元素.</param>
    /// <returns>找到返回下标, 未找到返回 -1.</returns>
    public abstract int IndexOf(T item);

    /// <summary>
    /// 将元素复制到目标数组.
    /// </summary>
    /// <param name="array">目标数组.</param>
    /// <param name="arrayIndex">目标起始下标.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> 小于 0 时抛出.</exception>
    /// <exception cref="ArgumentException">目标空间不足时抛出.</exception>
    public abstract void CopyTo(T[] array, int arrayIndex);

    /// <summary>
    /// 确保容量至少为指定值.
    /// </summary>
    /// <param name="capacity">目标最小容量.</param>
    /// <returns>确保后的容量.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于 0 时抛出.</exception>
    public abstract int EnsureCapacity(int capacity);

    /// <summary>
    /// 当使用率较低时收缩容量.
    /// </summary>
    public abstract void TrimExcess();

    /// <summary>
    /// 获取当前有效元素的只读跨度视图.
    /// </summary>
    /// <returns>指向当前有效元素区间的只读跨度.</returns>
    public abstract ReadOnlySpan<T> AsSpan();

    /// <summary>
    /// 将当前有效元素复制为新数组.
    /// </summary>
    /// <returns>包含当前元素的新数组.</returns>
    public abstract T[] ToArray();

    /// <summary>
    /// 获取泛型枚举器.
    /// </summary>
    /// <returns>当前元素枚举器.</returns>
    public abstract IEnumerator<T> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// 计算扩容后的容量值.
    /// </summary>
    /// <param name="currentCapacity">当前容量.</param>
    /// <param name="requiredCapacity">要求的最小容量.</param>
    /// <returns>扩容后的容量.</returns>
    protected static int GetExpandedCapacity(int currentCapacity, int requiredCapacity)
    {
        int newCapacity = currentCapacity == 0 ? 4 : checked(currentCapacity * 2);
        if (newCapacity < requiredCapacity) newCapacity = requiredCapacity;
        return newCapacity;
    }
}
