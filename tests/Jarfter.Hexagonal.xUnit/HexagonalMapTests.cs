using Jarfter.Hexagonal.Direction;
using Jarfter.Hexagonal.Grid;
using Jarfter.Hexagonal.Map;

namespace Jarfter.Hexagonal.xUnit;

public sealed class DenseAxialHexagonalMapTests
{
    [Fact]
    public void Constructor_WhenDimensionsArePositive_ShouldExposeRectangularPositionsInRowMajorOrder()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(-1, 2, 3, 2);
        HexagonalGrid<int>[] expectedPositions =
        [
            new HexagonalGrid<int>(-1, 2),
            new HexagonalGrid<int>(0, 2),
            new HexagonalGrid<int>(1, 2),
            new HexagonalGrid<int>(-1, 3),
            new HexagonalGrid<int>(0, 3),
            new HexagonalGrid<int>(1, 3)
        ];

        Assert.Equal(6, map.Count);
        Assert.False(map.IsEmpty);
        Assert.Equal(expectedPositions, map.Positions);
        Assert.True(map.Contains(new HexagonalGrid<int>(1, 3)));
        Assert.False(map.Contains(new HexagonalGrid<int>(2, 3)));
    }

    [Fact]
    public void Constructor_WhenWidthIsZero_ShouldCreateEmptyMap()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(0, 0, 0, 5);

        Assert.Equal(0, map.Count);
        Assert.True(map.IsEmpty);
        Assert.Empty(map.Positions);
    }

    [Fact]
    public void Constructor_WhenHeightIsZero_ShouldCreateEmptyMap()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(0, 0, 5, 0);

        Assert.Equal(0, map.Count);
        Assert.True(map.IsEmpty);
        Assert.Empty(map.Positions);
    }

    [Fact]
    public void Constructor_WhenBoundsOverflowOrDimensionIsNegative_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DenseAxialHexagonalMap<int>(0, 0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new DenseAxialHexagonalMap<int>(0, 0, 1, -1));
        Assert.Throws<OverflowException>(() => new DenseAxialHexagonalMap<int>(int.MaxValue, 0, 2, 1));
    }

    [Fact]
    public void IndexerAndSet_WhenPositionIsInsideBounds_ShouldStoreValues()
    {
        DenseAxialHexagonalMap<string> map = new DenseAxialHexagonalMap<string>(0, 0, 2, 2);
        HexagonalGrid<int> first = new HexagonalGrid<int>(1, 0);
        HexagonalGrid<int> second = new HexagonalGrid<int>(0, 1);

        map[first] = "first";
        map.Set(second, "second");

        Assert.Equal("first", map[first]);
        Assert.True(map.TryGetValue(second, out string? value));
        Assert.Equal("second", value);
    }

    [Fact]
    public void Indexer_WhenPositionIsOutsideBounds_ShouldThrowArgumentOutOfRangeException()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(0, 0, 2, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = map[new HexagonalGrid<int>(2, 0)];
        });
    }

    [Fact]
    public void TryGetValue_WhenPositionIsOutsideBounds_ShouldReturnFalseAndDefault()
    {
        DenseAxialHexagonalMap<string> map = new DenseAxialHexagonalMap<string>(0, 0, 2, 2);

        Assert.False(map.TryGetValue(new HexagonalGrid<int>(2, 0), out string? value));
        Assert.Null(value);
    }

    [Fact]
    public void GetValueOrDefault_WhenPositionExistsOrIsMissing_ShouldReturnStoredOrFallbackValue()
    {
        DenseAxialHexagonalMap<string> denseMap = new DenseAxialHexagonalMap<string>(0, 0, 2, 2);
        IHexagonalMap<string> map = denseMap;
        HexagonalGrid<int> storedPosition = new HexagonalGrid<int>(1, 0);
        HexagonalGrid<int> missingPosition = new HexagonalGrid<int>(2, 0);
        denseMap.Set(storedPosition, "stored");

        Assert.Equal("stored", map.GetValueOrDefault(storedPosition));
        Assert.Null(map.GetValueOrDefault(missingPosition));
        Assert.Equal("fallback", map.GetValueOrDefault(missingPosition, "fallback"));
    }

    [Fact]
    public void FillAndAsSpan_WhenSpanIsMutated_ShouldUpdateMapValues()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(0, 0, 2, 2);

        map.Fill(7);
        map.AsSpan()[2] = 11;

        Assert.Equal([7, 7, 11, 7], map.Values);
        Assert.Equal([7, 7, 11, 7], map.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void GetEnumerator_WhenDenseMapIsEnumerated_ShouldReturnCellsInRowMajorOrder()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(-1, 2, 2, 2);
        int[] values = [10, 20, 30, 40];
        values.CopyTo(map.AsSpan());

        KeyValuePair<HexagonalGrid<int>, int>[] cells = [.. map];

        Assert.Equal(
            [
                new KeyValuePair<HexagonalGrid<int>, int>(new HexagonalGrid<int>(-1, 2), 10),
                new KeyValuePair<HexagonalGrid<int>, int>(new HexagonalGrid<int>(0, 2), 20),
                new KeyValuePair<HexagonalGrid<int>, int>(new HexagonalGrid<int>(-1, 3), 30),
                new KeyValuePair<HexagonalGrid<int>, int>(new HexagonalGrid<int>(0, 3), 40)
            ],
            cells);
    }

    [Fact]
    public void GetNeighbors_WhenPositionIsCorner_ShouldReturnOnlyContainedNeighborsInDirectionOrder()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(0, 0, 3, 3);
        HexagonalGrid<int>[] expected =
        [
            new HexagonalGrid<int>(1, 0),
            new HexagonalGrid<int>(0, 1)
        ];

        Assert.Equal(expected, map.GetNeighbors(new HexagonalGrid<int>(0, 0)));
    }

    [Fact]
    public void GetEnumerator_WhenEnumerationHasEnded_ShouldKeepReturningFalse()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(0, 0, 1, 1);
        DenseAxialHexagonalMap<int>.Enumerator enumerator = map.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
    }

    [Fact]
    public void CopyNeighborCellsTo_WhenNeighborValuesExist_ShouldCopyContainedNeighborCells()
    {
        DenseAxialHexagonalMap<string> map = new DenseAxialHexagonalMap<string>(0, 0, 3, 3);
        map[new HexagonalGrid<int>(1, 0)] = "east";
        map[new HexagonalGrid<int>(0, 1)] = "south-east";
        KeyValuePair<HexagonalGrid<int>, string>[] destination = new KeyValuePair<HexagonalGrid<int>, string>[6];

        int count = map.CopyNeighborCellsTo(new HexagonalGrid<int>(0, 0), destination);

        Assert.Equal(2, count);
        Assert.Equal(new KeyValuePair<HexagonalGrid<int>, string>(new HexagonalGrid<int>(1, 0), "east"), destination[0]);
        Assert.Equal(new KeyValuePair<HexagonalGrid<int>, string>(new HexagonalGrid<int>(0, 1), "south-east"), destination[1]);
    }

    [Fact]
    public void CopyNeighborsTo_WhenDestinationIsTooSmall_ShouldThrowArgumentException()
    {
        DenseAxialHexagonalMap<int> map = new DenseAxialHexagonalMap<int>(0, 0, 3, 3);
        HexagonalGrid<int>[] destination = new HexagonalGrid<int>[5];

        Assert.Throws<ArgumentException>(() =>
        {
            map.CopyNeighborsTo(HexagonalGrid<int>.Zero, destination);
        });
    }
}

