using Point = (int x, int y);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 将带种子的二维噪声委托适配为噪声提供器.
/// 该类型仅供程序集内部的测试和适配场景使用.
/// </summary>
/// <param name="seed">噪声图的种子.</param>
/// <param name="valueFactory">根据种子和坐标计算噪声值的委托.</param>
internal sealed class NoiseDelegate2D(int seed, Func<int, Point, double> valueFactory) : INoise2DProvider
{
    private readonly Func<int, Point, double> m_ValueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));

    /// <inheritdoc />
    public int NoiseSeed { get; } = seed;

    /// <inheritdoc />
    public double ValueAt(Point localPosition) => m_ValueFactory(NoiseSeed, localPosition);
}
