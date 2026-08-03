using System.Collections;

namespace Jarfter.Core.Collections.Generic;

public partial class LinkedHashSet<T>
{
    /// <summary>
    /// 返回按从新到旧顺序枚举集合的枚举器.
    /// </summary>
    /// <returns>集合枚举器.</returns>
    public IEnumerator<T> GetEnumerator() => m_LinkedList.GetEnumerator();

    /// <summary>
    /// 返回按从新到旧顺序枚举集合的非泛型枚举器.
    /// </summary>
    /// <returns>集合的非泛型枚举器.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
