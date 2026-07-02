using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 无序数组的紧凑实现.
/// 使用连续数组存储, 按值删除需要线性查找.
/// </summary>
/// <typeparam name="T">元素类型.</typeparam>
internal sealed class ArrayBackedUnorderedArray<T> : UnorderedArray<T>
{
    private readonly IEqualityComparer<T> m_Comparer;
    private readonly bool m_UseArrayIndexOf;

    private T[] m_Items;
    private int m_Count;
    private int m_Version;

    internal ArrayBackedUnorderedArray(int capacity, IEqualityComparer<T>? comparer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        m_Comparer = comparer ?? EqualityComparer<T>.Default;
        m_UseArrayIndexOf = comparer is null || ReferenceEquals(m_Comparer, EqualityComparer<T>.Default);
        m_Items = capacity == 0 ? [] : new T[capacity];
    }

    public override int Count => m_Count;

    public override int Capacity
    {
        get => m_Items.Length;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, m_Count);
            if (value == m_Items.Length) return;
            if (value == 0)
            {
                m_Items = [];
                return;
            }

            T[] newItems = new T[value];
            if (m_Count > 0) Array.Copy(m_Items, 0, newItems, 0, m_Count);
            m_Items = newItems;
        }
    }

    public override T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]get
        {
            ValidateIndex(index);
            return m_Items[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]set
        {
            ValidateIndex(index);
            m_Items[index] = value;
            m_Version++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override void Add(T item)
    {
        int index = m_Count;
        if ((uint)index >= (uint)m_Items.Length) Grow(index + 1);
        m_Items[index] = item;
        m_Count = index + 1;
        m_Version++;
    }

    public override void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (ReferenceEquals(collection, this)) collection = collection.ToArray();

        if (collection is ICollection<T> source)
        {
            int addCount = source.Count;
            if (addCount <= 0) return;
            EnsureCapacity(checked(m_Count + addCount));
            source.CopyTo(m_Items, m_Count);
            m_Count += addCount;
            m_Version++;
            return;
        }

        foreach (T item in collection) Add(item);
    }

    public override bool Remove(T item)
    {
        int index = IndexOf(item);
        if (index < 0) return false;
        RemoveAtCore(index);
        return true;
    }

    public override void RemoveAt(int index)
    {
        ValidateIndex(index);
        RemoveAtCore(index);
    }

    public override bool TryRemoveAt(int index)
    {
        if ((uint)index >= (uint)m_Count) return false;
        RemoveAtCore(index);
        return true;
    }

    public override void Clear()
    {
        if (m_Count > 0 && RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(m_Items, 0, m_Count);
        }

        m_Count = 0;
        m_Version++;
    }

    public override bool Contains(T item) => IndexOf(item) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public override int IndexOf(T item)
    {
        if (m_Count == 0) return -1;

        if (m_UseArrayIndexOf)
        {
            return Array.IndexOf(m_Items, item, 0, m_Count);
        }

        for (int i = 0; i < m_Count; i++)
        {
            if (m_Comparer.Equals(m_Items[i], item)) return i;
        }

        return -1;
    }

    public override void CopyTo(T[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (array.Length - arrayIndex < m_Count)
        {
            throw new ArgumentException("Destination array is not long enough.", nameof(array));
        }

        if (m_Count > 0) Array.Copy(m_Items, 0, array, arrayIndex, m_Count);
    }

    public override int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        if (m_Items.Length < capacity) Grow(capacity);
        return m_Items.Length;
    }

    public override void TrimExcess()
    {
        if (m_Count >= (int)(m_Items.Length * 0.9)) return;
        Capacity = m_Count;
    }

    public override ReadOnlySpan<T> AsSpan() => m_Items.AsSpan(0, m_Count);

    public override T[] ToArray()
    {
        if (m_Count == 0) return [];
        T[] output = new T[m_Count];
        Array.Copy(m_Items, 0, output, 0, m_Count);
        return output;
    }

    public override IEnumerator<T> GetEnumerator()
    {
        int version = m_Version;
        int count = m_Count;

        for (int i = 0; i < count; i++)
        {
            if (version != m_Version) ThrowCollectionModified();
            yield return m_Items[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void RemoveAtCore(int index)
    {
        int lastIndex = m_Count - 1;
        if (index < lastIndex)
        {
            m_Items[index] = m_Items[lastIndex];
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            m_Items[lastIndex] = default!;
        }

        m_Count = lastIndex;
        m_Version++;
    }

    private void Grow(int requiredCapacity)
    {
        Capacity = GetExpandedCapacity(m_Items.Length, requiredCapacity);
    }
    
    private void ValidateIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)m_Count);
    }

    [DoesNotReturn]
    private static void ThrowCollectionModified()
    {
        throw new InvalidOperationException("The collection was modified during enumeration.");
    }
}
