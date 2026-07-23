//------------------------------------------------------------
// MiHoMiao
// Written by Mingxuan Liao.
// [Version] 1.0
//------------------------------------------------------------

using Jarfter.Core.Numerics.Noise.Calculators;
using Point = (int x, int y);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 噪声地图, 包含了一张无限大小的原始噪声地图.
/// 所有新加入的点都会通过一个 Dictionary ,
/// 映射到一个<see cref="NoiseChunk2D"/>区块中,
/// 通过在区块中缓存噪声结果以降低 Dictionary 的负载.
/// </summary>
/// <param name="seed">噪声图的种子.</param>
/// <param name="calculator">未缓存点使用的噪声计算器.</param>
public class NoiseMap2D(int seed, INoiseCalculator? calculator = null) : INoise2DProvider
{
    private const int ChunkSize = 16;
    private readonly Dictionary<Point, NoiseChunk2D> m_NoiseMap = new();

    /// <inheritdoc />
    public int NoiseSeed { get; } = seed;

    /// <inheritdoc />
    public INoiseCalculator Calculator { get; } = calculator ?? new HashNoiseCalculator();

    /// <inheritdoc />
    public double ValueAt(Point localPosition)
    {
        Point chunkPoint =
        (
            (int)Math.Floor((double)localPosition.x / ChunkSize),
            (int)Math.Floor((double)localPosition.y / ChunkSize)
        );
        int deltaX = localPosition.x % ChunkSize;
        int deltaY = localPosition.y % ChunkSize;
        Point localPoint =
        (
            deltaX < 0 ? deltaX + ChunkSize : deltaX,
            deltaY < 0 ? deltaY + ChunkSize : deltaY
        );

        NoiseChunk2D? cached = m_NoiseMap.GetValueOrDefault(chunkPoint);
        if (cached is not null) return cached.ValueAt(localPoint);

        cached = new NoiseChunk2D(NoiseSeed,
            (ChunkSize, ChunkSize),
            (chunkPoint.x * ChunkSize, chunkPoint.y * ChunkSize),
            Calculator
        );
        m_NoiseMap[chunkPoint] = cached;
        return cached.ValueAt(localPoint);
    }
}
