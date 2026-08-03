using Jarfter.Core.Collections.Generic;

namespace Jarfter.Core.xUnit.Collections.Generic;

/// <summary>
/// 验证 <see cref="LinkedHashSet{T}"/> 的容量和版本行为.
/// </summary>
public sealed class LinkedHashSetTest
{
    /// <summary>
    /// 容量属性应返回构造时指定的最大元素数量.
    /// </summary>
    [Fact]
    public void Capacity_ShouldReturnConfiguredMaximum()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(3);

        Assert.Equal(3, set.Capacity);
    }

    /// <summary>
    /// 负容量应在构造时抛出参数范围异常.
    /// </summary>
    [Fact]
    public void Constructor_WithNegativeCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinkedHashSet<int>(-1));
    }

    /// <summary>
    /// 成功移除元素应递增集合版本号.
    /// </summary>
    [Fact]
    public void Remove_WhenItemExists_ShouldIncrementVersion()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(2);
        set.Add(1);
        int version = set.Version;

        Assert.True(set.Remove(1));
        Assert.Equal(version + 1, set.Version);
    }

    /// <summary>
    /// 新元素应位于首部, 重复元素应移动到首部, 超出容量时应淘汰尾部元素.
    /// </summary>
    [Fact]
    public void Add_WhenAddingAndReaddingItems_ShouldMaintainOrderAndCapacity()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(3);

        set.Add(1);
        set.Add(2);
        set.Add(3);
        set.Add(2);
        set.Add(4);

        Assert.Equal([4, 2, 3], set.ToArray());
        Assert.DoesNotContain(1, set);
        Assert.Equal(3, set.Count);
    }

    /// <summary>
    /// 零容量集合添加元素后应保持为空.
    /// </summary>
    [Fact]
    public void Add_WhenCapacityIsZero_ShouldRemainEmpty()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(0);

        set.Add(1);

        Assert.Empty(set);
        Assert.DoesNotContain(1, set);
    }

    /// <summary>
    /// TryAdd 应区分首次添加和重复添加, 同时保持重复元素移动到首部的行为.
    /// </summary>
    [Fact]
    public void TryAdd_WhenItemIsNewOrExisting_ShouldReportInsertionAndMaintainOrder()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(2);
        set.Add(1);
        set.Add(2);

        Assert.False(set.TryAdd(1));
        Assert.True(set.TryAdd(3));

        Assert.Equal([3, 1], set.ToArray());
    }

    /// <summary>
    /// 加入新元素超出容量时, 应返回被淘汰的尾部元素.
    /// </summary>
    [Fact]
    public void AddAndGetEvicted_WhenCapacityIsExceeded_ShouldReturnRemovedItem()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(2);
        set.Add(1);
        set.Add(2);

        int evicted = set.AddAndGetEvicted(3);
        int noEviction = set.AddAndGetEvicted(3);

        Assert.Equal(1, evicted);
        Assert.Equal(0, noEviction);
        Assert.Equal([3, 2], set.ToArray());
    }

    /// <summary>
    /// 零容量集合加入元素时, 应返回刚被淘汰的元素并保持为空.
    /// </summary>
    [Fact]
    public void AddAndGetEvicted_WhenCapacityIsZero_ShouldReturnAddedItem()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(0);

        int evicted = set.AddAndGetEvicted(1);

        Assert.Equal(1, evicted);
        Assert.Empty(set);
    }

    /// <summary>
    /// 首尾查看和移除操作应返回对应元素并维护剩余顺序.
    /// </summary>
    [Fact]
    public void FirstAndLastOperations_WhenCollectionIsEmptyOrNonEmpty_ShouldReturnExpectedValues()
    {
        LinkedHashSet<int> set = new LinkedHashSet<int>(3);

        Assert.False(set.TryPeekFirst(out int emptyFirst));
        Assert.False(set.TryPeekLast(out int emptyLast));
        Assert.Equal(0, emptyFirst);
        Assert.Equal(0, emptyLast);

        set.Add(1);
        set.Add(2);
        set.Add(3);

        Assert.True(set.TryPeekFirst(out int first));
        Assert.True(set.TryPeekLast(out int last));
        Assert.Equal(3, first);
        Assert.Equal(1, last);

        Assert.True(set.TryRemoveFirst(out int removedFirst));
        Assert.True(set.TryRemoveLast(out int removedLast));
        Assert.Equal(3, removedFirst);
        Assert.Equal(1, removedLast);
        Assert.Equal([2], set.ToArray());

        Assert.True(set.TryRemoveFirst(out int lastItem));
        Assert.Equal(2, lastItem);
        Assert.False(set.TryRemoveLast(out int removedFromEmpty));
        Assert.Equal(0, removedFromEmpty);
    }

    /// <summary>
    /// 集合接口应报告可写状态并按逻辑顺序复制元素.
    /// </summary>
    [Fact]
    public void CollectionMembers_ShouldReportWritableAndCopyInOrder()
    {
        ICollection<int> collection = new LinkedHashSet<int>(3);
        collection.Add(1);
        collection.Add(2);

        int[] destination = [0, 0, 0];
        collection.CopyTo(destination, 1);

        Assert.False(collection.IsReadOnly);
        Assert.Equal([0, 2, 1], destination);
    }
}
