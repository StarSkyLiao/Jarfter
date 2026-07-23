using Jarfter.Core.Numerics.Random;

namespace Jarfter.Core.xUnit.Numerics.Random;

public sealed class RandomizationTest
{
    [Fact]
    public void SharedRandom_WhenSeedIsReset_ShouldReplaySequence()
    {
        Randomization.SetSeed(2026);
        int first = Randomization.Range(0, 1_000);
        float second = Randomization.Single();

        Randomization.SetSeed(2026);

        Assert.Equal(first, Randomization.Range(0, 1_000));
        Assert.Equal(second, Randomization.Single());
    }

    [Fact]
    public void Shuffle_WhenArrayProvided_ShouldPreserveAllElements()
    {
        int[] values = [1, 2, 3, 4, 5];
        System.Random random = new System.Random(2026);

        values.Shuffle(random);

        Assert.Equal([1, 2, 3, 4, 5], values.Order());
    }

    [Fact]
    public void Select_WhenSequenceIsEmpty_ShouldReturnDefault()
    {
        Assert.Null(Randomization.Select(Array.Empty<string>()));
        Assert.Null(new System.Random(1).Select(Array.Empty<string>()));
    }

    [Fact]
    public void Select_WhenReadOnlyListOrSpanProvided_ShouldReturnContainedElement()
    {
        IReadOnlyList<int> list = [1, 2, 3];
        ReadOnlySpan<int> span = [4, 5, 6];
        System.Random random = new System.Random(2026);

        int selectedFromList = list.Select();
        int selectedFromSpan = span.Select();
        int selectedFromRandomList = random.Select(list);
        int selectedFromRandomSpan = random.Select(span);

        int[] listValues = [1, 2, 3];
        int[] spanValues = [4, 5, 6];
        Assert.Contains(selectedFromList, listValues);
        Assert.Contains(selectedFromSpan, spanValues);
        Assert.Contains(selectedFromRandomList, listValues);
        Assert.Contains(selectedFromRandomSpan, spanValues);
    }
}
