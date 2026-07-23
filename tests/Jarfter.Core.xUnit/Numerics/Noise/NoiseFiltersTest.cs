using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Noise.Filters;
using Jarfter.Core.Numerics.Noise.Providers;

namespace Jarfter.Core.xUnit.Numerics.Noise;

public sealed class NoiseFiltersTest
{
    [Fact]
    public void ExtremeFilter_WhenValueIsBelowBoundary_ShouldReturnBlack()
    {
        ExtremeFilter filter = new(new DelegateNoiseProvider(static _ => 0.49));

        Assert.Equal(0, filter.ValueAt((0, 0)));
    }

    [Fact]
    public void EdgeFilter_WhenAdjacentValuesCrossBoundary_ShouldReturnBlack()
    {
        EdgeFilter filter = new(new DelegateNoiseProvider(point => point == (1, 0) ? 0.8 : 0.2));

        Assert.Equal(0, filter.ValueAt((0, 0)));
    }

    [Fact]
    public void CellFilter_WhenFewerThanThreeAdjacentValuesCrossBoundary_ShouldKeepOriginalValue()
    {
        CellFilter filter = new(new DelegateNoiseProvider(point =>
            point is (1, 0) or (-1, 0) ? 0.8 : 0.2));

        Assert.Equal(0.2, filter.ValueAt((0, 0)));
    }

    [Fact]
    public void CellFilter_WhenThreeAdjacentValuesCrossBoundary_ShouldInvertOriginalValue()
    {
        CellFilter filter = new(new DelegateNoiseProvider(point =>
            point is (1, 0) or (-1, 0) or (0, 1) ? 0.8 : 0.2));

        Assert.Equal(0.8, filter.ValueAt((0, 0)));
    }

    [Fact]
    public void GaussSample_WhenSourceIsConstant_ShouldPreserveConstantValue()
    {
        GaussSample filter = new(new DelegateNoiseProvider(static _ => 0.6));

        Assert.Equal(0.6, filter.ValueAt((4, -9)), precision: 6);
    }

    [Fact]
    public void ScaleFilter_WhenScaleIsTwo_ShouldSampleSourceAtHalfCoordinate()
    {
        ScaleFilter filter = new(new DelegateNoiseProvider(static point => point.x), 2);

        Assert.Equal(2, filter.ValueAt((4, 0)));
    }

    private sealed class DelegateNoiseProvider(Func<(int x, int y), double> valueFactory) : INoise2DProvider
    {
        public int NoiseSeed => 0;

        public INoiseCalculator Calculator { get; } = new DelegateNoiseCalculator();

        public double ValueAt((int x, int y) localPosition) => valueFactory(localPosition);
    }

    private sealed class DelegateNoiseCalculator : INoiseCalculator
    {
        public double Calculate(int localSeed, (int x, int y) point) => 0;
    }
}
