using Jarfter.Hexagonal.Direction;
using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.xUnit;

public sealed class HexagonalGridTests
{
    [Fact]
    public void FromCube_WhenCoordinatesSumToZero_ShouldCreateEquivalentAxialCoordinate()
    {
        HexagonalGrid<int> cell = HexagonalGrid<int>.FromCube(2, -5, 3);

        Assert.Equal(new HexagonalGrid<int>(2, -5), cell);
        Assert.Equal(3, cell.S);
    }

    [Fact]
    public void FromCube_WhenCoordinatesDoNotSumToZero_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => HexagonalGrid<int>.FromCube(1, 2, 3));
    }

    [Fact]
    public void FromCube_WhenComponentSumOverflowsInt32_ShouldThrowOverflowException()
    {
        Assert.Throws<OverflowException>(() => HexagonalGrid<int>.FromCube(int.MaxValue, int.MaxValue, 2));
    }

    [Fact]
    public void Direction_WhenDirectionIsValid_ShouldReturnExpectedOffset()
    {
        HexagonalGrid<int>[] expected =
        [
            new HexagonalGrid<int>(1, 0),
            new HexagonalGrid<int>(1, -1),
            new HexagonalGrid<int>(0, -1),
            new HexagonalGrid<int>(-1, 0),
            new HexagonalGrid<int>(-1, 1),
            new HexagonalGrid<int>(0, 1)
        ];

        for (int index = 0; index < expected.Length; index++)
        {
            Assert.Equal(expected[index], HexagonalGrid<int>.Direction((HexagonalDirection)index));
        }
    }

    [Fact]
    public void Neighbors_WhenDirectlyEnumerated_ShouldUseValueTypeEnumerableAndReturnDirectionsInOrder()
    {
        HexagonalGrid<int> cell = new HexagonalGrid<int>(2, -1);
        HexagonalGrid<int>.NeighborEnumerable neighbors = cell.Neighbors();
        HexagonalGrid<int>.NeighborEnumerable.Enumerator enumerator = neighbors.GetEnumerator();

        Assert.Equal(
            [
                new HexagonalGrid<int>(3, -1),
                new HexagonalGrid<int>(3, -2),
                new HexagonalGrid<int>(2, -2),
                new HexagonalGrid<int>(1, -1),
                new HexagonalGrid<int>(1, 0),
                new HexagonalGrid<int>(2, 0)
            ],
            neighbors);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(new HexagonalGrid<int>(3, -1), enumerator.Current);
    }

    [Fact]
    public void Neighbor_WhenDistanceIsProvided_ShouldMoveByDirectionTimesDistance()
    {
        HexagonalGrid<int> cell = new HexagonalGrid<int>(2, -1);

        HexagonalGrid<int> neighbor = cell.Neighbor(HexagonalDirection.PositiveR, 3);

        Assert.Equal(new HexagonalGrid<int>(-1, 2), neighbor);
    }

    [Fact]
    public void DistanceTo_WhenCellsAreSeparated_ShouldReturnHexDistance()
    {
        HexagonalGrid<int> source = new HexagonalGrid<int>(0, 0);
        HexagonalGrid<int> target = new HexagonalGrid<int>(3, -1);

        Assert.Equal(3, source.DistanceTo(target));
        Assert.Equal(3, target.DistanceTo(source));
    }

    [Fact]
    public void TryDistanceTo_WhenResultFitsOrOverflowsInt32_ShouldReturnExpectedResult()
    {
        HexagonalGrid<int> source = HexagonalGrid<int>.Zero;

        Assert.True(source.TryDistanceTo(new HexagonalGrid<int>(3, -1), out int distance));
        Assert.Equal(3, distance);
        Assert.False(new HexagonalGrid<int>(int.MaxValue, int.MaxValue).TryDistanceTo(
            new HexagonalGrid<int>(int.MinValue, int.MinValue), out distance));
        Assert.Equal(default, distance);
    }

    [Fact]
    public void RotateLeftAndRotateRight_WhenAppliedSixTimes_ShouldReturnOriginalCell()
    {
        HexagonalGrid<int> original = new HexagonalGrid<int>(2, -5);
        HexagonalGrid<int> rotatedLeft = original;
        HexagonalGrid<int> rotatedRight = original;

        for (int index = 0; index < 6; index++)
        {
            rotatedLeft = rotatedLeft.RotateLeft();
            rotatedRight = rotatedRight.RotateRight();
        }

        Assert.Equal(original, rotatedLeft);
        Assert.Equal(original, rotatedRight);
    }

    [Fact]
    public void TryRotate_WhenResultFitsOrOverflowsInt32_ShouldReturnExpectedResult()
    {
        HexagonalGrid<int> cell = new HexagonalGrid<int>(2, -5);

        Assert.True(cell.TryRotateLeft(out HexagonalGrid<int> rotatedLeft));
        Assert.Equal(cell.RotateLeft(), rotatedLeft);
        Assert.True(cell.TryRotateRight(out HexagonalGrid<int> rotatedRight));
        Assert.Equal(cell.RotateRight(), rotatedRight);
        Assert.False(new HexagonalGrid<int>(int.MaxValue, 1).TryRotateLeft(out _));
        Assert.False(new HexagonalGrid<int>(1, int.MaxValue).TryRotateRight(out _));
        Assert.False(new HexagonalGrid<int>(int.MinValue, int.MaxValue).TryRotateLeft(out _));
        Assert.False(new HexagonalGrid<int>(int.MaxValue, int.MinValue).TryRotateRight(out _));
    }

    [Fact]
    public void DirectionTo_WhenTargetIsAligned_ShouldReturnDirection()
    {
        HexagonalGrid<int> source = new HexagonalGrid<int>(2, -1);
        HexagonalGrid<int> target = new HexagonalGrid<int>(2, -4);

        Assert.True(source.TryGetDirectionTo(target, out HexagonalDirection direction));
        Assert.Equal(HexagonalDirection.PositiveS, direction);
        Assert.Equal(HexagonalDirection.PositiveS, source.DirectionTo(target));
    }

    [Fact]
    public void DirectionTo_WhenTargetIsNotAligned_ShouldThrowArgumentException()
    {
        HexagonalGrid<int> source = new HexagonalGrid<int>(0, 0);
        HexagonalGrid<int> target = new HexagonalGrid<int>(2, -1);

        Assert.False(source.TryGetDirectionTo(target, out _));
        Assert.Throws<ArgumentException>(() => source.DirectionTo(target));
    }

    [Fact]
    public void Ring_WhenRadiusIsTwo_ShouldReturnTwelveUniqueCellsAtDistanceTwo()
    {
        HexagonalGrid<int> center = new HexagonalGrid<int>(1, -2);
        HexagonalGrid<int>[] ring = [.. center.Ring(2)];

        Assert.Equal(HexagonalGridMetrics.CountInRing(2), ring.Length);
        Assert.Equal(ring.Length, ring.Distinct().Count());
        Assert.All(ring, cell => Assert.Equal(2, center.DistanceTo(cell)));
    }

    [Fact]
    public void Spiral_WhenRadiusIsTwo_ShouldReturnCenterAndAllCellsWithinRadius()
    {
        HexagonalGrid<int> center = new HexagonalGrid<int>(1, -2);
        HexagonalGrid<int>[] spiral = [.. center.Spiral(2)];

        Assert.Equal(HexagonalGridMetrics.CountInRange(2), spiral.Length);
        Assert.Equal(center, spiral[0]);
        Assert.Equal(spiral.Length, spiral.Distinct().Count());
        Assert.All(spiral, cell => Assert.InRange(center.DistanceTo(cell), 0, 2));
    }

    [Fact]
    public void CopyRangeTo_WhenDestinationIsLargeEnough_ShouldMatchSpiralEnumeration()
    {
        HexagonalGrid<int> center = new HexagonalGrid<int>(3, -3);
        HexagonalGrid<int>[] expected = [.. center.Spiral(2)];
        HexagonalGrid<int>[] destination = new HexagonalGrid<int>[expected.Length];

        int count = center.CopyRangeTo(2, destination);

        Assert.Equal(expected.Length, count);
        Assert.Equal(expected, destination);
    }

    [Fact]
    public void RingAndSpiral_WhenRadiusIsZero_ShouldReturnOnlyCenter()
    {
        HexagonalGrid<int> center = new HexagonalGrid<int>(3, -2);

        Assert.Equal([center], center.Ring(0));
        Assert.Equal([center], center.Spiral(0));
    }

    [Fact]
    public void CopyRingTo_WhenDestinationIsLargeEnough_ShouldMatchRingEnumeration()
    {
        HexagonalGrid<int> center = new HexagonalGrid<int>(3, -3);
        HexagonalGrid<int>[] expected = [.. center.Ring(2)];
        HexagonalGrid<int>[] destination = new HexagonalGrid<int>[expected.Length];

        int count = center.CopyRingTo(2, destination);

        Assert.Equal(expected.Length, count);
        Assert.Equal(expected, destination);
    }

    [Fact]
    public void RingOperations_WhenRadiusIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        HexagonalGrid<int> center = HexagonalGrid<int>.Zero;

        Assert.Throws<ArgumentOutOfRangeException>(() => center.Ring(-1).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => center.Spiral(-1).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => center.CopyRingTo(-1, new HexagonalGrid<int>[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => center.CopyRangeTo(-1, new HexagonalGrid<int>[1]));
    }

    [Fact]
    public void CopyNeighborsTo_WhenDestinationIsTooSmall_ShouldThrowArgumentException()
    {
        HexagonalGrid<int> cell = HexagonalGrid<int>.Zero;
        HexagonalGrid<int>[] destination = new HexagonalGrid<int>[5];

        Assert.Throws<ArgumentException>(() => cell.CopyNeighborsTo(destination));
    }

    [Fact]
    public void TryGetNeighbor_WhenResultWouldOverflowInt32_ShouldReturnFalse()
    {
        HexagonalGrid<int> cell = new HexagonalGrid<int>(int.MaxValue, 0);

        Assert.False(cell.TryGetNeighbor(HexagonalDirection.PositiveQ, out HexagonalGrid<int> neighbor));
        Assert.Equal(default, neighbor);
    }

    [Fact]
    public void DirectionAndNeighbor_WhenDirectionIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        HexagonalDirection direction = (HexagonalDirection)6;
        HexagonalGrid<int> cell = HexagonalGrid<int>.Zero;

        Assert.Throws<ArgumentOutOfRangeException>(() => HexagonalGrid<int>.Direction(direction));
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.Neighbor(direction));
        Assert.False(cell.TryGetNeighbor(direction, out _));
    }

    [Fact]
    public void Metrics_WhenRadiusIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => HexagonalGridMetrics.CountInRing(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => HexagonalGridMetrics.CountInRange(-1));
    }
}
