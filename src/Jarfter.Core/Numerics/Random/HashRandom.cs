using Jarfter.Core.Numerics.Hash;

namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 提供由哈希链驱动的确定性随机数生成器.
/// <para>每次读取 <see cref="Value"/> 或调用生成方法都会推进内部状态. 相同初始种子会产生相同序列.</para>
/// </summary>
public sealed class HashRandom(int value) : IRandomSource
{
    /// <summary>
    /// 获取当前哈希状态, 不推进随机序列.
    /// </summary>
    public int State { get; private set; } = value;

    /// <summary>
    /// 获取并推进当前哈希状态.
    /// </summary>
    public int Value => State = HashCodes.Combine(State);

    /// <summary>
    /// 使用指定种子重置当前随机序列.
    /// </summary>
    /// <param name="seed">新的初始种子.</param>
    public void Reset(int seed) => State = seed;

    /// <summary>
    /// 根据当前状态和指定盐值派生一个独立的随机序列.
    /// <para>该操作不推进当前实例的状态.</para>
    /// </summary>
    /// <param name="salt">用于区分派生序列的盐值.</param>
    /// <returns>由当前状态和 <paramref name="salt"/> 决定的新随机数生成器.</returns>
    public HashRandom Fork<T>(T salt) => new HashRandom(HashCodes.Combine(State, salt));

    /// <inheritdoc />
    public int NextInt32(int minInclusive, int maxExclusive) => HashRandomUtil.Range(Value, minInclusive, maxExclusive);

    /// <inheritdoc />
    public long NextInt64(long minInclusive, long maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "上限必须大于下限.");

        ulong range = unchecked((ulong)(maxExclusive - minInclusive));
        ulong threshold = unchecked(0UL - range) % range;
        ulong value;

        do value = NextUInt64();
        while (value < threshold);

        return unchecked((long)((ulong)minInclusive + value % range));
    }

    /// <inheritdoc />
    public float NextSingle() => HashRandomUtil.Single(Value);

    /// <inheritdoc />
    public double NextDouble() => HashRandomUtil.Double(Value);

    private ulong NextUInt64()
    {
        uint high = (uint)Value;
        uint low = (uint)Value;
        return ((ulong)high << 32) | low;
    }
}
