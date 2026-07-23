using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.xUnit.Numerics.Random;

public sealed class Mt19937Test
{
    [Fact]
    public void UInt64_WhenDefaultSeedUsed_ShouldMatchReferenceSequence()
    {
        Mt19937 random = new Mt19937();

        Assert.Equal(14_514_284_786_278_117_030UL, random.UInt64());
    }

    [Fact]
    public void UInt64_WhenSameSeedProvided_ShouldGenerateSameSequence()
    {
        Mt19937 first = new Mt19937();
        Mt19937 second = new Mt19937();
        first.Seed(12345);
        second.Seed(12345);

        for (int index = 0; index < 1_000; index++) Assert.Equal(first.UInt64(), second.UInt64());
    }

    [Fact]
    public void RealMethods_WhenGenerated_ShouldHonorDocumentedIntervals()
    {
        Mt19937 random = new Mt19937();
        random.Seed(54321);

        for (int index = 0; index < 1_000; index++)
        {
            Assert.InRange(random.Real1(), 0d, 1d);
            Assert.InRange(random.Real2(), 0d, 1d);
            Assert.True(random.Real2() < 1d);
            Assert.True(random.Real3() > 0d);
            Assert.True(random.Real3() < 1d);
            Assert.True(random.Int63() >= 0);
        }
    }

    [Fact]
    public void Seed_WhenKeyArrayIsEmpty_ShouldThrow()
    {
        Mt19937 random = new Mt19937();

        Assert.Throws<ArgumentException>(() => random.Seed([]));
    }
}