public sealed class DenseHexagonalRadiusMapTests
{
    [Fact]
    public void Constructor_WhenRadiusIsTwo_ShouldContainAllCellsWithinRadius()
    {
        HexagonalGrid<int> center = new HexagonalGrid<int>(3, -2);
        DenseHexagonalRadiusMap<int> map = new DenseHexagonalRadiusMap<int>(center, 2);
        HexagonalGrid<int>[] positions = [.. map.Positions];

        Assert.Equal(HexagonalGridMetrics.CountInRange(2), map.Count);
        Assert.Equal(positions.Length, positions.Distinct().Count());
        Assert.All(positions, position => Assert.True(center.DistanceTo(position) <= 2));
        Assert.True(map.Contains(center));
        Assert.False(map.Contains(center.Neighbor(HexagonalDirection.PositiveQ, 3)));
    }

    [Fact]
    public void IndexerAndEnumeration_WhenPositionIsInsideRadius_ShouldStoreValues()
    {
        DenseHexagonalRadiusMap<string> map = new DenseHexagonalRadiusMap<string>(1);
        HexagonalGrid<int> position = new HexagonalGrid<int>(1, -1);

        map[position] = "edge";

        Assert.True(map.TryGetValue(position, out string? value));
        Assert.Equal("edge", value);
        Assert.Contains(map, cell => cell.Key == position && cell.Value == "edge");
    }

