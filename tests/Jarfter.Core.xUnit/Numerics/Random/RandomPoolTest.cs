using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.xUnit.Numerics.Random;

public sealed class RandomPoolTest
{
    [Fact]
    public void RandomPool_WhenUpdatedAndRemoved_ShouldExposeCurrentEntries()
    {
        RandomPool<string> pool = new RandomPool<string>([
            ("initial", 1)
        ]);

        pool.Update([("first", 1), ("second", 1)]);

        Assert.Equal(2, pool.Count);
        Assert.True(pool.Contains("first"));
        Assert.True(pool.Remove("first"));
        Assert.False(pool.Contains("first"));
        Assert.False(pool.Remove("missing"));
        Assert.Equal("second", pool.GetRandomly(new HashRandom(2026)));
    }

    [Fact]
    public void RandomPool_WhenEmpty_ShouldReturnFalseFromTryGetRandomly()
    {
        RandomPool<int> pool = new RandomPool<int>([
        ]);

        Assert.False(pool.TryGetRandomly(out int value));
        Assert.Equal(0, value);
        Assert.False(pool.TryGetRandomly(new HashRandom(2026), out value));
        Assert.Equal(0, value);
    }

    [Fact]
    public void RandomPool_WhenInvalidWeightAdded_ShouldRejectItWithoutChangingPool()
    {
        RandomPool<int> pool = new([]);

        Assert.Throws<ArgumentOutOfRangeException>(() => pool.Add((1, -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => pool.Add((1, double.NaN)));
        pool.Add((1, 0));

        Assert.Equal(0, pool.Count);
        Assert.False(pool.TryGetRandomly(out int item));
        Assert.Equal(0, item);
    }

    [Fact]
    public void RandomPool_WhenEntriesAreAddedAfterConstruction_ShouldUseUpdatedWeightTable()
    {
        RandomPool<int> pool = new RandomPool<int>([
        ]);

        pool.Add((42, 1));

        Assert.Equal(42, pool.GetRandomly(100));
    }

    [Fact]
    public void RandomPool_WhenCleared_ShouldRejectSelectionFromEmptyPool()
    {
        RandomPool<int> pool = new RandomPool<int>([
            (42, 1)
        ]);
        pool.Clear();

        Assert.Throws<InvalidOperationException>(() => pool.GetRandomly());
        Assert.Throws<InvalidOperationException>(() => pool.GetRandomly(100));
    }

    [Fact]
    public void AliasPool_WhenSeedProvided_ShouldSelectDeterministically()
    {
        AliasPool<string> pool = new AliasPool<string>([
            ("rare", 0.1), ("common", 0.9)
        ]);

        for (int seed = 0; seed < 100; seed++)
        {
            string expected = pool.GetRandomly(seed);
            Assert.Equal(expected, pool.GetRandomly(seed));
        }
    }

    [Fact]
    public void AliasPool_WhenRandomSourceProvided_ShouldUseItsDeterministicSequence()
    {
        AliasPool<string> pool = new AliasPool<string>([
            ("rare", 0.1), ("common", 0.9)
        ]);
        IRandomSource first = new HashRandom(2026);
        IRandomSource second = new HashRandom(2026);

        for (int index = 0; index < 100; index++) Assert.Equal(pool.GetRandomly(first), pool.GetRandomly(second));
    }

    [Fact]
    public void AliasPool_WhenWeightsAreInvalid_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AliasPool<int>([(1, -1)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AliasPool<int>([(1, 0), (2, 0)]));
    }

    [Fact]
    public void AliasPool_WhenUpdateFails_ShouldKeepPreviousSelectionState()
    {
        AliasPool<string> pool = new([("preserved", 1)]);

        Assert.Throws<ArgumentOutOfRangeException>(() => pool.Update([("invalid", -1)]));

        Assert.Equal(1, pool.Count);
        Assert.Equal("preserved", pool.GetRandomly(new HashRandom(2026)));
    }
}
