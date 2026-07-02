using System.Diagnostics.CodeAnalysis;

namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示一个基于外部 <see cref="Span{T}"/> 的固定容量线性容器.
/// 当容量不足时, 写操作会抛出 <see cref="InvalidOperationException"/>.
/// </summary>
/// <typeparam name="T">元素类型.</typeparam>
public ref partial struct SpanList<T>
{
    private Span<T> m_Buffer;

    /// <summary>
    /// 获取当前已使用元素数量.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// 获取底层缓冲区容量.
    /// </summary>
    public int Capacity => m_Buffer.Length;

    /// <summary>
    /// 获取当前实例是否已满.
    /// </summary>
    public bool IsFull => Count == m_Buffer.Length;

    /// <summary>
    /// 初始化一个空的 <see cref="SpanList{T}"/> 实例.
    /// </summary>
    /// <param name="buffer">底层存储缓冲区.</param>
    public SpanList(Span<T> buffer)
    {
        m_Buffer = buffer;
        Count = 0;
    }

    /// <summary>
    /// 使用指定缓冲区和初始长度初始化 <see cref="SpanList{T}"/> 实例.
    /// </summary>
    /// <param name="buffer">底层存储缓冲区.</param>
    /// <param name="initialCount">初始已使用元素数量.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="initialCount"/> 小于 0 或大于缓冲区长度时抛出.
    /// </exception>
    public SpanList(Span<T> buffer, int initialCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCount);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialCount, buffer.Length);
        m_Buffer = buffer;
        Count = initialCount;
    }

    /// <summary>
    /// 获取或设置指定索引处的元素.
    /// </summary>
    /// <param name="index">元素索引.</param>
    /// <exception cref="ArgumentOutOfRangeException">索引超出有效范围时抛出.</exception>
    public ref T this[int index]
    {
        get
        {
            ValidateIndex(index);
            return ref m_Buffer[index];
        }
    }

    /// <summary>
    /// 获取当前有效数据对应的可写切片.
    /// </summary>
    /// <returns>有效元素切片.</returns>
    public Span<T> AsSpan() => m_Buffer[..Count];

    /// <summary>
    /// 获取当前有效数据对应的只读切片.
    /// </summary>
    /// <returns>有效元素只读切片.</returns>
    public ReadOnlySpan<T> AsReadOnlySpan() => m_Buffer[..Count];

    /// <summary>
    /// 返回可用于 <c>foreach</c> 的枚举器.
    /// </summary>
    /// <returns>有效元素的枚举器.</returns>
    public Span<T>.Enumerator GetEnumerator() => m_Buffer[..Count].GetEnumerator();

    private readonly void ValidateIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
    }

    private readonly void ValidateInsertIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, Count);
    }

    private readonly void EnsureCapacityFor(int requiredCount)
    {
        if (requiredCount > m_Buffer.Length)
        {
            ThrowCapacityExceededException();
        }
    }
    
    [DoesNotReturn]
    private static void ThrowCapacityExceededException()
    {
        throw new InvalidOperationException("The SpanList capacity is insufficient for this operation.");
    }
}
