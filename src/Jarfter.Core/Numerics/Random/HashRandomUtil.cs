using Jarfter.Core.Numerics.Hash;

namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 提供基于输入哈希值的无状态确定性随机运算.
/// <para>相同输入始终产生相同结果. 这类方法不保存状态, 适合需要可重现结果的场景.</para>
/// </summary>
public static class HashRandomUtil
{
    private const float SingleUnit = 1f / (1 << 24);
    private const double DoubleUnit = 1d / (1L << 32);

    /// <summary>
    /// 根据种子获取指定整数范围内的确定性值.
    /// </summary>
    /// <param name="seed">用于生成随机值的种子.</param>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    public static int Range<T>(T seed, int minInclusive, int maxExclusive)
        => HashCodes.Combine(seed).CircleClamp(minInclusive, maxExclusive);

    /// <summary>
    /// 根据种子获取指定单精度浮点数范围内的确定性值.
    /// </summary>
    /// <param name="seed">用于生成随机值的种子.</param>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    public static float Range<T>(T seed, float minInclusive, float maxExclusive)
        => minInclusive + Single(seed) * (maxExclusive - minInclusive);

    /// <summary>
    /// 根据种子获取指定双精度浮点数范围内的确定性值.
    /// </summary>
    /// <param name="seed">用于生成随机值的种子.</param>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    public static double Range<T>(T seed, double minInclusive, double maxExclusive)
        => minInclusive + Double(seed) * (maxExclusive - minInclusive);

    /// <summary>
    /// 根据一个种子获取确定性的 <see cref="int"/> 值.
    /// </summary>
    /// <param name="seed">用于生成随机值的种子.</param>
    /// <returns>确定性的 <see cref="int"/> 值.</returns>
    public static int Int32<T>(T seed) => HashCodes.Combine(seed);

    /// <summary>
    /// 根据两个种子获取确定性的 <see cref="int"/> 值.
    /// </summary>
    /// <param name="seed1">第一个种子.</param>
    /// <param name="seed2">第二个种子.</param>
    /// <returns>确定性的 <see cref="int"/> 值.</returns>
    public static int Int32<T>(T seed1, T seed2) => HashCodes.Combine(seed1, seed2);

    /// <summary>
    /// 根据三个种子获取确定性的 <see cref="int"/> 值.
    /// </summary>
    /// <param name="seed1">第一个种子.</param>
    /// <param name="seed2">第二个种子.</param>
    /// <param name="seed3">第三个种子.</param>
    /// <returns>确定性的 <see cref="int"/> 值.</returns>
    public static int Int32<T>(T seed1, T seed2, T seed3) => HashCodes.Combine(seed1, seed2, seed3);

    /// <summary>
    /// 根据一个种子获取位于 [0, 1) 的确定性单精度浮点数.
    /// </summary>
    /// <param name="seed">用于生成随机值的种子.</param>
    /// <returns>位于 [0, 1) 的确定性单精度浮点数.</returns>
    public static float Single<T>(T seed) => ToSingle(HashCodes.Combine(seed));

    /// <summary>
    /// 根据两个种子获取位于 [0, 1) 的确定性单精度浮点数.
    /// </summary>
    /// <param name="seed1">第一个种子.</param>
    /// <param name="seed2">第二个种子.</param>
    /// <returns>位于 [0, 1) 的确定性单精度浮点数.</returns>
    public static float Single<T>(T seed1, T seed2) => ToSingle(HashCodes.Combine(seed1, seed2));

    /// <summary>
    /// 根据三个种子获取位于 [0, 1) 的确定性单精度浮点数.
    /// </summary>
    /// <param name="seed1">第一个种子.</param>
    /// <param name="seed2">第二个种子.</param>
    /// <param name="seed3">第三个种子.</param>
    /// <returns>位于 [0, 1) 的确定性单精度浮点数.</returns>
    public static float Single<T>(T seed1, T seed2, T seed3) => ToSingle(HashCodes.Combine(seed1, seed2, seed3));

    /// <summary>
    /// 根据一个种子获取位于 [0, 1) 的确定性双精度浮点数.
    /// </summary>
    /// <param name="seed">用于生成随机值的种子.</param>
    /// <returns>位于 [0, 1) 的确定性双精度浮点数.</returns>
    public static double Double<T>(T seed) => ToDouble(HashCodes.Combine(seed));

    /// <summary>
    /// 根据两个种子获取位于 [0, 1) 的确定性双精度浮点数.
    /// </summary>
    /// <param name="seed1">第一个种子.</param>
    /// <param name="seed2">第二个种子.</param>
    /// <returns>位于 [0, 1) 的确定性双精度浮点数.</returns>
    public static double Double<T>(T seed1, T seed2) => ToDouble(HashCodes.Combine(seed1, seed2));

    /// <summary>
    /// 根据三个种子获取位于 [0, 1) 的确定性双精度浮点数.
    /// </summary>
    /// <param name="seed1">第一个种子.</param>
    /// <param name="seed2">第二个种子.</param>
    /// <param name="seed3">第三个种子.</param>
    /// <returns>位于 [0, 1) 的确定性双精度浮点数.</returns>
    public static double Double<T>(T seed1, T seed2, T seed3) => ToDouble(HashCodes.Combine(seed1, seed2, seed3));

    /// <summary>
    /// 根据种子从序列中确定性地选取一个元素.
    /// </summary>
    /// <param name="self">要选取元素的序列.</param>
    /// <param name="seed">用于选取元素的种子.</param>
    /// <returns>选取到的元素; 当序列为空时返回默认值.</returns>
    public static TOut? Select<TOut, TIn>(this IEnumerable<TOut> self, TIn seed)
    {
        ArgumentNullException.ThrowIfNull(self);

        IList<TOut> entries = self as IList<TOut> ?? [.. self];
        return entries.Count == 0 ? default : entries[Range(seed, 0, entries.Count)];
    }

    private static float ToSingle(int hash) => ((uint)hash >> 8) * SingleUnit;

    private static double ToDouble(int hash) => (uint)hash * DoubleUnit;
}
