using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Noise.Providers;

namespace Jarfter.Core.Numerics.Noise.Filters;

/// <summary>
/// 为给定的噪声图降噪.
/// 输入值优先预先变为纯黑或者纯白色;
/// 根据像素点四周点的颜色与阈值的关系,
/// 如果至少有三个点与该点不相同, 则该点反色.
/// </summary>
/// <param name="Noise">原始噪声图.</param>
/// <param name="Boundary">区分黑白像素的阈值.</param>
public record CellFilter(INoise2DProvider Noise, double Boundary = 0.5) : INoise2DProvider
{
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
        int count = 0;
        if (centerValue < Boundary)
        {
            foreach ((int dx, int dy) item in s_SamplePoints)
            {
                double value = Noise.ValueAt((item.dx + position.x, item.dy + position.y));
                if (value >= Boundary) count++;
                if (count > 1) return 1 - centerValue;
            }
        }
        else
        {
            foreach ((int dx, int dy) item in s_SamplePoints)
            {
                double value = Noise.ValueAt((item.dx + position.x, item.dy + position.y));
                if (value < Boundary) count++;
                if (count > 1) return 1 - centerValue;
            }
        }
        return centerValue;
    }
}
