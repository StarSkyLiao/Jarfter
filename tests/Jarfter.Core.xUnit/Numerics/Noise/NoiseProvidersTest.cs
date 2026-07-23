using Jarfter.Core.Numerics.Noise.Calculators;
using Jarfter.Core.Numerics.Noise.Providers;

namespace Jarfter.Core.xUnit.Numerics.Noise;

public sealed class NoiseProvidersTest
{
    [Fact]
    public void RandomNoiseCalculator_WhenSameSeedAndCoordinateProvided_ShouldReturnDeterministicValue()
    {
        HashNoiseCalculator calculator = new();

        double first = calculator.Calculate(2026, (-3, 9));
        double second = calculator.Calculate(2026, (-3, 9));

        Assert.Equal(first, second);
        Assert.InRange(first, 0, 1);
        Assert.True(first < 1);
    }

    [Fact]
    public void NoiseDictionary2D_WhenCoordinateRequestedRepeatedly_ShouldCacheCalculatedValue()
    {
        CountingNoiseCalculator calculator = new();
        NoiseDictionary2D noise = new(2026, calculator);

        double first = noise.ValueAt((3, -2));
        double second = noise.ValueAt((3, -2));

        Assert.Equal(298, first);
        Assert.Equal(first, second);
        Assert.Equal(1, calculator.CallCount);
    }

    [Fact]
    public void NoiseMap2D_WhenNegativeCoordinateRequested_ShouldPassOriginalCoordinateToCalculator()
    {
        CountingNoiseCalculator calculator = new();
        NoiseMap2D noise = new(2026, calculator);

        double value = noise.ValueAt((-1, -17));

        Assert.Equal(-117, value);
        Assert.Equal(1, calculator.CallCount);
    }

    [Fact]
    public void NoiseChunk2D_WhenSizeIsRectangular_ShouldKeepCoordinatesInSeparateCacheSlots()
    {
        CountingNoiseCalculator calculator = new();
        NoiseChunk2D noise = new(2026, (2, 3), calculator: calculator);

        double first = noise.ValueAt((0, 2));
        double second = noise.ValueAt((1, 0));

        Assert.Equal(2, first);
        Assert.Equal(100, second);
        Assert.Equal(2, calculator.CallCount);
    }

    [Fact]
    public void NoiseTree2D_WhenNegativeCoordinateRequested_ShouldUseDistinctCoordinateCacheEntry()
    {
        CountingNoiseCalculator calculator = new();
        NoiseTree2D noise = new(2026, calculator);

        double origin = noise.ValueAt((0, 0));
        double negative = noise.ValueAt((-1, 0));

        Assert.Equal(0, origin);
        Assert.Equal(-100, negative);
        Assert.Equal(2, calculator.CallCount);
    }

    [Fact]
    public void INoise2DProvider_WhenFractionalCoordinateRequested_ShouldBilinearlyInterpolateAdjacentValues()
    {
        INoise2DProvider noise = new CoordinateNoiseProvider();

        double value = noise.ValueAt((0.25d, 0.5d));

        Assert.Equal(25.5, value);
    }

    private sealed class CountingNoiseCalculator : INoiseCalculator
    {
        public int CallCount { get; private set; }

        public double Calculate(int localSeed, (int x, int y) point)
        {
            CallCount++;
            return point.x * 100 + point.y;
        }
    }

    private sealed class CoordinateNoiseProvider : INoise2DProvider
    {
        public int NoiseSeed => 0;

        public INoiseCalculator Calculator { get; } = new CountingNoiseCalculator();

        public double ValueAt((int x, int y) localPosition) => localPosition.x * 100 + localPosition.y;
    }
}
