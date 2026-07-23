using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Noise.Providers;

namespace Jarfter.Core.Numerics.Noise.Filters;

/// <summary>
/// 将边缘保留为黑色, 否则为白色.
/// </summary>
/// <param name="Noise">原始噪声图.</param>
public record EdgeFilter(INoise2DProvider Noise) : INoise2DProvider
{
    private const double Boundary = 0.5;
    private static readonly (int dx, int dy)[] s_SamplePoints =
    [
        (-1, 0), (1, 0), (0, -1), (0, 1)
    ];

    /// <inheritdoc />
    public int NoiseSeed => Noise.NoiseSeed;

    /// <inheritdoc />
    public INoiseCalculator Calculator => Noise.Calculator;

    /// <inheritdoc />
    public double ValueAt((int x, int y) position)
    {
        double centerValue = Noise.ValueAt(position);
        if (centerValue < Boundary)
        {
            foreach ((int dx, int dy) item in s_SamplePoints)
            {
                double value = Noise.ValueAt((item.dx + position.x, item.dy + position.y));
                if (value >= Boundary) return 0;
            }
        }
        else
        {
            foreach ((int dx, int dy) item in s_SamplePoints)
            {
                double value = Noise.ValueAt((item.dx + position.x, item.dy + position.y));
                if (value < Boundary) return 0;
            }
        }
        return 1;
    }
}
