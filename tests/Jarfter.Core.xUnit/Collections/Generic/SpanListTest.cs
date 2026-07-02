using Jarfter.Core.Collections.Generic;

namespace Jarfter.Core.xUnit.Collections.Generic;

public sealed class SpanListTest
{
    [Fact]
    public void Constructor_WithInitialCount_ShouldExposeExistingData()
    {
        int[] buffer = [10, 20, 30, 40];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        Assert.Equal(3, list.Count);
        Assert.Equal(4, list.Capacity);
        Assert.Equal([10, 20, 30], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void Constructor_WithInvalidInitialCount_ShouldThrowArgumentOutOfRangeException()
    {
        int[] buffer = [1, 2];

        bool negativeThrown = false;
        try
        {
            _ = new SpanList<int>(buffer, -1);
        }
        catch (ArgumentOutOfRangeException)
        {
            negativeThrown = true;
        }

        bool overflowThrown = false;
        try
        {
            _ = new SpanList<int>(buffer, 3);
        }
        catch (ArgumentOutOfRangeException)
        {
            overflowThrown = true;
        }

        Assert.True(negativeThrown);
        Assert.True(overflowThrown);
    }

    [Fact]
    public void Add_WhenNoCapacity_ShouldThrowInvalidOperationException()
    {
        int[] buffer = [1, 2];
        SpanList<int> list = new SpanList<int>(buffer, 2);

        bool thrown = false;
        try
        {
            list.Add(3);
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }

        Assert.True(thrown);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void TryAdd_WhenNoCapacity_ShouldReturnFalse()
    {
        int[] buffer = [1];
        SpanList<int> list = new SpanList<int>(buffer, 1);

        bool result = list.TryAdd(2);

        Assert.False(result);
        Assert.Equal([1], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void AddRange_WhenInsufficientCapacity_ShouldThrowInvalidOperationException()
    {
        int[] buffer = [1, 2, 0];
        SpanList<int> list = new SpanList<int>(buffer, 2);
        int[] source = [3, 4];

        bool thrown = false;
        try
        {
            list.AddRange(source);
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }

        Assert.True(thrown);
        Assert.Equal(2, list.Count);
        Assert.Equal([1, 2], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void Insert_ShouldShiftTailAndKeepOrder()
    {
        int[] buffer = [1, 3, 4, 0];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        list.Insert(1, 2);

        Assert.Equal(4, list.Count);
        Assert.Equal([1, 2, 3, 4], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void TryInsert_WhenNoCapacity_ShouldReturnFalse()
    {
        int[] buffer = [1, 2, 3];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        bool result = list.TryInsert(1, 9);

        Assert.False(result);
        Assert.Equal([1, 2, 3], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void InsertRange_ShouldShiftTailAndKeepOrder()
    {
        int[] buffer = [1, 4, 5, 0, 0];
        SpanList<int> list = new SpanList<int>(buffer, 3);
        int[] insertItems = [2, 3];

        list.InsertRange(1, insertItems);

        Assert.Equal(5, list.Count);
        Assert.Equal([1, 2, 3, 4, 5], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void InsertRange_WhenItemsOverlapInternalBuffer_ShouldKeepSourceSnapshot()
    {
        int[] buffer = [1, 2, 3, 4, 0, 0];
        SpanList<int> list = new SpanList<int>(buffer, 4);
        ReadOnlySpan<int> overlapItems = list.AsReadOnlySpan().Slice(1, 2);

        list.InsertRange(0, overlapItems);

        Assert.Equal(6, list.Count);
        Assert.Equal([2, 3, 1, 2, 3, 4], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void TryInsertRange_WhenNoCapacity_ShouldReturnFalse()
    {
        int[] buffer = [1, 4, 5];
        SpanList<int> list = new SpanList<int>(buffer, 3);
        int[] insertItems = [2, 3];

        bool result = list.TryInsertRange(1, insertItems);

        Assert.False(result);
        Assert.Equal([1, 4, 5], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void TryInsertRange_WhenItemsOverlapInternalBuffer_ShouldReturnTrueAndKeepSourceSnapshot()
    {
        int[] buffer = [1, 2, 3, 4, 0, 0];
        SpanList<int> list = new SpanList<int>(buffer, 4);
        ReadOnlySpan<int> overlapItems = list.AsReadOnlySpan().Slice(1, 2);

        bool result = list.TryInsertRange(0, overlapItems);

        Assert.True(result);
        Assert.Equal(6, list.Count);
        Assert.Equal([2, 3, 1, 2, 3, 4], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void InsertAndInsertRange_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        int[] buffer = [1, 2, 3, 0, 0];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        AssertInsertOutOfRange(ref list, -1);
        AssertInsertOutOfRange(ref list, 4);
        AssertTryInsertOutOfRange(ref list, -1);
        AssertTryInsertOutOfRange(ref list, 4);

        int[] insertItems = [8, 9];
        AssertInsertRangeOutOfRange(ref list, -1, insertItems);
        AssertInsertRangeOutOfRange(ref list, 4, insertItems);
        AssertTryInsertRangeOutOfRange(ref list, -1, insertItems);
        AssertTryInsertRangeOutOfRange(ref list, 4, insertItems);
    }

    [Fact]
    public void RemoveAt_ForReferenceType_ShouldClearFreedSlot()
    {
        object first = new object();
        object second = new object();
        object?[] buffer = [first, second, "tail"];
        SpanList<object?> list = new SpanList<object?>(buffer, 2);

        list.RemoveAt(0);

        Assert.Equal(1, list.Count);
        Assert.Same(second, buffer[0]);
        Assert.Null(buffer[1]);
        Assert.Equal("tail", buffer[2]);
    }

    [Fact]
    public void TryRemove_WhenMatched_ShouldReturnTrueAndRemove()
    {
        int[] buffer = [1, 2, 3, 4];
        SpanList<int> list = new SpanList<int>(buffer, 4);

        bool result = list.TryRemove(2);

        Assert.True(result);
        Assert.Equal(3, list.Count);
        Assert.Equal([1, 3, 4], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void TryRemove_WhenNotFound_ShouldReturnFalseAndKeepContent()
    {
        int[] buffer = [1, 2, 3, 4];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        bool result = list.TryRemove(9);

        Assert.False(result);
        Assert.Equal(3, list.Count);
        Assert.Equal([1, 2, 3], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void TryRemoveAt_WhenIndexOutOfRange_ShouldReturnFalse()
    {
        int[] buffer = [1, 2, 3, 4];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        Assert.False(list.TryRemoveAt(-1));
        Assert.False(list.TryRemoveAt(3));
        Assert.False(list.TryRemoveAt(100));
        Assert.Equal([1, 2, 3], list.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void TryRemoveAt_ForReferenceType_ShouldReturnTrueAndClearFreedSlot()
    {
        object first = new object();
        object second = new object();
        object?[] buffer = [first, second, "tail"];
        SpanList<object?> list = new SpanList<object?>(buffer, 2);

        bool result = list.TryRemoveAt(0);

        Assert.True(result);
        Assert.Equal(1, list.Count);
        Assert.Same(second, buffer[0]);
        Assert.Null(buffer[1]);
    }

    [Fact]
    public void RemoveRange_ShouldShiftAndClearFreedSlots()
    {
        object?[] buffer = ["a", "b", "c", "d", "keep"];
        SpanList<object?> list = new SpanList<object?>(buffer, 4);

        list.RemoveRange(1, 2);

        Assert.Equal(2, list.Count);
        Assert.Equal("a", buffer[0]);
        Assert.Equal("d", buffer[1]);
        Assert.Null(buffer[2]);
        Assert.Null(buffer[3]);
        Assert.Equal("keep", buffer[4]);
    }

    [Fact]
    public void Clear_ForReferenceType_ShouldClearUsedSlots()
    {
        object?[] buffer = ["x", "y", "z"];
        SpanList<object?> list = new SpanList<object?>(buffer, 2);

        list.Clear();

        Assert.Equal(0, list.Count);
        Assert.Null(buffer[0]);
        Assert.Null(buffer[1]);
        Assert.Equal("z", buffer[2]);
    }

    [Fact]
    public void RemoveRange_WithInvalidArgument_ShouldThrowArgumentOutOfRangeException()
    {
        int[] buffer = [1, 2, 3, 4];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        AssertRemoveRangeOutOfRange(ref list, -1, 1);
        AssertRemoveRangeOutOfRange(ref list, 0, -1);
        AssertRemoveRangeOutOfRange(ref list, 0, 4);
        AssertRemoveRangeOutOfRange(ref list, 3, 1);
    }

    [Fact]
    public void Foreach_ShouldEnumerateUsedRangeOnly()
    {
        int[] buffer = [1, 2, 3, 99, 100];
        SpanList<int> list = new SpanList<int>(buffer, 3);
        int sum = 0;

        foreach (int item in list)
        {
            sum += item;
        }

        Assert.Equal(6, sum);
    }

    [Fact]
    public void Indexer_ByRef_ShouldAllowInPlaceUpdate()
    {
        int[] buffer = [1, 2, 3];
        SpanList<int> list = new SpanList<int>(buffer, 3);

        list[1] = 42;

        Assert.Equal([1, 42, 3], list.AsReadOnlySpan().ToArray());
    }

    private static void AssertInsertOutOfRange(ref SpanList<int> list, int index)
    {
        bool thrown = false;
        try
        {
            list.Insert(index, 7);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }

    private static void AssertInsertRangeOutOfRange(ref SpanList<int> list, int index, ReadOnlySpan<int> items)
    {
        bool thrown = false;
        try
        {
            list.InsertRange(index, items);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }

    private static void AssertTryInsertOutOfRange(ref SpanList<int> list, int index)
    {
        bool thrown = false;
        try
        {
            list.TryInsert(index, 7);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }

    private static void AssertTryInsertRangeOutOfRange(ref SpanList<int> list, int index, ReadOnlySpan<int> items)
    {
        bool thrown = false;
        try
        {
            list.TryInsertRange(index, items);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }

    private static void AssertRemoveRangeOutOfRange(ref SpanList<int> list, int index, int count)
    {
        bool thrown = false;
        try
        {
            list.RemoveRange(index, count);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }
}
