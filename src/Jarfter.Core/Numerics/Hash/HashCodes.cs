namespace Jarfter.Core.Numerics.Hash;

/// <summary>
/// 提供跨进程稳定的组合哈希与字符串哈希算法.
/// <para>组合哈希改编自 <see cref="System.HashCode"/>, 但使用固定种子, 因此相同输入在每次进程启动后均产生相同结果.</para>
/// </summary>
public static class HashCodes
{
    private const uint Seed = 1073676287;

    private const uint Prime1 = 2654435761U;
    private const uint Prime2 = 2246822519U;
    private const uint Prime3 = 3266489917U;
    private const uint Prime4 = 668265263U;
    private const uint Prime5 = 374761393U;

    /// <summary>
    /// 组合一个值的哈希代码.
    /// </summary>
    /// <param name="value1">要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1>(T1 value1)
    {
        // 扩散哈希空间有限的值, 以便作为集合键时得到更均匀的分布.

        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);

        uint hash = MixEmptyState();
        hash += 4;

        hash = QueueRound(hash, hc1);

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 组合两个值的哈希代码.
    /// </summary>
    /// <param name="value1">第一个要参与组合的值.</param>
    /// <param name="value2">第二个要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1, T2>(T1 value1, T2 value2)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);

        uint hash = MixEmptyState();
        hash += 8;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 组合三个值的哈希代码.
    /// </summary>
    /// <param name="value1">第一个要参与组合的值.</param>
    /// <param name="value2">第二个要参与组合的值.</param>
    /// <param name="value3">第三个要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);

        uint hash = MixEmptyState();
        hash += 12;

        hash = QueueRound(hash, hc1);
        hash = QueueRound(hash, hc2);
        hash = QueueRound(hash, hc3);

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 组合四个值的哈希代码.
    /// </summary>
    /// <param name="value1">第一个要参与组合的值.</param>
    /// <param name="value2">第二个要参与组合的值.</param>
    /// <param name="value3">第三个要参与组合的值.</param>
    /// <param name="value4">第四个要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 16;

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 组合五个值的哈希代码.
    /// </summary>
    /// <param name="value1">第一个要参与组合的值.</param>
    /// <param name="value2">第二个要参与组合的值.</param>
    /// <param name="value3">第三个要参与组合的值.</param>
    /// <param name="value4">第四个要参与组合的值.</param>
    /// <param name="value5">第五个要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 20;

