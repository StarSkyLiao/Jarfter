using Jarfter.Core.Numerics.Noise.Calculators;
using Point = (int x, int y);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 噪声地图, 包含了一张有限大小的原始噪声地图.
/// 所有新加入的点都会通过 Array 来缓存下来.
/// 访问超出范围的坐标时会引发异常.
/// </summary>
/// <param name="seed">噪声图的种子.</param>
/// <param name="size">噪声图的宽度和高度.</param>
/// <param name="start">噪声图在全局坐标中的起始位置.</param>
/// <param name="calculator">未缓存点使用的噪声计算器.</param>
public class NoiseChunk2D(int seed, Point size, Point start = default, INoiseCalculator? calculator = null)
    : INoise2DProvider
{
    private readonly double[] m_NoiseMap = new double[size.x * size.y];
    private readonly bool[] m_IsCached = new bool[size.x * size.y];

    /// <inheritdoc />
    public int NoiseSeed { get; } = seed;

    /// <inheritdoc />
    public INoiseCalculator Calculator { get; } = calculator ?? new HashNoiseCalculator();

    /// <inheritdoc />
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="localPosition"/> 超出区块范围时引发.</exception>
    public double ValueAt(Point localPosition)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)localPosition.x, (uint)size.x);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)localPosition.y, (uint)size.y);

        // 数组按 X 轴分组、Y 轴连续存储, 因此每组的步长必须是高度.
        int index = localPosition.x * size.y + localPosition.y;
        if (m_IsCached[index]) return m_NoiseMap[index];

        double cached = Calculator.Calculate(NoiseSeed,
            (localPosition.x + start.x, localPosition.y + start.y)
        );
        m_NoiseMap[index] = cached;
        m_IsCached[index] = true;
        return cached;
    }
}
