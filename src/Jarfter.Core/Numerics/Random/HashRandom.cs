using Jarfter.Core.Numerics.Hash;

namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 提供由哈希链驱动的确定性随机数生成器.
/// <para>每次读取 <see cref="Value"/> 或调用生成方法都会推进内部状态. 相同初始种子会产生相同序列.</para>
/// </summary>
public sealed class HashRandom(int value)
{
    /// <summary>
    /// 获取并推进当前哈希状态.
    /// </summary>
    public int Value { get => field = HashCodes.Combine(field); } = value;

    /// <summary>
    /// 获取指定整数范围内的下一个值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    public int Range(int minInclusive, int maxExclusive) => HashRandomUtil.Range(Value, minInclusive, maxExclusive);

    /// <summary>
    /// 获取指定单精度浮点数范围内的下一个值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    public float Single(float minInclusive, float maxExclusive) => HashRandomUtil.Range(Value, minInclusive, maxExclusive);

    /// <summary>
    /// 获取指定双精度浮点数范围内的下一个值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    public double Double(double minInclusive, double maxExclusive) => HashRandomUtil.Range(Value, minInclusive, maxExclusive);

    /// <summary>
    /// 获取指定整数范围内的下一个值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的值.</returns>
    public int Int32(int minInclusive, int maxExclusive) => HashRandomUtil.Range(Value, minInclusive, maxExclusive);
}
