namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 实现 64 位 Mersenne Twister 伪随机数生成器.
/// <para>该类型不保证线程安全. 未显式设定种子时, 首次生成随机数会使用默认种子 5489.</para>
/// </summary>
public sealed class Mt19937 : IRandomSource
{
    private const int StateSize = 312;
    private const int MiddleWordOffset = 156;
    private const ulong MatrixA = 0xB5026F5AA96619E9;
    private const ulong UpperMask = 0xFFFFFFFF80000000;
    private const ulong LowerMask = 0x7FFFFFFF;
    private const ulong TemperingMaskB = 0x71D67FFFEDA60000;
    private const ulong TemperingMaskC = 0xFFF7EEE000000000;

    private static readonly ulong[] s_Mag01 = [0, MatrixA];

    private readonly ulong[] m_State = new ulong[StateSize];
    private uint m_Index = StateSize + 1;

    /// <summary>
    /// 使用一个非零种子初始化生成器状态.
    /// </summary>
    /// <param name="seed">用于初始化状态的种子.</param>
    public void Seed(ulong seed)
    {
        ArgumentOutOfRangeException.ThrowIfZero(seed);
        m_State[0] = seed;
        for (m_Index = 1; m_Index < StateSize; m_Index++)
            m_State[m_Index] = 6364136223846793005 * (m_State[m_Index - 1] ^ (m_State[m_Index - 1] >> 62)) + m_Index;
    }

    /// <summary>
    /// 使用种子数组初始化生成器状态.
    /// </summary>
    /// <param name="initKey">用于初始化状态的种子数组.</param>
    /// <exception cref="ArgumentNullException"><paramref name="initKey"/> 为 <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="initKey"/> 为空.</exception>
    public void Seed(ulong[] initKey)
    {
        ArgumentNullException.ThrowIfNull(initKey);
        if (initKey.Length == 0) throw new ArgumentException("种子数组不能为空.", nameof(initKey));

        ulong keyLength = (ulong)initKey.Length;
        Seed(19650218UL);

        ulong i = 1;
        ulong j = 0;
        ulong k = Math.Max(StateSize, keyLength);

        for (; k > 0; k--)
        {
            m_State[i] = (m_State[i] ^ (m_State[i - 1] ^ (m_State[i - 1] >> 62)) * 3935559000370003845UL) + initKey[j] + j;
            i++;
            j++;

            if (i >= StateSize)
            {
                m_State[0] = m_State[StateSize - 1];
                i = 1;
            }

            if (j >= keyLength) j = 0;
        }

        for (k = StateSize - 1; k > 0; k--)
        {
            m_State[i] = (m_State[i] ^ (m_State[i - 1] ^ (m_State[i - 1] >> 62)) * 2862933555777941757UL) - i;
            i++;

            if (i >= StateSize)
            {
                m_State[0] = m_State[StateSize - 1];
                i = 1;
            }
        }

        // 将最高位置为 1, 确保内部状态不全为零.
        m_State[0] = 1UL << 63;
    }

    /// <summary>
    /// 获取下一个无符号 64 位随机整数.
    /// </summary>
    /// <returns>下一个伪随机 <see cref="ulong"/> 值.</returns>
    public ulong UInt64()
    {
        if (m_Index >= StateSize)
        {
            if (m_Index == StateSize + 1) Seed(5489);

            GenerateState();
        }

        ulong value = m_State[m_Index++];
        value ^= (value >> 29) & 0x5555555555555555;
        value ^= (value << 17) & TemperingMaskB;
        value ^= (value << 37) & TemperingMaskC;
        value ^= value >> 43;
        return value;
    }

    /// <inheritdoc />
    public int NextInt32(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "上限必须大于下限.");

        ulong range = (ulong)((long)maxExclusive - minInclusive);
        ulong threshold = unchecked(0UL - range) % range;
        ulong value;

        do value = UInt64();
        while (value < threshold);

        return (int)(minInclusive + (long)(value % range));
    }

    /// <inheritdoc />
    public long NextInt64(long minInclusive, long maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "上限必须大于下限.");

        ulong range = unchecked((ulong)(maxExclusive - minInclusive));
        ulong threshold = unchecked(0UL - range) % range;
        ulong value;

        do value = UInt64();
        while (value < threshold);

        return unchecked((long)((ulong)minInclusive + value % range));
    }

    /// <inheritdoc />
    public float NextSingle() => (UInt64() >> 40) * (1f / (1 << 24));

    /// <inheritdoc />
    public double NextDouble() => (UInt64() >> 11) * (1d / (1L << 53));

    /// <summary>
    /// 获取下一个非负的 63 位随机整数.
    /// </summary>
    /// <returns>位于 [0, 2<sup>63</sup> - 1] 的伪随机整数.</returns>
    public long Int63() => (long)(UInt64() >> 1);

    /// <summary>
    /// 获取位于 [0, 1] 的随机双精度浮点数.
    /// </summary>
    /// <returns>位于 [0, 1] 的伪随机双精度浮点数.</returns>
    public double Real1() => (UInt64() >> 11) * (1.0 / 9007199254740991.0);

    /// <summary>
    /// 获取位于 [0, 1) 的随机双精度浮点数.
    /// </summary>
    /// <returns>位于 [0, 1) 的伪随机双精度浮点数.</returns>
    public double Real2() => (UInt64() >> 11) * (1.0 / 9007199254740992.0);

    /// <summary>
    /// 获取位于 (0, 1) 的随机双精度浮点数.
    /// </summary>
    /// <returns>位于 (0, 1) 的伪随机双精度浮点数.</returns>
    public double Real3() => ((UInt64() >> 12) + 0.5) * (1.0 / 4503599627370496.0);

    private void GenerateState()
    {
        // 一次刷新整个状态数组, 以摊销扭转状态所需的成本.
        int index;
        for (index = 0; index < StateSize - MiddleWordOffset; index++)
        {
            ulong value = (m_State[index] & UpperMask) | (m_State[index + 1] & LowerMask);
            m_State[index] = m_State[index + MiddleWordOffset] ^ (value >> 1) ^ s_Mag01[value & 1];
        }

        for (; index < StateSize - 1; index++)
        {
            ulong value = (m_State[index] & UpperMask) | (m_State[index + 1] & LowerMask);
            m_State[index] = m_State[index + (MiddleWordOffset - StateSize)] ^ (value >> 1) ^ s_Mag01[value & 1];
        }

        ulong lastValue = (m_State[StateSize - 1] & UpperMask) | (m_State[0] & LowerMask);
        m_State[StateSize - 1] = m_State[MiddleWordOffset - 1] ^ (lastValue >> 1) ^ s_Mag01[lastValue & 1];
        m_Index = 0;
    }
}
