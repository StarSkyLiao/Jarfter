using Jarfter.Core.Collections.Generic;

namespace Jarfter.Core.xUnit.Collections.Generic;

/// <summary>
/// 验证 <see cref="SkipList{T}"/> 的边界查找和按索引范围删除行为.
/// </summary>
public sealed class SkipListTest
{
    /// <summary>
    /// 验证边界查找会返回重复元素两端和集合末尾的插入位置.
    /// </summary>
    [Fact]
    public void BoundIndexes_WithDuplicates_ShouldReturnInsertionBoundaries()
    {
        SkipList<int> list = new SkipList<int>([1, 2, 2, 2, 4, 6]);

        Assert.Equal(0, list.LowerBoundIndex(0));
        Assert.Equal(0, list.UpperBoundIndex(0));
        Assert.Equal(0, list.LowerBoundIndex(1));
        Assert.Equal(1, list.UpperBoundIndex(1));
        Assert.Equal(1, list.LowerBoundIndex(2));
        Assert.Equal(4, list.UpperBoundIndex(2));
        Assert.Equal(4, list.LowerBoundIndex(3));
        Assert.Equal(4, list.UpperBoundIndex(3));
        Assert.Equal(5, list.LowerBoundIndex(6));
        Assert.Equal(6, list.UpperBoundIndex(6));
        Assert.Equal(6, list.LowerBoundIndex(7));
        Assert.Equal(6, list.UpperBoundIndex(7));
    }

    /// <summary>
    /// 验证空跳表的边界查找返回零.
    /// </summary>
    [Fact]
    public void BoundIndexes_WhenEmpty_ShouldReturnZero()
    {
        SkipList<int> list = new SkipList<int>();

        Assert.Equal(0, list.LowerBoundIndex(1));
        Assert.Equal(0, list.UpperBoundIndex(1));
    }

    /// <summary>
    /// 验证范围删除会跨越多层链接, 同时保留范围外元素和正确的索引.
    /// </summary>
    [Fact]
    public void RemoveIndexRange_WhenMiddleRange_ShouldPreserveRemainingElementsAndIndexes()
    {
        SkipList<int> list = new SkipList<int>(Enumerable.Range(0, 10));

        list.RemoveIndexRange(3, 4);

        Assert.Equal([0, 1, 2, 7, 8, 9], list.ToArray());
        Assert.Equal(6, list.Count);
        Assert.Equal(0, list.LowerBoundIndex(0));
        Assert.Equal(3, list.LowerBoundIndex(7));
        Assert.Equal(6, list.UpperBoundIndex(9));
        Assert.Equal(8, list[4]);
    }

    /// <summary>
    /// 验证范围删除支持空范围和整个集合, 并验证参数边界.
    /// </summary>
    [Fact]
    public void RemoveIndexRange_WithEmptyFullAndInvalidRanges_ShouldMatchListSemantics()
    {
        SkipList<int> list = new SkipList<int>([1, 2, 3]);

        list.RemoveIndexRange(3, 0);
        Assert.Equal([1, 2, 3], list.ToArray());

        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveIndexRange(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveIndexRange(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveIndexRange(4, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveIndexRange(2, 2));

        list.RemoveIndexRange(0, list.Count);

        Assert.Empty(list);
        Assert.Equal(0, list.LowerBoundIndex(1));
        Assert.Equal(0, list.UpperBoundIndex(1));
    }

    /// <summary>
    /// 验证交替插入与范围删除后, 跳表顺序、跨度和边界索引始终与排序列表一致.
    /// </summary>
    [Fact]
    public void AddAndRemoveIndexRange_WhenRepeated_ShouldMatchSortedList()
    {
        Random random = new Random(20260730);
        SkipList<int> list = new SkipList<int>();
        List<int> expected = [];

        for (int i = 0; i < 300; i++)
        {
            if (expected.Count == 0 || random.Next(2) == 0)
            {
                int value = random.Next(-10, 11);
                list.Add(value);
                expected.Add(value);
                expected.Sort();
            }
            else
            {
                int index = random.Next(expected.Count);
                int count = random.Next(1, expected.Count - index + 1);
                list.RemoveIndexRange(index, count);
                expected.RemoveRange(index, count);
            }

            Assert.Equal(expected, list.ToArray());
            for (int value = -11; value <= 11; value++)
            {
                Assert.Equal(LowerBound(expected, value), list.LowerBoundIndex(value));
                Assert.Equal(UpperBound(expected, value), list.UpperBoundIndex(value));
            }
        }
    }

    private static int LowerBound(IReadOnlyList<int> values, int value)
    {
        int index = 0;
        while (index < values.Count && values[index] < value) index++;
        return index;
    }

    private static int UpperBound(IReadOnlyList<int> values, int value)
    {
        int index = 0;
        while (index < values.Count && values[index] <= value) index++;
        return index;
    }
}
