using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.ObjectModel;

public static partial class Factory
{
    /// <summary>
    /// 从对象池租借一个实现 <see cref="IPoolable{T}"/> 的对象.
    /// </summary>
    /// <typeparam name="T">要租借的对象类型.</typeparam>
    /// <returns>可供调用方使用的对象.</returns>
    public static T Rent<T>() where T : class, IPoolable<T> => InternalPool<T>.Get();

    /// <summary>
    /// 将对象归还到其所属的对象池.
    /// </summary>
    /// <typeparam name="T">要归还的对象类型.</typeparam>
    /// <param name="poolable">要归还的对象.</param>
    public static void Release<T>(T poolable) where T : class, IPoolable<T> => poolable.Release();

    private static class InternalPool<T> where T : class, IPoolable<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ConcurrentStack<T> s_Queue = new ConcurrentStack<T>();

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static T Get()
        {
            if (!s_Queue.TryPop(out T? result)) result = T.CreatePooled();
            result.SourcePool = s_Queue;
            return result;
        }
    }
}
