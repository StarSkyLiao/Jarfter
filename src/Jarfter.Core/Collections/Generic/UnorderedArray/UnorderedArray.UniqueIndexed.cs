using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 不允许重复元素的字典索引无序数组实现.
/// 通过值到下标的直接映射减少重复元素链表维护成本.
/// </summary>
/// <typeparam name="T">元素类型.</typeparam>
internal sealed class UniqueIndexedUnorderedArray<T> : UnorderedArray<T> where T : notnull
{
    private const int NoIndex = -1;

    private readonly IEqualityComparer<T> m_Comparer;
    private readonly Dictionary<T, int> m_IndexMap;

    private T[] m_Items;
    private int m_Count;
    private int m_Version;

    internal UniqueIndexedUnorderedArray(int capacity, IEqualityComparer<T>? comparer)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        m_Comparer = comparer ?? EqualityComparer<T>.Default;
        m_IndexMap = new Dictionary<T, int>(capacity, m_Comparer);
        m_Items = capacity == 0 ? [] : new T[capacity];
    }

    public override int Count => m_Count;

    public override int Capacity
    {
        get => m_Items.Length;
        set
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
        get
        {
            ValidateIndex(index);
            return m_Items[index];
        }
        set
        {
            ValidateIndex(index);
            T oldValue = m_Items[index];
            if (m_Comparer.Equals(oldValue, value))
            {
                m_Items[index] = value;
            }
            else
            {
                ref int mappedIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(m_IndexMap, value, out bool exists);
                if (exists) ThrowDuplicate();
                mappedIndex = index;
                m_Items[index] = value;
                m_IndexMap.Remove(oldValue);
            }

            m_Version++;
        }
    }

    public override void Add(T item)
    {
        int index = m_Count;
        if ((uint)index >= (uint)m_Items.Length) Grow(index + 1);

        ref int mappedIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(m_IndexMap, item, out bool exists);
        if (exists) ThrowDuplicate();
        mappedIndex = index;
        m_Items[index] = item;
        m_Count = index + 1;
        m_Version++;
    }

    public override void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (ReferenceEquals(collection, this))
        {
            collection = collection.ToArray();
        }

        if (collection is ICollection<T> source)
        {
            int addCount = source.Count;
            if (addCount <= 0) return;
            EnsureCapacity(checked(m_Count + addCount));
        }

        foreach (T item in collection) Add(item);
    }

    public override bool Remove(T item)
    {
        if (!m_IndexMap.Remove(item, out int index)) return false;
        RemoveAtCore(index, removeIndexMapEntry: false);
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

        m_IndexMap.Clear();
        m_Count = 0;
        m_Version++;
    }

    public override bool Contains(T item)
    {
        return m_IndexMap.ContainsKey(item);
    }

    public override int IndexOf(T item)
    {
        return m_IndexMap.GetValueOrDefault(item, NoIndex);
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

    private void RemoveAtCore(int index, bool removeIndexMapEntry = true)
    {
        int lastIndex = m_Count - 1;
        T removedItem = m_Items[index];
        if (removeIndexMapEntry) m_IndexMap.Remove(removedItem);

        if (index < lastIndex)
        {
            T movedItem = m_Items[lastIndex];
            m_Items[index] = movedItem;
            m_IndexMap[movedItem] = index;
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
    private static void ThrowDuplicate()
    {
        throw new ArgumentException("Duplicate items are not allowed.");
    }

    [DoesNotReturn]
    private static void ThrowCollectionModified()
    {
        throw new InvalidOperationException("The collection was modified during enumeration.");
    }

}
