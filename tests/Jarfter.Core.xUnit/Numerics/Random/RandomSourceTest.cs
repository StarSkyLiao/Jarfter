using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.xUnit.Numerics.Random;

public sealed class RandomSourceTest
{
    [Fact]
    public void Range_WhenUnitValueRoundsToUpperBound_ShouldKeepUpperBoundExclusive()
    {
        IRandomSource source = new UpperBoundRandomSource();

        float single = source.NextSingle(10f, 20f);
        double doubleValue = source.NextDouble(10d, 20d);

        Assert.Equal(MathF.BitDecrement(20f), single);
        Assert.Equal(Math.BitDecrement(20d), doubleValue);
        Assert.True(single < 20f);
        Assert.True(doubleValue < 20d);
        Assert.Throws<ArgumentOutOfRangeException>(() => source.NextSingle(20f, 10f));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.NextDouble(20d, 10d));
    }

    [Fact]
    public void SystemRandomSource_WhenWrappingSameSeed_ShouldReplayBclSequence()
    {
        SystemRandomSource source = new SystemRandomSource(new System.Random(2026));
        System.Random expected = new System.Random(2026);

        Assert.Equal(expected.Next(10, 100), source.NextInt32(10, 100));
        Assert.Equal(expected.NextInt64(-1_000, 1_000), source.NextInt64(-1_000, 1_000));
        Assert.Equal(expected.NextSingle(), source.NextSingle());
        Assert.Equal(expected.NextDouble(), source.NextDouble());
    }

    [Fact]
    public void RandomSources_WhenUsedThroughInterface_ShouldHonorCommonContract()
    {
        Mt19937 mt19937 = new Mt19937();
        mt19937.Seed(2026);

        IRandomSource[] randomSources =
        [
            new SystemRandomSource(new System.Random(2026)),
            new HashRandom(2026),
            mt19937,
        ];

        foreach (IRandomSource randomSource in randomSources)
        {
            for (int index = 0; index < 100; index++)
            {
                Assert.InRange(randomSource.NextInt32(-100, 100), -100, 99);
                Assert.InRange(randomSource.NextInt64(-1_000, 1_000), -1_000, 999);
                Assert.InRange(randomSource.NextSingle(), 0f, 1f);
                Assert.True(randomSource.NextSingle() < 1f);
                Assert.InRange(randomSource.NextSingle(10f, 20f), 10f, 20f);
                Assert.True(randomSource.NextSingle(10f, 20f) < 20f);
                Assert.InRange(randomSource.NextDouble(), 0d, 1d);
                Assert.True(randomSource.NextDouble() < 1d);
                Assert.InRange(randomSource.NextDouble(-20d, -10d), -20d, -10d);
                Assert.True(randomSource.NextDouble(-20d, -10d) < -10d);
            }
        }
    }

    [Fact]
    public void SelectAndShuffle_WhenUsingRandomSource_ShouldOperateOnListsAndSpans()
    {
        IRandomSource source = new HashRandom(2026);
        IReadOnlyList<int> list = [1, 2, 3];
        ReadOnlySpan<int> readOnlySpan = [4, 5, 6];
        Span<int> span = [1, 2, 3, 4, 5];

        int selectedFromList = source.Select(list);
        int selectedFromSpan = source.Select(readOnlySpan);
        span.Shuffle(source);

        int[] listValues = [1, 2, 3];
        int[] spanValues = [4, 5, 6];
        int[] shuffledValues = [1, 2, 3, 4, 5];
        Assert.Contains(selectedFromList, listValues);
        Assert.Contains(selectedFromSpan, spanValues);
        Assert.Equal(shuffledValues, span.ToArray().Order());
    }

    private sealed class UpperBoundRandomSource : IRandomSource
    {
        public int NextInt32(int minInclusive, int maxExclusive) => minInclusive;

        public long NextInt64(long minInclusive, long maxExclusive) => minInclusive;

        public float NextSingle() => MathF.BitDecrement(1f);

        public double NextDouble() => Math.BitDecrement(1d);
    }
}
