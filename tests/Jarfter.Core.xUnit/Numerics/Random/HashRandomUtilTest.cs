using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.xUnit.Numerics.Random;

public sealed class HashRandomUtilTest
{
    [Fact]
    public void RandomValues_WhenSameSeedProvided_ShouldBeDeterministicAndWithinExpectedRanges()
    {
        for (int seed = 0; seed < 1_000; seed++)
        {
            float single = HashRandomUtil.Single(seed);
            double doubleValue = HashRandomUtil.Double(seed);
            float rangedSingle = HashRandomUtil.Range(seed, 10f, 20f);
            double rangedDouble = HashRandomUtil.Range(seed, -20d, -10d);

            Assert.Equal(single, HashRandomUtil.Single(seed));
            Assert.Equal(doubleValue, HashRandomUtil.Double(seed));
            Assert.InRange(single, 0f, 1f);
            Assert.True(single < 1f);
            Assert.InRange(doubleValue, 0d, 1d);
            Assert.True(doubleValue < 1d);
            Assert.InRange(rangedSingle, 10f, 20f);
            Assert.True(rangedSingle < 20f);
            Assert.InRange(rangedDouble, -20d, -10d);
            Assert.True(rangedDouble < -10d);
            Assert.InRange(HashRandomUtil.Range(seed, 10, 20), 10, 19);
        }
    }

    [Fact]
    public void Select_WhenSequenceIsEmpty_ShouldReturnDefault()
    {
        Assert.Null(Array.Empty<string>().Select(123));

        int selected = new[] { 1, 2, 3 }.Select(123);
        int[] expectedValues = [1, 2, 3];
        Assert.Contains(selected, expectedValues);
    }

    [Fact]
    public void HashRandom_WhenSameSeedProvided_ShouldGenerateSameBoundedSequence()
    {
        IRandomSource first = new HashRandom(100);
        IRandomSource second = new HashRandom(100);

        for (int index = 0; index < 100; index++)
        {
            Assert.Equal(first.NextInt32(10, 20), second.NextInt32(10, 20));

            int firstRange = first.Range(-5, 5);
            int secondRange = second.Range(-5, 5);
            Assert.Equal(firstRange, secondRange);
            Assert.InRange(firstRange, -5, 4);
        }
    }

    [Fact]
    public void HashRandom_WhenResetAndForked_ShouldReplayAndIsolateSequences()
    {
        HashRandom random = new HashRandom(2026);
        int initialState = random.State;
        int firstValue = random.NextInt32(-100, 100);

        random.Reset(initialState);

        Assert.Equal(firstValue, random.NextInt32(-100, 100));

        int stateBeforeFork = random.State;
        HashRandom firstFork = random.Fork(42);
        HashRandom secondFork = random.Fork(42);

        Assert.Equal(stateBeforeFork, random.State);
        for (int index = 0; index < 100; index++)
            Assert.Equal(firstFork.NextInt64(long.MinValue, long.MaxValue), secondFork.NextInt64(long.MinValue, long.MaxValue));
    }
}
