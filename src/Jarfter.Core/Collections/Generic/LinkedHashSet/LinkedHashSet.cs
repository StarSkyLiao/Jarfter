namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示一个具有固定最大容量的哈希集合.
/// 集合按从新到旧的顺序枚举; 添加已存在的元素会将其移动到首部, 添加新元素超出容量时会淘汰尾部元素.
/// </summary>
/// <typeparam name="T">元素类型.</typeparam>
public partial class LinkedHashSet<T> where T : notnull
{
    private readonly int m_Capacity;
    private readonly LinkedList<T> m_LinkedList = [];
    private readonly Dictionary<T, LinkedListNode<T>> m_Dictionary = [];

    /// <summary>
    /// 获取集合允许保留的最大元素数量.
    /// </summary>
    public int Capacity => m_Capacity;

    /// <summary>
    /// 初始化一个具有指定最大容量的 <see cref="LinkedHashSet{T}"/> 实例.
    /// </summary>
    /// <param name="capacity">集合允许保留的最大元素数量.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> 小于 0 时抛出.</exception>
    public LinkedHashSet(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        m_Capacity = capacity;
    }
}
