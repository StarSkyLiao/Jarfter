using System.Runtime.CompilerServices;
using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Random;
using Point = (int x, int y);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 噪声地图, 包含了一张有限大小的原始噪声地图.
/// 所有新加入的点都会通过 Array 来缓存下来.
/// 当访问超出了范围的点时，将会返回 -1 .
/// </summary>
/// <param name="seed">噪声图的种子.</param>
/// <param name="size">噪声图的宽度和高度.</param>
/// <param name="start">噪声图在全局坐标中的起始位置.</param>
/// <param name="calculator">未缓存点使用的噪声计算器.</param>
public class NoiseChunk2D(int seed, Point size, Point start = default, INoiseCalculator? calculator = null)
    : INoise2DProvider
{
    private readonly float[] m_NoiseMap = InitArray(size.x * size.y);

    /// <inheritdoc />
    public int NoiseSeed { get; } = seed;

    /// <inheritdoc />
    public INoiseCalculator Calculator { get; } = calculator ?? new RandomNoiseCalculator(new HashRandom(0));

    /// <inheritdoc />
    public double ValueAt(Point localPosition)
    {
        if (localPosition.x < 0 || localPosition.x >= size.x) return -1;
        if (localPosition.y < 0 || localPosition.y >= size.y) return -1;
        float cached = m_NoiseMap[localPosition.x * size.x + localPosition.y];
        if (cached >= 0) return cached;
        cached = (float)Calculator.Calculate(NoiseSeed,
            (localPosition.x + start.x, localPosition.y + start.y)
        );
        m_NoiseMap[localPosition.x * size.x + localPosition.y] = cached;
        return cached;
    }

    private static float[] InitArray(int size)
    {
        float[] noiseMap = new float[size];
        Array.Fill(noiseMap, -1);
        return noiseMap;
    }

}