        hash = QueueRound(hash, hc5);

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 组合六个值的哈希代码.
    /// </summary>
    /// <param name="value1">第一个要参与组合的值.</param>
    /// <param name="value2">第二个要参与组合的值.</param>
    /// <param name="value3">第三个要参与组合的值.</param>
    /// <param name="value4">第四个要参与组合的值.</param>
    /// <param name="value5">第五个要参与组合的值.</param>
    /// <param name="value6">第六个要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
        uint hc6 = (uint)(value6?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 24;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 组合七个值的哈希代码.
    /// </summary>
    /// <param name="value1">第一个要参与组合的值.</param>
    /// <param name="value2">第二个要参与组合的值.</param>
    /// <param name="value3">第三个要参与组合的值.</param>
    /// <param name="value4">第四个要参与组合的值.</param>
    /// <param name="value5">第五个要参与组合的值.</param>
    /// <param name="value6">第六个要参与组合的值.</param>
    /// <param name="value7">第七个要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
        uint hc6 = (uint)(value6?.GetHashCode() ?? 0);
        uint hc7 = (uint)(value7?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 28;

        hash = QueueRound(hash, hc5);
        hash = QueueRound(hash, hc6);
        hash = QueueRound(hash, hc7);

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 组合八个值的哈希代码.
    /// </summary>
    /// <param name="value1">第一个要参与组合的值.</param>
    /// <param name="value2">第二个要参与组合的值.</param>
    /// <param name="value3">第三个要参与组合的值.</param>
    /// <param name="value4">第四个要参与组合的值.</param>
    /// <param name="value5">第五个要参与组合的值.</param>
    /// <param name="value6">第六个要参与组合的值.</param>
    /// <param name="value7">第七个要参与组合的值.</param>
    /// <param name="value8">第八个要参与组合的值.</param>
    /// <returns>稳定的组合哈希代码.</returns>
    public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
    {
        uint hc1 = (uint)(value1?.GetHashCode() ?? 0);
        uint hc2 = (uint)(value2?.GetHashCode() ?? 0);
        uint hc3 = (uint)(value3?.GetHashCode() ?? 0);
        uint hc4 = (uint)(value4?.GetHashCode() ?? 0);
        uint hc5 = (uint)(value5?.GetHashCode() ?? 0);
        uint hc6 = (uint)(value6?.GetHashCode() ?? 0);
        uint hc7 = (uint)(value7?.GetHashCode() ?? 0);
        uint hc8 = (uint)(value8?.GetHashCode() ?? 0);

        Initialize(out uint v1, out uint v2, out uint v3, out uint v4);

        v1 = Round(v1, hc1);
        v2 = Round(v2, hc2);
        v3 = Round(v3, hc3);
        v4 = Round(v4, hc4);

        v1 = Round(v1, hc5);
        v2 = Round(v2, hc6);
        v3 = Round(v3, hc7);
        v4 = Round(v4, hc8);

        uint hash = MixState(v1, v2, v3, v4);
        hash += 32;

        hash = MixFinal(hash);
        return (int)hash;
    }

    /// <summary>
    /// 基于字符内容生成 FNV-1a 哈希值.
    /// </summary>
    /// <param name="str">输入字符串.</param>
    /// <returns>32位整数哈希值.</returns>
    public static int StringHash(this string str)
    {
        if (string.IsNullOrEmpty(str)) return 0; // 空字符串使用默认哈希值.

        const uint fnvOffsetBasis = 2166136261;
        const uint fnvPrime = 16777619;

        uint hash = fnvOffsetBasis;

        foreach (char c in str)
        {
            hash ^= (byte)(c >> 8);
            hash *= fnvPrime;
            hash ^= (byte)c;
            hash *= fnvPrime;
        }

        return unchecked((int)hash);
    }

    /// <summary>
    /// 基于字符内容生成 FNV-1a 哈希值.
    /// </summary>
    /// <param name="str">输入字符串.</param>
    /// <returns>32位整数哈希值.</returns>
    public static int StringHash(this ReadOnlySpan<char> str)
    {
        if (str.Length == 0) return 0; // 空字符串使用默认哈希值.

        const uint fnvOffsetBasis = 2166136261;
        const uint fnvPrime = 16777619;

        uint hash = fnvOffsetBasis;

        foreach (char c in str)
        {
            hash ^= (byte)(c >> 8);
            hash *= fnvPrime;
            hash ^= (byte)c;
            hash *= fnvPrime;
        }

        return unchecked((int)hash);
    }

    private static void Initialize(out uint v1, out uint v2, out uint v3, out uint v4)
    {
        v1 = unchecked(Seed + Prime1 + Prime2);
        v2 = Seed + Prime2;
        v3 = Seed;
        v4 = unchecked(Seed - Prime1);
    }

    private static uint Round(uint hash, uint input) => BitOperations.RotateLeft(hash + input * Prime2, 13) * Prime1;

    private static uint QueueRound(uint hash, uint queuedValue) => BitOperations.RotateLeft(hash + queuedValue * Prime3, 17) * Prime4;

    private static uint MixState(uint v1, uint v2, uint v3, uint v4)
        => BitOperations.RotateLeft(v1, 1) + BitOperations.RotateLeft(v2, 7) + BitOperations.RotateLeft(v3, 12) + BitOperations.RotateLeft(v4, 18);

    private static uint MixEmptyState() => Seed + Prime5;

    private static uint MixFinal(uint hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }

    /// <summary>
    /// 提供哈希计算所需的位操作.
    /// </summary>
    private static class BitOperations
    {
        /// <summary>
        /// 将指定值循环左移指定的位数.
        /// </summary>
        /// <param name="value">要循环左移的值.</param>
        /// <param name="offset">要循环左移的位数.</param>
        /// <returns>循环左移后的值.</returns>
        public static uint RotateLeft(uint value, int offset)
            => (value << offset) | (value >> (32 - offset));
    }
}