    [Fact]
    public void GetEnumerator_WhenDenseMapIsEnumerated_ShouldReturnEveryCell()
    {
        DenseHexagonalRadiusMap<int> map = new DenseHexagonalRadiusMap<int>(1);
        int[] values = [10, 20, 30, 40, 50, 60, 70];
        values.CopyTo(map.AsSpan());

        KeyValuePair<HexagonalGrid<int>, int>[] cells = [.. map];

        Assert.Equal(map.Count, cells.Length);
        Assert.Equal([10, 20, 30, 40, 50, 60, 70], cells.Select(static cell => cell.Value));
        Assert.Equal(map.Positions, cells.Select(static cell => cell.Key));
    }

    [Fact]
    public void GetNeighbors_WhenPositionIsOnRadiusEdge_ShouldReturnOnlyContainedNeighbors()
    {
        DenseHexagonalRadiusMap<int> map = new DenseHexagonalRadiusMap<int>(1);
        HexagonalGrid<int>[] expected =
        [
            new HexagonalGrid<int>(1, -1),
            new HexagonalGrid<int>(0, 0),
            new HexagonalGrid<int>(0, 1)
        ];

        Assert.Equal(expected, map.GetNeighbors(new HexagonalGrid<int>(1, 0)));
    }

    [Fact]
    public void GetEnumerator_WhenEnumerationHasEnded_ShouldKeepReturningFalse()
    {
        DenseHexagonalRadiusMap<int> map = new DenseHexagonalRadiusMap<int>(0);
        DenseHexagonalRadiusMap<int>.Enumerator enumerator = map.GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => _ = enumerator.Current);
    }

    [Fact]
    public void CopyNeighborsTo_WhenDestinationIsLargeEnough_ShouldCopyContainedNeighbors()
    {
        DenseHexagonalRadiusMap<int> map = new DenseHexagonalRadiusMap<int>(1);
        HexagonalGrid<int>[] destination = new HexagonalGrid<int>[6];

        int count = map.CopyNeighborsTo(new HexagonalGrid<int>(1, 0), destination);

        Assert.Equal(3, count);
        Assert.Equal(new HexagonalGrid<int>(1, -1), destination[0]);
        Assert.Equal(new HexagonalGrid<int>(0, 0), destination[1]);
        Assert.Equal(new HexagonalGrid<int>(0, 1), destination[2]);
    }

    [Fact]
    public void Constructor_WhenRadiusIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DenseHexagonalRadiusMap<int>(-1));
    }

    [Fact]
    public void Constructor_WhenRadiusIsZero_ShouldContainOnlyCenter()
    {
        HexagonalGrid<int> center = new HexagonalGrid<int>(3, -2);
        DenseHexagonalRadiusMap<int> map = new DenseHexagonalRadiusMap<int>(center, 0);

        Assert.Equal(1, map.Count);
        Assert.Equal([center], map.Positions);
        Assert.True(map.Contains(center));
        Assert.Empty(map.GetNeighbors(center));
    }
}

public sealed class SparseHexagonalMapTests
{
    [Fact]
    public void Constructor_WhenCellsAreProvided_ShouldStoreAllCells()
    {
        KeyValuePair<HexagonalGrid<int>, string>[] cells =
        [
            new KeyValuePair<HexagonalGrid<int>, string>(HexagonalGrid<int>.Zero, "center"),
            new KeyValuePair<HexagonalGrid<int>, string>(new HexagonalGrid<int>(1, 0), "east")
        ];

        SparseHexagonalMap<string> map = new SparseHexagonalMap<string>(cells);

        Assert.Equal(2, map.Count);
        Assert.False(map.IsEmpty);
        Assert.Equal("center", map[HexagonalGrid<int>.Zero]);
        Assert.Equal("east", map[new HexagonalGrid<int>(1, 0)]);
    }

