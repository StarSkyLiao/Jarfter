using System.Collections;
using Jarfter.Core.Collections.Generic;

namespace Jarfter.Core.xUnit.Collections.Generic;

public sealed class LinkedArrayTest
{
    [Fact]
    public void Constructor_Default_ShouldCreateEmptyCollection()
    {
        LinkedArray<int> list = [];

        Assert.Empty(list);
        Assert.Equal(0, list.Capacity);
        Assert.Null(list.First);
        Assert.Null(list.Last);
        Assert.Equal(0, list.FirstValue);
        Assert.Equal(0, list.LastValue);
    }

    [Fact]
    public void Constructor_WithCapacity_ShouldRespectCapacityAndThrowForNegative()
    {
        LinkedArray<int> list = new LinkedArray<int>(5);

        Assert.Empty(list);
        Assert.Equal(5, list.Capacity);

        Assert.Throws<ArgumentOutOfRangeException>(() => new LinkedArray<int>(-1));
    }

    [Fact]
    public void Constructor_WithCollection_ShouldCopyDataAndThrowForNull()
    {
        LinkedArray<int> fromCollection = new LinkedArray<int>([1, 2, 3]);

        Assert.Equal([1, 2, 3], Snapshot(fromCollection));
        Assert.Equal(3, fromCollection.Count);

        Assert.Throws<ArgumentNullException>(() => new LinkedArray<int>(null!));
    }

    [Fact]
    public void AddFamily_ShouldKeepLinkedOrder()
    {
        LinkedArray<int> list =
        [
            2
        ];

        list.AddFirst(1);
        list.AddLast(3);
        list.Enqueue(4);

        Assert.Equal([1, 2, 3, 4], Snapshot(list));
        Assert.Equal(1, RequireNode(list.First).Value);
        Assert.Equal(4, RequireNode(list.Last).Value);
        Assert.Equal(1, list.FirstValue);
        Assert.Equal(4, list.LastValue);
    }

    [Fact]
    public void QueueMethods_ShouldHandleEmptyAndNonEmptyCases()
    {
        LinkedArray<int> list = [];

        bool emptyTryDequeue = list.TryDequeue(out int emptyOut);
        int emptyDequeue = list.Dequeue();

        Assert.False(emptyTryDequeue);
        Assert.Equal(0, emptyOut);
        Assert.Equal(0, emptyDequeue);
        Assert.False(list.RemoveFirst());
        Assert.False(list.RemoveLast());

        list.AddRange([1, 2, 3]);
        Assert.True(list.TryDequeue(out int value));
        Assert.Equal(1, value);
        Assert.Equal(2, list.Dequeue());
        Assert.True(list.RemoveLast());
        Assert.Empty(list);
    }

