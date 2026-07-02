using System.Collections;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Collections.Generic;

public sealed partial class LinkedArray<T>
{
    /// <summary>
    /// 返回泛型枚举器.
    /// </summary>
    /// <returns>枚举器实例.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// 返回泛型枚举器.
    /// </summary>
    /// <returns>枚举器实例.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// 返回非泛型枚举器.
    /// </summary>
    /// <returns>枚举器实例.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// 表示 <see cref="LinkedArray{T}"/> 的枚举器.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly LinkedArray<T> m_Target;
        private readonly int m_Version;
        private int m_Next;
        private T? m_Current;

        /// <summary>
        /// 初始化枚举器.
        /// </summary>
        /// <param name="target">目标容器.</param>
        internal Enumerator(LinkedArray<T> target)
        {
            m_Target = target;
            m_Version = target.Version;
            m_Next = target.Items[0].Next;
            m_Current = default;
        }

        /// <summary>
        /// 获取当前元素.
        /// </summary>
        public T Current => m_Current!;

        /// <summary>
        /// 获取当前元素.
        /// </summary>
        object? IEnumerator.Current => Current;

        /// <summary>
        /// 移动到下一个元素.
        /// </summary>
        /// <returns>存在下一个元素返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">检测到结构被修改时抛出.</exception>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public bool MoveNext()
        {
            if (m_Version != m_Target.Version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            if (m_Next == 0)
            {
                m_Current = default;
                return false;
            }

            int current = m_Next;
            m_Current = m_Target.Items[current].Value;
            m_Next = m_Target.Items[current].Next;
            return true;
        }

        /// <summary>
        /// 重置枚举器.
        /// </summary>
        /// <exception cref="InvalidOperationException">检测到结构被修改时抛出.</exception>
        void IEnumerator.Reset()
        {
            if (m_Version != m_Target.Version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            m_Current = default;
            m_Next = m_Target.Items[0].Next;
        }

        /// <summary>
        /// 释放枚举器资源.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
