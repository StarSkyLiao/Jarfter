using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.ObjectModel;

public static partial class Factory
{
    /// <summary>
    /// 从对象池租借一个列表.
    /// </summary>
    /// <typeparam name="T">列表元素的类型.</typeparam>
    /// <returns>已清空且可供调用方使用的列表.</returns>
    public static List<T> RentList<T>() => InternalListPool<T>.Get();

    /// <summary>
    /// 清空列表并归还到对象池.
    /// </summary>
    /// <typeparam name="T">列表元素的类型.</typeparam>
    /// <param name="list">要归还的列表.</param>
    public static void Release<T>(List<T> list) => InternalListPool<T>.Release(list);

    private static class InternalListPool<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Stack<List<T>> s_Queue = new Stack<List<T>>(8);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static List<T> Get()
        {
            lock (s_Queue)
            {
                List<T> result = s_Queue.Count != 0 ? s_Queue.Pop() : [];
                return result;
            }
        }

        public static void Release(List<T> list)
        {
            lock (s_Queue)
            {
                list.Clear();
                s_Queue.Push(list);
            }
        }
    }
}
