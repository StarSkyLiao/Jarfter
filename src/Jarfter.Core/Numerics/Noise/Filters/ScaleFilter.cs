using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Noise.Providers;

namespace Jarfter.Core.Numerics.Noise.Filters;

/// <summary>
/// 缩放噪声图.
/// 新的噪声图会将原噪声图缩放为 scale 倍.
/// </summary>
/// <param name="Noise">原始噪声图.</param>
/// <param name="Scale">缩放倍数.</param>
public record ScaleFilter(INoise2DProvider Noise, double Scale = 1) : INoise2DProvider
{
    private NoiseMap2D NoiseCache { get; } = new NoiseMap2D(Noise.NoiseSeed, new InternalCalculator(
        Noise, Scale > 0 ? 1 / Scale : throw new ArgumentOutOfRangeException(nameof(Scale))
    ));

    /// <inheritdoc />
    public int NoiseSeed => Noise.NoiseSeed;

    /// <inheritdoc />
    public INoiseCalculator Calculator => Noise.Calculator;

    /// <inheritdoc />
    public double ValueAt((int x, int y) position) => NoiseCache.ValueAt(position);

    private record InternalCalculator(INoise2DProvider Noise, double ScaleRate = 1) : INoiseCalculator
    {
        public double Calculate(int localSeed, (int x, int y) point)
        {
            return Noise.ValueAt((point.x * ScaleRate, point.y * ScaleRate));
        }
    }

}
