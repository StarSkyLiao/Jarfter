namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 定义可替换的随机数来源.
/// <para>实现必须为同一实例维护独立状态, 并保证浮点数方法返回值位于 [0, 1). 该接口不保证线程安全.</para>
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// 获取指定整数范围内的下一个值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    int Range(int minInclusive, int maxExclusive) => NextInt32(minInclusive, maxExclusive);

    /// <summary>
    /// 获取指定整数范围内的下一个随机值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的随机整数.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minInclusive"/> 不小于 <paramref name="maxExclusive"/>.</exception>
    int NextInt32(int minInclusive, int maxExclusive);

    /// <summary>
    /// 获取指定 64 位整数范围内的下一个随机值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的随机 64 位整数.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minInclusive"/> 不小于 <paramref name="maxExclusive"/>.</exception>
    long NextInt64(long minInclusive, long maxExclusive);

    /// <summary>
    /// 获取位于 [0, 1) 的下一个随机单精度浮点数.
    /// </summary>
    /// <returns>位于 [0, 1) 的随机单精度浮点数.</returns>
    float NextSingle();

    /// <summary>
    /// 获取指定单精度浮点数范围内的下一个随机值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的随机单精度浮点数.</returns>
    /// <exception cref="ArgumentOutOfRangeException">边界不是有限值, 或 <paramref name="minInclusive"/> 不小于 <paramref name="maxExclusive"/>.</exception>
    float NextSingle(float minInclusive, float maxExclusive)
    {
        if (!float.IsFinite(minInclusive) || !float.IsFinite(maxExclusive) || minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "上限必须是大于下限的有限值.");

        // 使用 double 中间值避免单精度范围计算溢出, 并在舍入到上界时保持上界不包含的契约.
        float value = (float)((double)minInclusive + NextSingle() * ((double)maxExclusive - minInclusive));
        return value < maxExclusive ? value : MathF.BitDecrement(maxExclusive);
    }

    /// <summary>
    /// 获取位于 [0, 1) 的下一个随机双精度浮点数.
    /// </summary>
    /// <returns>位于 [0, 1) 的随机双精度浮点数.</returns>
    double NextDouble();

    /// <summary>
    /// 获取指定双精度浮点数范围内的下一个随机值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的随机双精度浮点数.</returns>
    /// <exception cref="ArgumentOutOfRangeException">边界不是有限值, 或 <paramref name="minInclusive"/> 不小于 <paramref name="maxExclusive"/>.</exception>
    double NextDouble(double minInclusive, double maxExclusive)
    {
        if (!double.IsFinite(minInclusive) || !double.IsFinite(maxExclusive) || minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), "上限必须是大于下限的有限值.");

        // 使用加权和避免极大且异号的边界相减时溢出.
        double unit = NextDouble();
        double value = minInclusive * (1d - unit) + maxExclusive * unit;
        return value < maxExclusive ? value : Math.BitDecrement(maxExclusive);
    }
}
