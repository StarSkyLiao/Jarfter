using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.xUnit.Numerics.Random;

public sealed class RandomPoolTest
{
    [Fact]
    public void RandomPool_WhenEntriesAreAddedAfterConstruction_ShouldUseUpdatedWeightTable()
    {
        RandomPool<int> pool = new([]);

        pool.Add((42, 1));

        Assert.Equal(42, pool.GetRandomly(100));
    }

    [Fact]
    public void RandomPool_WhenCleared_ShouldRejectSelectionFromEmptyPool()
    {
        RandomPool<int> pool = new([(42, 1)]);
        pool.Clear();

        Assert.Throws<InvalidOperationException>(() => pool.GetRandomly());
        Assert.Throws<InvalidOperationException>(() => pool.GetRandomly(100));
    }

    [Fact]
    public void AliasPool_WhenSeedProvided_ShouldSelectDeterministically()
    {
        AliasPool<string> pool = new([("rare", 0.1), ("common", 0.9)]);

        for (int seed = 0; seed < 100; seed++)
        {
            string expected = pool.GetRandomly(seed);
            Assert.Equal(expected, pool.GetRandomly(seed));
        }
    }

    [Fact]
    public void AliasPool_WhenWeightsAreInvalid_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AliasPool<int>([(1, -1)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AliasPool<int>([(1, 0), (2, 0)]));
    }
}
