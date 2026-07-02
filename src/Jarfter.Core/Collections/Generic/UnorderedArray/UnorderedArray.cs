namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 提供无序数组的非泛型工厂入口.
/// </summary>
public static class UnorderedArray
{
    /// <summary>
    /// 创建一个无序数组实例.
    /// </summary>
    /// <typeparam name="T">元素类型.</typeparam>
    /// <param name="capacity">初始容量.</param>
    /// <param name="comparer">元素比较器, 用于线性比较.</param>
    /// <returns>无序数组实例.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于 0 时抛出.</exception>
    public static UnorderedArray<T> Create<T>(int capacity = 0, IEqualityComparer<T>? comparer = null)
    {
        return new ArrayBackedUnorderedArray<T>(capacity, comparer);
    }

    /// <summary>
    /// 使用指定集合创建一个无序数组实例.
    /// </summary>
    /// <typeparam name="T">元素类型.</typeparam>
    /// <param name="collection">用于初始化的集合.</param>
    /// <param name="comparer">元素比较器, 用于线性比较.</param>
    /// <returns>无序数组实例.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 <see langword="null"/> 时抛出.</exception>
    public static UnorderedArray<T> Create<T>(IEnumerable<T> collection, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(collection);
        int capacity = collection is ICollection<T> source ? source.Count : 0;
        UnorderedArray<T> array = Create(capacity, comparer);
        array.AddRange(collection);
        return array;
    }

    /// <summary>
    /// 创建一个不允许重复元素的字典索引无序数组实例.
    /// </summary>
    /// <typeparam name="T">非空元素类型.</typeparam>
    /// <param name="capacity">初始容量.</param>
    /// <param name="comparer">元素比较器, 用于字典键比较.</param>
    /// <returns>不允许重复元素的无序数组实例.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于 0 时抛出.</exception>
    public static UnorderedArray<T> CreateUnique<T>(int capacity = 0, IEqualityComparer<T>? comparer = null) where T : notnull
    {
        return new UniqueIndexedUnorderedArray<T>(capacity, comparer);
    }

    /// <summary>
    /// 使用指定集合创建一个不允许重复元素的字典索引无序数组实例.
    /// </summary>
    /// <typeparam name="T">非空元素类型.</typeparam>
    /// <param name="collection">用于初始化的集合.</param>
    /// <param name="comparer">元素比较器, 用于字典键比较.</param>
    /// <returns>不允许重复元素的无序数组实例.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentException"><paramref name="collection"/> 包含重复元素时抛出.</exception>
    public static UnorderedArray<T> CreateUnique<T>(IEnumerable<T> collection, IEqualityComparer<T>? comparer = null) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(collection);
        int capacity = collection is ICollection<T> source ? source.Count : 0;
        UnorderedArray<T> array = CreateUnique(capacity, comparer);
        array.AddRange(collection);
        return array;
    }
}
