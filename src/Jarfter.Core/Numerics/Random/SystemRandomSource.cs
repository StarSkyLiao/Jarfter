namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 将 <see cref="System.Random"/> 适配为 <see cref="IRandomSource"/>.
/// <para>该适配器不会复制或重置传入实例的状态. 多个适配器包装同一实例时会共享随机序列.</para>
/// </summary>
public sealed class SystemRandomSource : IRandomSource
{
    private readonly System.Random m_Random;

    /// <summary>
    /// 使用指定的 BCL 随机数生成器创建适配器.
    /// </summary>
    /// <param name="random">要包装的随机数生成器.</param>
    /// <exception cref="ArgumentNullException"><paramref name="random"/> 为 <see langword="null"/>.</exception>
    public SystemRandomSource(System.Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        m_Random = random;
    }

    /// <inheritdoc />
    public int NextInt32(int minInclusive, int maxExclusive) => m_Random.Next(minInclusive, maxExclusive);

    /// <inheritdoc />
    public long NextInt64(long minInclusive, long maxExclusive) => m_Random.NextInt64(minInclusive, maxExclusive);

    /// <inheritdoc />
    public float NextSingle() => m_Random.NextSingle();

    /// <inheritdoc />
    public double NextDouble() => m_Random.NextDouble();
}
