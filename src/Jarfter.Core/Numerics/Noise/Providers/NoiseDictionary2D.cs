using Jarfter.Core.Numerics.Noise.Calculators;
using Point = (int x, int y);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 噪声地图, 包含了一张无限大小的原始噪声地图.
/// 所有新加入的点都会通过 Dictionary 来缓存下来.
/// </summary>
/// <param name="seed">噪声图的种子.</param>
/// <param name="calculator">未缓存点使用的噪声计算器.</param>
public class NoiseDictionary2D(int seed, INoiseCalculator? calculator = null) : INoise2DProvider
{
    private readonly Dictionary<Point, double> m_NoiseMap = new();
    private readonly INoiseCalculator m_Calculator = calculator ?? HashNoiseCalculator.Instance;

    /// <inheritdoc />
    public int NoiseSeed { get; } = seed;

    /// <inheritdoc />
    public double ValueAt(Point localPosition)
    {
        if (m_NoiseMap.TryGetValue(localPosition, out double cached))
        {
            return cached;
        }
        cached = m_Calculator.Calculate(NoiseSeed, localPosition);
        m_NoiseMap[localPosition] = cached;
        return cached;
    }

}