    [Fact]
    public void AddSetRemoveAndClear_WhenCalled_ShouldUpdateMapState()
    {
        SparseHexagonalMap<int> map = new SparseHexagonalMap<int>();
        HexagonalGrid<int> position = new HexagonalGrid<int>(1, -1);

        Assert.True(map.TryAdd(position, 4));
        Assert.False(map.TryAdd(position, 5));
        map.Set(position, 9);

        Assert.Equal(1, map.Count);
        Assert.Equal(9, map[position]);
        Assert.True(map.Remove(position));
        Assert.False(map.Contains(position));

        map.Add(HexagonalGrid<int>.Zero, 1);
        map.Clear();

        Assert.True(map.IsEmpty);
    }

    [Fact]
    public void GetNeighborCells_WhenSparseNeighborsExist_ShouldReturnOnlyStoredNeighborCells()
    {
        SparseHexagonalMap<string> map = new SparseHexagonalMap<string>();
        HexagonalGrid<int> center = HexagonalGrid<int>.Zero;
        map.Set(center.Neighbor(HexagonalDirection.PositiveQ), "east");
        map.Set(center.Neighbor(HexagonalDirection.PositiveS), "north-west");
        map.Set(new HexagonalGrid<int>(3, 3), "outside");

        KeyValuePair<HexagonalGrid<int>, string>[] cells = [.. map.GetNeighborCells(center)];

        Assert.Equal(2, cells.Length);
        Assert.Equal(new KeyValuePair<HexagonalGrid<int>, string>(new HexagonalGrid<int>(1, 0), "east"), cells[0]);
        Assert.Equal(new KeyValuePair<HexagonalGrid<int>, string>(new HexagonalGrid<int>(0, -1), "north-west"), cells[1]);
    }

    [Fact]
    public void CopyNeighborCellsTo_WhenSparseNeighborsExist_ShouldCopyOnlyStoredNeighborCells()
    {
        SparseHexagonalMap<string> map = new SparseHexagonalMap<string>();
        HexagonalGrid<int> center = HexagonalGrid<int>.Zero;
        map.Set(center.Neighbor(HexagonalDirection.NegativeR), "north-east");
        map.Set(center.Neighbor(HexagonalDirection.NegativeS), "south-east");
        KeyValuePair<HexagonalGrid<int>, string>[] destination = new KeyValuePair<HexagonalGrid<int>, string>[6];

        int count = map.CopyNeighborCellsTo(center, destination);

        Assert.Equal(2, count);
        Assert.Equal(new KeyValuePair<HexagonalGrid<int>, string>(new HexagonalGrid<int>(1, -1), "north-east"), destination[0]);
        Assert.Equal(new KeyValuePair<HexagonalGrid<int>, string>(new HexagonalGrid<int>(0, 1), "south-east"), destination[1]);
    }

    [Fact]
    public void Constructor_WhenCellsIsNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SparseHexagonalMap<int>(null!));
    }

    [Fact]
    public void Constructor_WhenCellsContainDuplicatePositions_ShouldThrowArgumentException()
    {
        KeyValuePair<HexagonalGrid<int>, int>[] cells =
        [
            new KeyValuePair<HexagonalGrid<int>, int>(HexagonalGrid<int>.Zero, 1),
            new KeyValuePair<HexagonalGrid<int>, int>(HexagonalGrid<int>.Zero, 2)
        ];

        Assert.Throws<ArgumentException>(() => new SparseHexagonalMap<int>(cells));
    }

    [Fact]
    public void GetNeighbors_WhenDenseAndSparseMapsContainSamePositions_ShouldReturnSamePositions()
    {
        DenseAxialHexagonalMap<int> denseMap = new DenseAxialHexagonalMap<int>(0, 0, 3, 3);
        SparseHexagonalMap<int> sparseMap = new SparseHexagonalMap<int>();
        foreach (HexagonalGrid<int> position in denseMap.Positions)
        {
            sparseMap.Set(position, 0);
        }

        HexagonalGrid<int> center = new HexagonalGrid<int>(1, 1);

        Assert.Equal(denseMap.GetNeighbors(center), sparseMap.GetNeighbors(center));
    }

    [Fact]
    public void ReadOnlyInterface_WhenMapIsEnumerated_ShouldExposeAllCells()
    {
        IReadOnlyHexagonalMap<int> map = new SparseHexagonalMap<int>(
        [
            new KeyValuePair<HexagonalGrid<int>, int>(HexagonalGrid<int>.Zero, 1),
            new KeyValuePair<HexagonalGrid<int>, int>(new HexagonalGrid<int>(1, 0), 2)
        ]);

        Assert.Equal(2, map.Count());
    }
}
