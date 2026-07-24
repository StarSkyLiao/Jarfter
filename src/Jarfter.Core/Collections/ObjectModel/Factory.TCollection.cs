using System.Collections;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.ObjectModel;

/// <summary>
/// 提供了一系列工厂与对象池操作.
/// 租借的对象需要正确地使用, 否则会引发难以排查的错误.
/// </summary>
public static partial class Factory
{
    /// <summary>
    /// 从对象池租借一个集合.
    /// </summary>
    /// <typeparam name="TCollection">集合的具体类型.</typeparam>
    /// <typeparam name="TValue">集合元素的类型.</typeparam>
    /// <returns>已清空且可供调用方使用的集合.</returns>
    public static TCollection RentCollection<TCollection, TValue>() where TCollection : ICollection<TValue>, new()
        => InternalCollectionPool<TCollection, TValue>.Get();

    /// <summary>
    /// 清空集合并归还到对象池.
    /// </summary>
    /// <typeparam name="TCollection">集合的具体类型.</typeparam>
    /// <typeparam name="TValue">集合元素的类型.</typeparam>
    /// <param name="collection">要归还的集合.</param>
    public static void Release<TCollection, TValue>(TCollection collection) where TCollection : ICollection<TValue>, new()
        => InternalCollectionPool<TCollection, TValue>.Release(collection);

    private static class InternalCollectionPool<TCollection, TValue> where TCollection : ICollection<TValue>, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Stack<TCollection> s_Queue = new Stack<TCollection>(8);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static TCollection Get()
        {
            lock (s_Queue)
            {
                TCollection result = s_Queue.Count != 0 ? s_Queue.Pop() : [];
                return result;
            }
        }

        public static void Release(TCollection list)
        {
            lock (s_Queue)
            {
                list.Clear();
                s_Queue.Push(list);
            }
        }
    }
}
