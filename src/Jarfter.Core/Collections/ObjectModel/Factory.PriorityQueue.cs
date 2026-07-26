using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.ObjectModel;

public static partial class Factory
{
    /// <summary>
    /// 从对象池租借使用默认优先级比较器的优先队列.
    /// </summary>
    /// <typeparam name="TElement">队列元素的类型.</typeparam>
    /// <typeparam name="TPriority">元素优先级的类型.</typeparam>
    /// <returns>已清空且可供调用方使用的优先队列.</returns>
    public static PriorityQueue<TElement, TPriority> RentPriorityQueue<TElement, TPriority>()
        => InternalPriorityQueuePool<TElement, TPriority>.Get();

    /// <summary>
    /// 清空使用默认优先级比较器的优先队列并归还到对象池.
    /// </summary>
    /// <typeparam name="TElement">队列元素的类型.</typeparam>
    /// <typeparam name="TPriority">元素优先级的类型.</typeparam>
    /// <param name="priorityQueue">要归还的优先队列.</param>
    public static void ReleasePriorityQueue<TElement, TPriority>(PriorityQueue<TElement, TPriority> priorityQueue)
        => InternalPriorityQueuePool<TElement, TPriority>.Release(priorityQueue);

    private static class InternalPriorityQueuePool<TElement, TPriority>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Stack<PriorityQueue<TElement, TPriority>> s_Queue =
            new Stack<PriorityQueue<TElement, TPriority>>(8);

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public static PriorityQueue<TElement, TPriority> Get()
        {
            lock (s_Queue)
            {
                PriorityQueue<TElement, TPriority> result =
                    s_Queue.Count != 0 ? s_Queue.Pop() : new PriorityQueue<TElement, TPriority>();
                return result;
            }
        }

        public static void Release(PriorityQueue<TElement, TPriority> priorityQueue)
        {
            lock (s_Queue)
            {
                priorityQueue.Clear();
                s_Queue.Push(priorityQueue);
            }
        }
    }
}
