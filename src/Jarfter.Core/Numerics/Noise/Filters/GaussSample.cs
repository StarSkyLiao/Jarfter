using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Noise.Providers;

namespace Jarfter.Core.Numerics.Noise.Filters;

/// <summary>
/// 用于平滑噪声图.
/// 在原始噪声的基础上,
/// 通过结合 sampleRadius 范围内其他原始噪声点的数值进行平滑.
/// </summary>
/// <param name="Noise">原始的噪声图</param>
/// <param name="Contrast">对比度</param>
public record GaussSample(INoise2DProvider Noise, float Contrast = 1.0f) : INoise2DProvider
{
    /// <inheritdoc />
    public int NoiseSeed => Noise.NoiseSeed;

    /// <inheritdoc />
    public INoiseCalculator Calculator => Noise.Calculator;

    /// <inheritdoc />
    public double ValueAt((int x, int y) position) => NoiseCache.ValueAt(position);

    private NoiseMap2D NoiseCache { get; } = new NoiseMap2D(
        Noise.NoiseSeed, new InternalCalculator(Noise, Contrast)
    );

    private record InternalCalculator(INoise2DProvider Noise, float Contrast) : INoiseCalculator
    {
        private static readonly List<(int dx, int dy, float weight)> s_SamplePoints = GetSamplePoints();

        public double Calculate(int localSeed, (int x, int y) point)
        {
            double cached = 0;
            double length = 0;

            foreach ((int dx, int dy, float weight) item in s_SamplePoints)
            {
                (int, int) temp = (item.dx + point.x, item.dy + point.y);
                cached += Noise.ValueAt(temp) * item.weight;
                length += item.weight;
            }

            cached /= length;
            cached = (Contrast * Math.Sqrt(length) * (cached - 0.5f) + 0.5f).Clamp01();

            return cached;
        }

        private static readonly float[,] s_GaussFilter =
        {
            {
                8, 4, 2
            },
            {
                4, 3, 1
            },
            {
                2, 1, 0
            },
        };

        private static List<(int dx, int dy, float weight)> GetSamplePoints()
        {
            List<(int dx, int dy, float weight)> samplePoints = new(9);
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    float weight = s_GaussFilter[Math.Abs(dx), Math.Abs(dy)];
                    if (weight == 0) continue;
                    samplePoints.Add((dx, dy, weight));
                }
            }
            return samplePoints;
        }

    }
}
