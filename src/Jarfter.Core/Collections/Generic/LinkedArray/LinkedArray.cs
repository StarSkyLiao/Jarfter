using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示一个基于数组存储的双向链表.
/// 内部使用索引 0 作为哨兵节点, 统一维护头尾链接.
/// </summary>
/// <typeparam name="T">元素类型.</typeparam>
public sealed partial class LinkedArray<T> : IList<T>, System.Collections.IList, IReadOnlyList<T>
{
    /// <summary>
    /// 表示链表中的内部节点.
    /// </summary>
    /// <param name="Value">节点值.</param>
    /// <param name="Front">前驱节点索引.</param>
    /// <param name="Next">后继节点索引.</param>
    internal record struct Node(T Value, int Front, int Next);

    /// <summary>
    /// 存储节点的数组.
    /// 索引 0 固定为哨兵节点, 不用于承载业务值.
    /// </summary>
    internal Node[] Items;

    /// <summary>
    /// 结构版本号, 用于检测枚举期间的并发修改.
    /// </summary>
    internal int Version;

    private static readonly Node[] s_EmptyArray = [new Node(default!, 0, 0)];

    /// <summary>
    /// 初始化一个空的 <see cref="LinkedArray{T}"/> 实例.
    /// </summary>
    public LinkedArray() => Items = s_EmptyArray;

    /// <summary>
    /// 初始化一个具有指定容量的 <see cref="LinkedArray{T}"/> 实例.
    /// </summary>
    /// <param name="capacity">初始可容纳的元素数量.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于 0 时抛出.</exception>
    public LinkedArray(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        Items = capacity == 0 ? s_EmptyArray : new Node[capacity + 1];
    }

    /// <summary>
    /// 使用指定集合初始化 <see cref="LinkedArray{T}"/> 实例.
    /// </summary>
    /// <param name="collection">用于初始化的集合.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 <see langword="null"/> 时抛出.</exception>
    public LinkedArray(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        Items = collection is ICollection<T> { Count: > 0 } source ? new Node[source.Count + 1] : s_EmptyArray;
        AddRange(collection);
    }

    /// <summary>
    /// 获取当前元素个数.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// 获取或设置可容纳的元素数量.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">设置值小于 <see cref="Count"/> 时抛出.</exception>
    public int Capacity
    {
        get => Items.Length - 1;
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, Count);
            if (value == Capacity) return;
            if (value == 0)
            {
                Items = s_EmptyArray;
                return;
            }

