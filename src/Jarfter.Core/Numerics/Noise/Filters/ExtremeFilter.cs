using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Noise.Providers;

namespace Jarfter.Core.Numerics.Noise.Filters;

/// <summary>
/// 根据阈值 Boundary 将图像坍缩到纯黑或者纯白
/// </summary>
/// <param name="Noise">原始噪声图.</param>
/// <param name="Boundary">区分黑白像素的阈值.
/// </param>
public record ExtremeFilter(INoise2DProvider Noise, double Boundary = 0.5) : INoise2DProvider
{
    /// <inheritdoc />
    public int NoiseSeed => Noise.NoiseSeed;

    /// <inheritdoc />
    public INoiseCalculator Calculator => Noise.Calculator;

    /// <inheritdoc />
    public double ValueAt((int x, int y) position)
    {
        double centerValue = Noise.ValueAt(position);
        return centerValue < Boundary ? 0 : 1;
    }
}
