using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.Numerics.Noise.Calculators;

/// <summary>
/// 使用哈希函数为指定种子和坐标生成确定性噪声值.
/// </summary>
public sealed class HashNoiseCalculator : INoiseCalculator
{
    /// <summary>
    /// 提供一个 HashNoiseCalculator 的静态单例.
    /// </summary>
    public static readonly HashNoiseCalculator Instance = new HashNoiseCalculator();

    /// <inheritdoc />
    public double Calculate(int localSeed, (int x, int y) point)
    {
        return HashRandomUtil.Single(localSeed, point.x, point.y);
    }
}
