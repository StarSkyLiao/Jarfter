using Jarfter.Core.Collections.Generic;

namespace Jarfter.Core.xUnit.Collections.Generic;

public sealed class UnorderedArrayTest
{
    [Fact]
    public void Create_WithCapacity_ShouldRespectInitialState()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>(capacity: 8);

        Assert.Empty(array);
        Assert.True(array.IsEmpty);
        Assert.Equal(8, array.Capacity);
        Assert.Empty(array.AsSpan().ToArray());
    }

    [Fact]
    public void AddAndRemoveAt_ShouldUseTailOverwriteSemantics()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>();
        array.AddRange([1, 2, 3, 4]);

        array.RemoveAt(1);

        Assert.Equal(3, array.Count);
        Assert.Equal([1, 4, 3], array.AsSpan().ToArray());
    }

    [Fact]
    public void Remove_WithDuplicates_ShouldRemoveSingleMatch()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>();
        array.AddRange([1, 2, 1, 3]);

        bool removed = array.Remove(1);

        Assert.True(removed);
        Assert.Equal(3, array.Count);
        Assert.True(array.Contains(1));
        int duplicateCount = 0;
        foreach (int value in array.AsSpan())
        {
            if (value == 1) duplicateCount++;
        }

        Assert.Equal(1, duplicateCount);
    }

    [Fact]
    public void Indexer_Set_ShouldUpdateValue()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>();
        array.AddRange([10, 20, 30]);

        array[1] = 99;

        Assert.Equal(99, array[1]);
        Assert.False(array.Contains(20));
        Assert.True(array.Contains(99));
    }

    [Fact]
    public void TryRemoveAt_WhenOutOfRange_ShouldReturnFalse()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>();
        array.AddRange([1, 2, 3]);

        Assert.False(array.TryRemoveAt(-1));
        Assert.False(array.TryRemoveAt(3));
        Assert.Equal([1, 2, 3], array.AsSpan().ToArray());
    }

    [Fact]
    public void EnsureCapacityAndTrimExcess_ShouldResizeAsExpected()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>();

        int capacity = array.EnsureCapacity(16);
        Assert.True(capacity >= 16);

        array.AddRange([1, 2]);
        array.TrimExcess();

        Assert.Equal(2, array.Capacity);
        Assert.Equal([1, 2], array.AsSpan().ToArray());
    }

    [Fact]
    public void Enumerator_WhenCollectionModified_ShouldThrowInvalidOperationException()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>();
        array.AddRange([1, 2, 3]);

        using IEnumerator<int> enumerator = array.GetEnumerator();
        Assert.True(enumerator.MoveNext());

        array.Add(4);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Create_WithCollectionAndComparer_ShouldUseComparerForLookup()
    {
        UnorderedArray<string?> array = UnorderedArray.Create<string?>(
            ["A", "B", null],
            comparer: StringComparer.OrdinalIgnoreCase);

        Assert.True(array.Contains("a"));
        Assert.True(array.Contains(null));
        Assert.Equal(3, array.Count);
    }

    [Fact]
    public void UniqueIndexedMode_ShouldRejectDuplicateAdd()
    {
        UnorderedArray<int> array = UnorderedArray.CreateUnique<int>();
        array.Add(1);

        Assert.Throws<ArgumentException>(() => array.Add(1));
        Assert.Equal([1], array.AsSpan().ToArray());
    }

    [Fact]
    public void UniqueIndexedMode_CreateWithDuplicateCollection_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => UnorderedArray.CreateUnique<int>([1, 2, 1]));
    }

    [Fact]
    public void UniqueIndexedMode_Comparer_ShouldRejectEquivalentDuplicate()
    {
        UnorderedArray<string> array = UnorderedArray.CreateUnique<string>(
            comparer: StringComparer.OrdinalIgnoreCase);

        array.Add("A");

        Assert.Throws<ArgumentException>(() => array.Add("a"));
        Assert.True(array.Contains("a"));
    }

    [Fact]
    public void UniqueIndexedMode_IndexerSetDuplicate_ShouldThrowAndKeepContent()
    {
        UnorderedArray<int> array = UnorderedArray.CreateUnique<int>([1, 2, 3]);

        Assert.Throws<ArgumentException>(() => array[0] = 2);

        Assert.Equal([1, 2, 3], array.AsSpan().ToArray());
    }

    [Fact]
    public void UniqueIndexedMode_RemoveAt_ShouldUpdateMovedElementIndex()
    {
        UnorderedArray<int> array = UnorderedArray.CreateUnique<int>([1, 2, 3, 4]);

        array.RemoveAt(1);

        Assert.Equal(1, array.IndexOf(4));
        Assert.True(array.Remove(4));
        Assert.False(array.Contains(4));
        Assert.Equal([1, 3], array.AsSpan().ToArray());
    }

    [Fact]
    public void ArrayBackedMode_ShouldAlsoUseProvidedComparer()
    {
        UnorderedArray<string> array = UnorderedArray.Create<string>(
            comparer: StringComparer.OrdinalIgnoreCase);
        array.AddRange(["A", "B"]);

        Assert.True(array.Contains("a"));
        Assert.Equal(0, array.IndexOf("a"));
    }

    [Fact]
    public void Create_WithNullCollection_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UnorderedArray.Create<int>((IEnumerable<int>)null!));
    }
}
