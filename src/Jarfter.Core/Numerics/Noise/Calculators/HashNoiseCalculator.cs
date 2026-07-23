using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.Numerics.Noise.Calculators;

/// <summary>
/// 使用哈希函数为指定种子和坐标生成确定性噪声值.
/// </summary>
public class HashNoiseCalculator : INoiseCalculator
{
    /// <inheritdoc />
    public double Calculate(int localSeed, (int x, int y) point)
    {
        return HashRandomUtil.Single(localSeed, point.x, point.y);
    }
}
