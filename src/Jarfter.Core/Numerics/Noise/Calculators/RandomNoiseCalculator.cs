using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.Numerics.Noise.Calculators;

/// <summary>
/// 使用哈希函数为指定种子和坐标生成确定性噪声值.
/// </summary>
/// <param name="randomSource">预留的随机源; 当前实现不会读取该参数.</param>
public class RandomNoiseCalculator(IRandomSource randomSource) : INoiseCalculator
{
    /// <inheritdoc />
    public double Calculate(int localSeed, (int x, int y) point)
    {
        return HashRandomUtil.Single(localSeed, point.x, point.y);
    }
}
