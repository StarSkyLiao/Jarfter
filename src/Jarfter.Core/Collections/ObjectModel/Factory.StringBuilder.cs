using System.Runtime.CompilerServices;
using System.Text;

namespace Jarfter.Core.Collections.ObjectModel;

public static partial class Factory
{
    /// <summary>
    /// 从对象池租借一个字符串生成器.
    /// </summary>
    /// <returns>可供调用方使用的字符串生成器.</returns>
    public static StringBuilder RentStringBuilder() => InternalStringBuilderPool.Get();

    /// <summary>
    /// 获取字符串生成器中的文本并将其归还到对象池.
    /// </summary>
    /// <param name="stringBuilder">要归还的字符串生成器.</param>
    /// <returns>归还前字符串生成器中的文本.</returns>
    public static string Release(StringBuilder stringBuilder) => InternalStringBuilderPool.Release(stringBuilder);

    private static class InternalStringBuilderPool
    {
        private static readonly Stack<StringBuilder> s_Queue = new Stack<StringBuilder>(8);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static StringBuilder Get()
        {
            lock (s_Queue)
            {
                StringBuilder result = s_Queue.Count != 0 ? s_Queue.Pop() : new StringBuilder();
                return result;
            }
        }

        // ReSharper disable once MemberHidesStaticFromOuterClass
        public static string Release(StringBuilder list)
        {
            lock (s_Queue)
            {
                string result = list.ToString();
                list.Clear();
                s_Queue.Push(list);
                return result;
            }
        }
    }
}
