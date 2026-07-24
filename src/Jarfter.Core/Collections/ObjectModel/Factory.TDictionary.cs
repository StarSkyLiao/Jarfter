using System.Collections;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.ObjectModel;

public static partial class Factory
{
    /// <summary>
    /// 从对象池租借一个字典.
    /// </summary>
    /// <typeparam name="TDictionary">字典的具体类型.</typeparam>
    /// <returns>已清空且可供调用方使用的字典.</returns>
    public static TDictionary RentDictionary<TDictionary>() where TDictionary : IDictionary, new()
        => InternalDictionaryPool<TDictionary>.Get();

    /// <summary>
    /// 清空字典并归还到对象池.
    /// </summary>
    /// <typeparam name="TDictionary">字典的具体类型.</typeparam>
    /// <param name="collection">要归还的字典.</param>
    public static void ReleaseDictionary<TDictionary>(TDictionary collection) where TDictionary : IDictionary, new()
        => InternalDictionaryPool<TDictionary>.Release(collection);

    private static class InternalDictionaryPool<TDictionary> where TDictionary : IDictionary, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Stack<TDictionary> s_Queue = new Stack<TDictionary>(8);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static TDictionary Get()
        {
            lock (s_Queue)
            {
                TDictionary result = s_Queue.Count != 0 ? s_Queue.Pop() : [];
                return result;
            }
        }

        public static void Release(TDictionary list)
        {
            lock (s_Queue)
            {
                list.Clear();
                s_Queue.Push(list);
            }
        }
    }
}
