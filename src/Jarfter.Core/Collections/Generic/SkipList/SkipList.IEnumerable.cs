using System.Collections;

namespace Jarfter.Core.Collections.Generic;

public partial class SkipList<T> : IEnumerable
{
    /// <summary>
    /// 返回循环访问跳表的枚举器.
    /// </summary>
    /// <returns>跳表的枚举器.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        SkipListNode? current = Head.Forward[0].Next;
        while (current != null)
        {
            yield return current.Value;
            current = current.Forward[0].Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
