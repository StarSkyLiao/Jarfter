namespace Jarfter.Core.Collections.Generic;

public ref partial struct SpanList<T>
{
    /// <summary>
    /// 判断当前容器是否包含指定元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    public readonly bool Contains(T item) => IndexOf(item) >= 0;

    /// <summary>
    /// 查找指定元素的首个索引.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到返回索引, 未找到返回 -1.</returns>
    public readonly int IndexOf(T item)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < Count; i++)
        {
            if (comparer.Equals(m_Buffer[i], item)) return i;
        }

        return -1;
    }
}
