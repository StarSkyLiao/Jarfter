using Jarfter.Core.Numerics.Noise.Calculators;
using Point = (int x, int y);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 表示不缓存采样结果的二维噪声提供器.
/// 每次采样都会直接调用计算器, 适合计算成本较低或需要避免缓存增长的场景.
/// </summary>
/// <param name="seed">噪声图的种子.</param>
/// <param name="calculator">用于计算噪声值的计算器.</param>
public sealed class NoiseDirect2D(int seed, INoiseCalculator? calculator = null) : INoise2DProvider
{
    /// <inheritdoc />
    public int NoiseSeed { get; } = seed;

    /// <inheritdoc />
    public INoiseCalculator Calculator { get; } = calculator ?? HashNoiseCalculator.Instance;

    /// <inheritdoc />
    public double ValueAt(Point localPosition) => Calculator.Calculate(NoiseSeed, localPosition);
}
