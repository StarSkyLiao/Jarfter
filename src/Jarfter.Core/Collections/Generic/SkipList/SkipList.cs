namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示可重复元素的跳表.
/// 跳表按比较器维持元素有序, 并通过跨度信息支持按索引访问.
/// 插入, 删除, 查找和索引访问的平均时间复杂度均为 O(log N). 此类型不保证线程安全.
/// </summary>
public partial class SkipList<T> where T : IComparable<T>
{
    private static readonly SkipListNode[] s_UpdateArray = new SkipListNode[MaxLevel];

    /// <summary>
    /// 获取用于排序和比较元素的比较器.
    /// </summary>
    internal Comparer<T> LocalComparer { get; } = Comparer<T>.Default;

    /// <summary>
    /// 获取第一个与指定元素比较相等的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? FirstEqual(T item)
    {
        SkipListNode? node = FirstEqual_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取最后一个与指定元素比较相等的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? LastEqual(T item)
    {
        SkipListNode? node = LastEqual_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取第一个大于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? FirstGreaterThan(T item)
    {
        SkipListNode? node = FirstGreaterThan_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取第一个大于或等于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T? FirstGreaterThanOrEqual(T item)
    {
        SkipListNode? node = FirstGreaterThanOrEqual_Internal(item);
        return node == null ? default : node.Value;
    }

    /// <summary>
    /// 获取最后一个小于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T LastLessThan(T item) => LastLessThan_Internal(item).Value;

    /// <summary>
    /// 获取最后一个小于或等于指定元素的元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的元素; 未找到时为默认值.</returns>
    public T LastLessThanOrEqual(T item) => LastLessThanOrEqual_Internal(item).Value;

    /// <summary>
    /// 返回范围内的所有元素.
    /// 参数 lower 与参数 upper 都是闭区间.
    /// </summary>
    /// <param name="lower">范围下限.</param>
    /// <param name="upper">范围上限.</param>
    /// <returns>位于指定闭区间内的元素序列.</returns>
    public IEnumerable<T> Range(T lower, T upper)
    {
        SkipListNode? current = FirstGreaterThanOrEqual_Internal(lower);
        if (current == null) yield break;
        while (LocalComparer.Compare(current.Value, upper) <= 0)
        {
            yield return current.Value;
            current = current.Forward[0].Next;
            if (current == null) yield break;
        }
    }

    /// <summary>
    /// 获取第一个不小于指定元素的插入索引.
    /// </summary>
    /// <param name="item">要定位边界的元素.</param>
    /// <returns>第一个不小于 <paramref name="item"/> 的元素索引; 不存在时为 <see cref="Count"/>.</returns>
    public int LowerBoundIndex(T item) => GetBoundIndex(item, skipEqualValues: false);

    /// <summary>
    /// 获取第一个大于指定元素的插入索引.
    /// </summary>
    /// <param name="item">要定位边界的元素.</param>
    /// <returns>第一个大于 <paramref name="item"/> 的元素索引; 不存在时为 <see cref="Count"/>.</returns>
    public int UpperBoundIndex(T item) => GetBoundIndex(item, skipEqualValues: true);

    #region 字段

    private const int MaxLevel = 32;
    private const float Probability = 0.25f;
    private int m_CurrLevel;
    /// <summary>
    /// 获取跳表的头节点.
    /// </summary>
    internal SkipListNode Head { get; } = new SkipListNode(MaxLevel, default!);

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用默认比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    public SkipList()
    {
    }

    /// <summary>
    /// 使用指定比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    /// <param name="comparer">用于排序和比较元素的比较器.</param>
    public SkipList(Comparer<T> comparer) => LocalComparer = comparer;

    /// <summary>
    /// 使用指定集合中的元素和默认比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    /// <param name="collection">要复制到跳表中的元素.</param>
    public SkipList(IEnumerable<T> collection)
    {
        T[] array = collection.ToArray();
        Array.Sort(array, LocalComparer);
        foreach (T item in array) Add(item);
    }

    /// <summary>
    /// 使用指定集合中的元素和比较器初始化 <see cref="SkipList{T}"/> 的新实例.
    /// </summary>
    /// <param name="collection">要复制到跳表中的元素.</param>
    /// <param name="comparer">用于排序和比较元素的比较器.</param>
    public SkipList(IEnumerable<T> collection, Comparer<T> comparer)
    {
        T[] array = collection.ToArray();
        Array.Sort(array, LocalComparer = comparer);
        foreach (T item in array) Add(item);
    }

    #endregion

}