            Node[] destination = new Node[value + 1];
            if (Count > 0) Array.Copy(Items, destination, Count + 1);
            Items = destination;
        }
    }

    /// <summary>
    /// 获取头节点.
    /// </summary>
    public LinkedArrayNode<T>? First => Count == 0 ? null : new LinkedArrayNode<T>(Items[0].Next, this);

    /// <summary>
    /// 获取尾节点.
    /// </summary>
    public LinkedArrayNode<T>? Last => Count == 0 ? null : new LinkedArrayNode<T>(Items[0].Front, this);

    /// <summary>
    /// 获取头节点的值.
    /// 当容器为空时返回默认值.
    /// </summary>
    public T? FirstValue => Count == 0 ? default : Items[Items[0].Next].Value;

    /// <summary>
    /// 获取尾节点的值.
    /// 当容器为空时返回默认值.
    /// </summary>
    public T? LastValue => Count == 0 ? default : Items[Items[0].Front].Value;

    /// <summary>
    /// 向尾部添加一个元素.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    public void AddLast(T item) => Add(item);

    /// <summary>
    /// 向头部添加一个元素.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    public void AddFirst(T item)
    {
        if (Count == 0)
        {
            Add(item);
            return;
        }

        InsertBetween(0, Items[0].Next, item);
    }

    /// <summary>
    /// 入队一个元素, 等价于 <see cref="AddLast"/>.
    /// </summary>
    /// <param name="item">要入队的元素.</param>
    public void Enqueue(T item) => Add(item);

    /// <summary>
    /// 尝试出队一个元素.
    /// </summary>
    /// <param name="item">出队成功时返回队首元素.</param>
    /// <returns>出队成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool TryDequeue([MaybeNullWhen(false)] out T item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        int head = Items[0].Next;
        item = Items[head].Value;
        RemoveNodeIndex(head);
        return true;
    }

    /// <summary>
    /// 出队一个元素.
    /// 当容器为空时返回默认值.
    /// </summary>
    /// <returns>出队元素或默认值.</returns>
    public T? Dequeue()
    {
        if (!TryDequeue(out T? item)) return default;
        return item;
    }

    /// <summary>
    /// 移除头节点.
    /// </summary>
    /// <returns>移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool RemoveFirst()
    {
        if (Count == 0) return false;
        RemoveNodeIndex(Items[0].Next);
        return true;
    }

    /// <summary>
    /// 移除尾节点.
    /// </summary>
    /// <returns>移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool RemoveLast()
    {
        if (Count == 0) return false;
        RemoveNodeIndex(Items[0].Front);
        return true;
    }

    /// <summary>
    /// 在指定节点之后插入新元素.
    /// </summary>
    /// <param name="node">基准节点.</param>
    /// <param name="value">要插入的元素值.</param>
    /// <returns>新插入节点的句柄.</returns>
    /// <exception cref="InvalidOperationException">节点不属于当前实例或节点已失效时抛出.</exception>
    public LinkedArrayNode<T> AddAfter(LinkedArrayNode<T> node, T value)
    {
        ValidateNode(node);
        int current = InsertBetween(node.Index, Items[node.Index].Next, value);
        return new LinkedArrayNode<T>(current, this);
    }

    /// <summary>
    /// 在指定节点之前插入新元素.
    /// </summary>
    /// <param name="node">基准节点.</param>
    /// <param name="value">要插入的元素值.</param>
    /// <returns>新插入节点的句柄.</returns>
    /// <exception cref="InvalidOperationException">节点不属于当前实例或节点已失效时抛出.</exception>
    public LinkedArrayNode<T> AddBefore(LinkedArrayNode<T> node, T value)
    {
        ValidateNode(node);
        int current = InsertBetween(Items[node.Index].Front, node.Index, value);
        return new LinkedArrayNode<T>(current, this);
    }

    /// <summary>
    /// 移除指定节点.
    /// </summary>
    /// <param name="node">要移除的节点句柄.</param>
    /// <returns>移除成功返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public bool Remove(LinkedArrayNode<T> node)
    {
        if (node.Target != this || node.Version != Version) return false;
        RemoveNodeIndex(node.Index);
        return true;
    }

    /// <summary>
    /// 查找第一个匹配指定值的节点.
    /// </summary>
    /// <param name="item">要查找的值.</param>
    /// <returns>匹配节点句柄; 未找到返回 <see langword="null"/>.</returns>
    public LinkedArrayNode<T>? Find(T item)
    {
        int index = FindNodeIndex(item);
        return index < 0 ? null : new LinkedArrayNode<T>(index, this);
    }

    /// <summary>
    /// 反向查找最后一个匹配指定值的节点.
    /// </summary>
    /// <param name="item">要查找的值.</param>
    /// <returns>匹配节点句柄; 未找到返回 <see langword="null"/>.</returns>
    public LinkedArrayNode<T>? FindLast(T item)
    {
        int index = FindLastNodeIndex(item);
        return index < 0 ? null : new LinkedArrayNode<T>(index, this);
    }

    /// <summary>
    /// 批量添加元素.
    /// </summary>
    /// <param name="collection">要添加的集合.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> 为 <see langword="null"/> 时抛出.</exception>
    public void AddRange(IEnumerable<T> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        if (ReferenceEquals(collection, this))
        {
            int originalCount = Count;
            if (originalCount == 0) return;

            T[] snapshot = new T[originalCount];
            CopyTo(snapshot, 0);
            EnsureCapacity(checked(originalCount * 2));

            foreach (T item in snapshot) Add(item);
            return;
        }

        if (collection is ICollection<T> source) EnsureCapacity(checked(Count + source.Count));
        foreach (T item in collection) Add(item);
    }

    /// <summary>
    /// 返回当前容器的只读包装.
    /// </summary>
    /// <returns>只读包装对象.</returns>
    public ReadOnlyCollection<T> AsReadOnly() => new ReadOnlyCollection<T>(this);

    /// <summary>
    /// 将当前元素批量转换为目标类型.
    /// </summary>
    /// <typeparam name="TOutput">目标元素类型.</typeparam>
    /// <param name="converter">元素转换委托.</param>
    /// <returns>转换后的列表.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="converter"/> 为 <see langword="null"/> 时抛出.</exception>
    public List<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        List<TOutput> result = new List<TOutput>(Count);
        int node = Items[0].Next;
        while (node != 0)
        {
            result.Add(converter(Items[node].Value));
            node = Items[node].Next;
        }

        return result;
    }

    /// <summary>
    /// 确保容量至少为指定值.
    /// </summary>
    /// <param name="capacity">期望的最小容量.</param>
    /// <returns>调整后的容量.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于 0 时抛出.</exception>
    public int EnsureCapacity(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        GrowForCount(capacity);
        return Capacity;
    }

    /// <summary>
    /// 当实际使用率过低时, 缩减容量.
    /// </summary>
    public void TrimExcess()
    {
        if (Count >= (int)(Capacity * 0.9)) return;
        Capacity = Count;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private int InsertBetween(int front, int next, T value)
    {
        int newIndex = checked(Count + 1);
        GrowForCount(newIndex);
        Count = newIndex;
        Items[newIndex] = new Node(value, front, next);
        Items[front].Next = newIndex;
        Items[next].Front = newIndex;
        Version++;
        return newIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void GrowForCount(int requiredCount)
    {
        if (requiredCount <= Capacity) return;

        int currentCapacity = Capacity;
        int newCapacity = currentCapacity == 0 ? 4 : checked(currentCapacity * 2);
        if (newCapacity < requiredCount) newCapacity = requiredCount;
        Capacity = newCapacity;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void RemoveNodeIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);

        int front = Items[index].Front;
        int next = Items[index].Next;

        // 先摘链, 让前后节点直接相连.
        Items[front].Next = next;
        Items[next].Front = front;

        int lastIndex = Count;
        if (index != lastIndex)
        {
            // 通过“尾节点搬移”保持存储区连续, 避免维护空洞索引.
            Node moved = Items[lastIndex];
            Items[index] = moved;
            Items[moved.Front].Next = index;
            Items[moved.Next].Front = index;
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Items[lastIndex] = default!;
        }

        Count = lastIndex - 1;
        Version++;
    }

    private int GetNodeIndexByListIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        int node = Items[0].Next;
        for (int i = 0; i < index; i++) node = Items[node].Next;
        return node;
    }

    private int FindNodeIndex(T item)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int node = Items[0].Next;
        while (node != 0)
        {
            if (comparer.Equals(Items[node].Value, item)) return node;
            node = Items[node].Next;
        }

        return -1;
    }

    private int FindLastNodeIndex(T item)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int node = Items[0].Front;
        while (node != 0)
        {
            if (comparer.Equals(Items[node].Value, item)) return node;
            node = Items[node].Front;
        }

        return -1;
    }

    private void ValidateNode(LinkedArrayNode<T> node)
    {
        if (node.Target != this || node.Version != Version)
        {
            throw new InvalidOperationException("The linked-array node is invalid for this instance.");
        }
    }

    private static bool IsCompatibleObject(object? value)
    {
        if (value is T) return true;
        return value is null && default(T) is null;
    }

    private static void ThrowIfWrongValueType(object? value)
    {
        if (IsCompatibleObject(value)) return;
        throw new ArgumentException($"Value must be assignable to {typeof(T).FullName}.", nameof(value));
    }
}
