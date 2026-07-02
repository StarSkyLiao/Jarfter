using System.Buffers;
using System.Numerics;

namespace Jarfter.Core.Text;

/// <summary>
/// 表示一个基于 <see cref="ArrayPool{T}"/> 的可增长字符缓冲区, 适用于高频字符串拼接场景.
/// </summary>
public ref struct InterpolatedString : IDisposable
{
    /// <summary>
    /// 保存租用的字符数组.
    /// </summary>
    private char[]? m_InternalCharArray;

    /// <summary>
    /// 表示当前可写入的字符区域.
    /// </summary>
    internal Span<char> InternalCharSpan;

    /// <summary>
    /// 获取当前已写入的字符长度.
    /// </summary>
    public int Length { get; internal set; }

    /// <summary>
    /// 初始化一个默认容量的 <see cref="InterpolatedString"/> 实例.
    /// </summary>
    public InterpolatedString()
    {
        Length = 0;
        m_InternalCharArray = ArrayPool<char>.Shared.Rent(8);
        InternalCharSpan = m_InternalCharArray.AsSpan();
    }

    /// <summary>
    /// 初始化一个指定容量的 <see cref="InterpolatedString"/> 实例.
    /// </summary>
    /// <param name="capacity">期望的初始容量, 最小为 8.</param>
    public InterpolatedString(int capacity = 8)
    {
        Length = 0;
        int minimumLength = int.Max(capacity, 8);
        m_InternalCharArray = ArrayPool<char>.Shared.Rent(minimumLength);
        InternalCharSpan = m_InternalCharArray.AsSpan();
    }

    /// <summary>
    /// 使用指定字符串初始化 <see cref="InterpolatedString"/> 实例.
    /// </summary>
    /// <param name="initialString">用于初始化内容的字符串.</param>
    public InterpolatedString(string initialString)
    {
        Length = initialString.Length;
        int minimumLength = int.Max(Length, 1 << (BitOperations.Log2((uint)Length) + 1));
        m_InternalCharArray = ArrayPool<char>.Shared.Rent(minimumLength);
        InternalCharSpan = m_InternalCharArray.AsSpan();
        initialString.CopyTo(InternalCharSpan);
    }

    /// <summary>
    /// 获取当前有效内容对应的字符区域.
    /// </summary>
    private Span<char> Text => InternalCharSpan[..Length];

    /// <summary>
    /// 将当前内容转换为字符串.
    /// </summary>
    /// <returns>当前内容对应的字符串.</returns>
    public override string ToString() => new string(Text);

    /// <summary>
    /// 将当前内容转换为字符串并清理内部状态.
    /// </summary>
    /// <returns>当前内容对应的字符串.</returns>
    /// <remarks>
    /// 该方法会释放租用数组, 通常只应在实例生命周期末尾调用一次.
    /// 调用后继续使用当前实例属于未定义行为.
    /// </remarks>
    public string ToStringAndClear()
    {
        string result = new string(Text);
        Clear();
        return result;
    }

    /// <summary>
    /// 清理实例状态并将租用数组归还到池中.
    /// </summary>
    /// <remarks>
    /// 该方法会释放租用数组, 通常只应在实例生命周期末尾调用一次.
    /// 调用后继续使用当前实例属于未定义行为.
    /// </remarks>
    public void Clear()
    {
        char[]? toReturn = m_InternalCharArray;
        // 先断开当前实例对池数组的引用, 避免后续误用或重复归还.
        this = default;
        if (toReturn is not null) ArrayPool<char>.Shared.Return(toReturn);
    }

    /// <summary>
    /// 仅清空长度计数, 不归还租用数组.
    /// </summary>
    public void SafeClear() => Length = 0;

    /// <summary>
    /// 在追加字符串前扩容并完成拷贝.
    /// </summary>
    /// <param name="value">要追加的字符串.</param>
    internal void GrowThenCopyString(string value)
    {
        GrowCore(1 << (BitOperations.Log2((uint)Length + (uint)value.Length) + 1));
        value.CopyTo(InternalCharSpan[Length..]);
        Length += value.Length;
    }

    /// <summary>
    /// 在追加字符区域前扩容并完成拷贝.
    /// </summary>
    /// <param name="value">要追加的字符区域.</param>
    internal void GrowThenCopySpan(scoped ReadOnlySpan<char> value)
    {
        int requiredMinCapacity = checked(Length + value.Length);
        GrowCore(requiredMinCapacity);
        value.CopyTo(InternalCharSpan[Length..]);
        Length += value.Length;
    }

    /// <summary>
    /// 扩容内部缓冲区, 并保留已有内容.
    /// </summary>
    /// <param name="requiredMinCapacity">扩容后的最小容量要求.</param>
    internal void GrowCore(int requiredMinCapacity)
    {
        int newCapacity = int.Max(InternalCharSpan.Length << 1, requiredMinCapacity);
        int arraySize = int.Clamp(newCapacity, 4, int.MaxValue);

        char[] newArray = ArrayPool<char>.Shared.Rent(arraySize);
        InternalCharSpan[..Length].CopyTo(newArray);

        char[]? toReturn = m_InternalCharArray;
        InternalCharSpan = m_InternalCharArray = newArray;

        // 新数组接管后, 将旧数组归还到池中.
        if (toReturn is not null) ArrayPool<char>.Shared.Return(toReturn);
    }

    /// <summary>
    /// 释放当前实例持有的池化资源.
    /// </summary>
    public void Dispose() => Clear();
}