    [Fact]
    public void ListIndexerInsertRemoveAt_ShouldOperateByLogicalIndex()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 3, 4]);

        list.Insert(1, 2);
        Assert.Equal([1, 2, 3, 4], Snapshot(list));

        Assert.Equal(3, list[2]);
        list[2] = 30;
        Assert.Equal([1, 2, 30, 4], Snapshot(list));

        list.RemoveAt(2);
        Assert.Equal([1, 2, 4], Snapshot(list));
    }

    [Fact]
    public void ListIndexerInsertRemoveAt_WhenIndexOutOfRange_ShouldThrow()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = list[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => list[3] = 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(3));
    }

    [Fact]
    public void FindAndFindLast_ShouldReturnExpectedNodes()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3, 2]);

        LinkedArrayNode<int> firstTwo = RequireNode(list.Find(2));
        LinkedArrayNode<int> lastTwo = RequireNode(list.FindLast(2));

        Assert.Equal(2, firstTwo.Value);
        Assert.Equal(2, lastTwo.Value);
        Assert.Equal(1, RequireNode(firstTwo.Previous).Value);
        Assert.Equal(3, RequireNode(firstTwo.Next).Value);
        Assert.Equal(3, RequireNode(lastTwo.Previous).Value);
        Assert.Null(lastTwo.Next);
        Assert.Null(list.Find(9));
        Assert.Null(list.FindLast(9));
    }

    [Fact]
    public void AddBeforeAndAddAfter_ShouldInsertAroundNode()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 3]);
        LinkedArrayNode<int> node3 = RequireNode(list.Find(3));

        LinkedArrayNode<int> node2 = list.AddBefore(node3, 2);
        list.AddAfter(node2, 99);

        Assert.Equal([1, 2, 99, 3], Snapshot(list));
    }

    [Fact]
    public void AddBeforeAddAfter_WithInvalidNode_ShouldThrowInvalidOperationException()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2]);
        LinkedArray<int> other = new LinkedArray<int>([9]);

        LinkedArrayNode<int> foreign = RequireNode(other.First);
        LinkedArrayNode<int> stale = RequireNode(list.First);
        list.Add(3);

        Assert.Throws<InvalidOperationException>(() => list.AddBefore(foreign, 0));
        Assert.Throws<InvalidOperationException>(() => list.AddAfter(foreign, 0));
        Assert.Throws<InvalidOperationException>(() => list.AddBefore(stale, 0));
        Assert.Throws<InvalidOperationException>(() => list.AddAfter(stale, 0));
    }

    [Fact]
    public void Remove_ByNode_ShouldSupportCurrentNodeAndRejectForeignOrStaleNode()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);
        LinkedArray<int> other = new LinkedArray<int>([4, 5]);

        LinkedArrayNode<int> current = RequireNode(list.Find(2));
        LinkedArrayNode<int> foreign = RequireNode(other.First);
        LinkedArrayNode<int> stale = RequireNode(list.First);

        Assert.True(list.Remove(current));
        Assert.Equal([1, 3], Snapshot(list));

        Assert.False(list.Remove(foreign));

        list.Add(9);
        Assert.False(list.Remove(stale));
    }

    [Fact]
    public void AddRange_WithSelf_ShouldAppendSnapshotWithoutVersionConflict()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);

        list.AddRange(list);

        Assert.Equal([1, 2, 3, 1, 2, 3], Snapshot(list));
    }

    [Fact]
    public void AddRange_WithICollectionAndEnumerable_ShouldAppendElements()
    {
        LinkedArray<int> list = [];

        list.AddRange(new List<int> { 1, 2 });
        list.AddRange(Generate(3, 2));

        Assert.Equal([1, 2, 3, 4], Snapshot(list));
        Assert.Throws<ArgumentNullException>(() => list.AddRange(null!));
    }

    [Fact]
    public void ContainsIndexOfRemove_ShouldMatchExpectedBehavior()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3, 2]);

        bool containsTwo = list.Contains(2);
        Assert.Equal(1, list.IndexOf(2));
        bool containsNine = list.Contains(9);
        Assert.Equal(-1, list.IndexOf(9));

        Assert.True(containsTwo);
        Assert.False(containsNine);

        Assert.True(list.Remove(2));
        Assert.Equal([1, 3, 2], Snapshot(list));
        Assert.False(list.Remove(9));
    }

    [Fact]
    public void RemoveNode_WithTailMove_ShouldKeepLogicalOrder()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3, 4, 5]);

        list.RemoveAt(1);
        list.Remove(4);

        Assert.Equal([1, 3, 5], Snapshot(list));
        Assert.Equal(1, RequireNode(list.First).Value);
        Assert.Equal(5, RequireNode(list.Last).Value);
    }

    [Fact]
    public void EnsureCapacityAndCapacitySetter_ShouldResizeAndValidate()
    {
        LinkedArray<int> list = [];

        int ensured = list.EnsureCapacity(7);
        Assert.True(ensured >= 7);

        list.AddRange([1, 2, 3]);
        int previousCapacity = list.Capacity;
        list.Capacity = previousCapacity + 2;
        Assert.Equal(previousCapacity + 2, list.Capacity);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.EnsureCapacity(-1));
    }

    [Fact]
    public void TrimExcess_ShouldShrinkOnlyWhenUsageIsLow()
    {
        LinkedArray<int> list = new LinkedArray<int>(20);
        list.AddRange([1, 2]);

        list.TrimExcess();

        Assert.Equal(2, list.Capacity);

        LinkedArray<int> dense = new LinkedArray<int>(10);
        dense.AddRange([1, 2, 3, 4, 5, 6, 7, 8, 9]);

        dense.TrimExcess();

        Assert.Equal(10, dense.Capacity);
    }

    [Fact]
    public void AsReadOnly_ShouldExposeLiveReadonlyView()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2]);
        IReadOnlyList<int> readOnly = list.AsReadOnly();

        Assert.Equal(2, readOnly.Count);
        Assert.Equal(2, readOnly[1]);

        list.Add(3);

        Assert.Equal(3, readOnly.Count);
        Assert.Equal(3, readOnly[2]);
    }

    [Fact]
    public void ConvertAll_ShouldConvertInLogicalOrder()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);

        List<string> text = list.ConvertAll(static value => $"#{value}");

        Assert.Equal(["#1", "#2", "#3"], text);
        Assert.Throws<ArgumentNullException>(() => list.ConvertAll<string>(null!));
    }

    [Fact]
    public void Enumerator_ShouldEnumerateInOrder()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);
        List<int> snapshot = [];

        foreach (int item in list) snapshot.Add(item);

        Assert.Equal([1, 2, 3], snapshot);
    }

    [Fact]
    public void Enumerator_WhenCollectionModified_ShouldThrowOnMoveNextAndReset()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);
        using LinkedArray<int>.Enumerator enumerator = list.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        list.Add(4);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        IEnumerator nonGeneric = ((IEnumerable)list).GetEnumerator();
        Assert.True(nonGeneric.MoveNext());
        list.Add(5);
        Assert.Throws<InvalidOperationException>(nonGeneric.Reset);
        (nonGeneric as IDisposable)?.Dispose();
    }

    [Fact]
    public void GenericCollectionMembers_ShouldCopyAndReportReadOnlyCorrectly()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);
        ICollection<int> collection = list;

        Assert.False(collection.IsReadOnly);

        int[] destination = [0, 0, 0, 0, 0];
        collection.CopyTo(destination, 1);
        Assert.Equal([0, 1, 2, 3, 0], destination);

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(destination, -1));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new int[2], 0));
    }

    [Fact]
    public void NonGenericCollectionMembers_ShouldValidateAndCopyToObjectArray()
    {
        LinkedArray<int> list = new LinkedArray<int>([1, 2, 3]);
        ICollection collection = list;

        Assert.False(collection.IsSynchronized);
        Assert.Same(list, collection.SyncRoot);

        object?[] boxed = [null, null, null, null, null];
        collection.CopyTo(boxed, 1);
        Assert.Equal([null, 1, 2, 3, null], boxed);

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(boxed, -1));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new object?[2], 0));
        Assert.Throws<ArgumentException>(() => collection.CopyTo(new int[2, 2], 0));
        Assert.Throws<ArrayTypeMismatchException>(() => collection.CopyTo(new string[5], 0));
    }

    [Fact]
    public void NonGenericListMembers_ShouldWorkForCompatibleValues()
    {
        IList list = new LinkedArray<int>();

        Assert.False(list.IsReadOnly);
        Assert.False(list.IsFixedSize);

        int index0 = list.Add(1);
        list.Insert(1, 2);
        list[0] = 10;

        Assert.Equal(0, index0);
        Assert.True(list.Contains(10));
        Assert.Equal(0, list.IndexOf(10));

        list.Remove(2);

        Assert.Single(list);
        Assert.Equal(10, list[0]);
    }

    [Fact]
    public void NonGenericListMembers_WithIncompatibleValue_ShouldThrowOrIgnore()
    {
        IList list = new LinkedArray<int>([1, 2]);

        Assert.Throws<ArgumentException>(() => list.Add("x"));
        Assert.Throws<ArgumentException>(() => list.Insert(0, "x"));
        Assert.Throws<ArgumentException>(() => list[0] = "x");
        Assert.Throws<ArgumentException>(() => list.Add(null));

        Assert.False(list.Contains("x"));
        Assert.Equal(-1, list.IndexOf("x"));

        list.Remove("x");
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void LinkedArrayNode_DefaultAndExpiredNode_ShouldThrowInvalidOperationException()
    {
        LinkedArrayNode<int> defaultNode = default;
        Assert.Throws<InvalidOperationException>(() => _ = defaultNode.Value);
        Assert.Throws<InvalidOperationException>(() => _ = defaultNode.Previous);
        Assert.Throws<InvalidOperationException>(() => _ = defaultNode.Next);

        LinkedArray<int> list = new LinkedArray<int>([1, 2]);
        LinkedArrayNode<int> node = RequireNode(list.First);
        list.Add(3);

        Assert.Throws<InvalidOperationException>(() => _ = node.Value);
        Assert.Throws<InvalidOperationException>(() => _ = node.Previous);
        Assert.Throws<InvalidOperationException>(() => _ = node.Next);
    }

    private static int[] Snapshot(LinkedArray<int> list)
    {
        int[] array = new int[list.Count];
        list.CopyTo(array, 0);
        return array;
    }

    private static LinkedArrayNode<T> RequireNode<T>(LinkedArrayNode<T>? node)
    {
        Assert.True(node.HasValue);
        return node.Value;
    }

    private static IEnumerable<int> Generate(int start, int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return start + i;
        }
    }
}
