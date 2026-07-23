using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.MapProvider;

namespace Jarfter.Hexagonal.xUnit;

public sealed class HexagonalTests
{
    [Fact]
    public void HexagonalCubePoint_FromCube_WhenComponentsSumToZero_ShouldCreateEquivalentPoint()
    {
        HexagonalCubePoint point = HexagonalCubePoint.FromCube(3, -5, 2);

        Assert.Equal(new HexagonalCubePoint(3, -5), point);
        Assert.Equal(2, point.S);
        Assert.Equal(HexagonalCubePoint.Zero, HexagonalCubePoint.FromCube(0, 0, 0));
    }

    [Fact]
    public void HexagonalCubePoint_FromCube_WhenComponentsDoNotSumToZero_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => HexagonalCubePoint.FromCube(1, 2, 3));
    }

    [Fact]
    public void HexagonalCubePoint_OperatorsAndDistance_ShouldUseCubeCoordinateRules()
    {
        HexagonalCubePoint point = new HexagonalCubePoint(3, -5);
        HexagonalCubePoint other = new HexagonalCubePoint(-1, 4);

        Assert.Equal(point, +point);
        Assert.Equal(new HexagonalCubePoint(-3, 5), -point);
        Assert.Equal(new HexagonalCubePoint(2, -1), point + other);
        Assert.Equal(new HexagonalCubePoint(4, -9), point - other);
        Assert.Equal(new HexagonalCubePoint(6, -10), point * 2);
        Assert.Equal(new HexagonalCubePoint(6, -10), 2 * point);
        Assert.Equal(new HexagonalCubePoint(1, -2), point / 2);
        Assert.Equal(9, point.DistanceTo(other));
        Assert.Equal(point.DistanceTo(other), other.DistanceTo(point));
    }

    [Fact]
    public void HexagonalCubePoint_Neighbors_ShouldContainEachAdjacentPointOnce()
    {
        HexagonalCubePoint point = new HexagonalCubePoint(2, -1);
        List<HexagonalCubePoint> neighbors = [.. point.Neighbors];

        Assert.Equal(6, point.Neighbors.Count);
        Assert.Equal(6, neighbors.Count);
        Assert.Equal(6, neighbors.Distinct().Count());

        foreach (HexagonalCubePoint neighbor in neighbors)
        {
            Assert.Equal(1, point.DistanceTo(neighbor));
        }

        Assert.Equal(new HexagonalCubePoint(3, -1), point.NeighborAt(HexagonalCubePoint.Direction.PositiveQ));
        Assert.Equal(new HexagonalCubePoint(3, -2), point.NeighborAt(HexagonalCubePoint.Direction.NegativeR));
        Assert.Equal(new HexagonalCubePoint(2, -2), point.NeighborAt(HexagonalCubePoint.Direction.PositiveS));
        Assert.Equal(new HexagonalCubePoint(1, -1), point.NeighborAt(HexagonalCubePoint.Direction.NegativeQ));
        Assert.Equal(new HexagonalCubePoint(1, 0), point.NeighborAt(HexagonalCubePoint.Direction.PositiveR));
        Assert.Equal(new HexagonalCubePoint(2, 0), point.NeighborAt(HexagonalCubePoint.Direction.NegativeS));
        Assert.Throws<ArgumentOutOfRangeException>(() => point.NeighborAt((HexagonalCubePoint.Direction)(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => point.NeighborAt((HexagonalCubePoint.Direction)6));
    }

    [Fact]
    public void HexagonalCubePoint_RingAt_ShouldEnumerateUniquePointsAtRequestedDistance()
    {
        HexagonalCubePoint center = new HexagonalCubePoint(2, -1);

        for (int radius = 1; radius <= 4; radius++)
        {
            List<HexagonalCubePoint> ring = [.. center.RingAt(radius)];

            Assert.Equal(6 * radius, center.RingAt(radius).Count);
            Assert.Equal(6 * radius, ring.Count);
            Assert.Equal(ring.Count, ring.Distinct().Count());

            foreach (HexagonalCubePoint point in ring)
            {
                Assert.Equal(radius, center.DistanceTo(point));
            }
        }
    }

    [Fact]
    public void HexagonalCubePoint_RangeIn_ShouldEnumerateEveryPointWithinRadiusExactlyOnce()
    {
        HexagonalCubePoint center = new HexagonalCubePoint(2, -1);

        for (int radius = 0; radius <= 4; radius++)
        {
            List<HexagonalCubePoint> range = [.. center.RangeIn(radius)];
            int expectedCount = 1 + 3 * radius * (radius + 1);

            Assert.Equal(expectedCount, center.RangeIn(radius).Count);
            Assert.Equal(expectedCount, range.Count);
            Assert.Equal(expectedCount, range.Distinct().Count());
            Assert.Contains(center, range);

            foreach (HexagonalCubePoint point in range)
            {
                Assert.InRange(center.DistanceTo(point), 0, radius);
            }
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => center.RangeIn(-1));
    }

    [Fact]
    public void HexGridCentralProvider_WhenAccessingValidCoordinates_ShouldStoreAndRetrieveElements()
    {
        HexGridCentralProvider<string> map = new HexGridCentralProvider<string>(1);
        HexagonalCubePoint center = HexagonalCubePoint.Zero;
        HexagonalCubePoint edge = new HexagonalCubePoint(1, -1);

        map[center] = "center";
        map[edge] = "edge";

        Assert.Equal(7, map.Count);
        Assert.True(map.Contains(center));
        Assert.True(map.Contains(edge));
        Assert.Equal("center", map[center]);
        Assert.Equal("edge", map[edge]);
        Assert.Equal(7, map.Elements.Length);
        Assert.Equal("center", map.Elements[HexGridCentralProvider<string>.ToIndex(center)]);
        Assert.Equal("edge", map.Elements[HexGridCentralProvider<string>.ToIndex(edge)]);
    }

    [Fact]
    public void HexGridCentralProvider_WhenCoordinateIsOutsideRadius_ShouldReportMissingValue()
    {
        HexGridCentralProvider<string> map = new HexGridCentralProvider<string>(1);
        HexagonalCubePoint outside = new HexagonalCubePoint(2, -1);

        Assert.False(map.Contains(outside));
        Assert.False(map.TryGetValue(outside, out string? value));
        Assert.Null(value);
        Assert.Equal("fallback", map.GetValueOrDefault(outside, "fallback"));
    }

    [Fact]
    public void HexGridCentralProvider_WhenCoordinateIsPresent_ShouldReturnStoredValueFromQueries()
    {
        HexGridCentralProvider<string> map = new HexGridCentralProvider<string>(1);
        HexagonalCubePoint point = new HexagonalCubePoint(1, 0);
        map[point] = "value";

        Assert.True(map.TryGetValue(point, out string? value));
        Assert.Equal("value", value);
        Assert.Equal("value", map.GetValueOrDefault(point, "fallback"));
    }

    [Fact]
    public void IndexAndPoint_ShouldBeMutuallyInvertible()
    {
        for (int radius = 0; radius <= 100; radius++)
        {
            HexGridCentralProvider<int> map = new HexGridCentralProvider<int>(radius);

            // 验证 Index -> Point -> Index。
            for (int index = 0; index < map.Count; index++)
            {
                HexagonalCubePoint point = HexGridCentralProvider<int>.FromIndex(index);
                int actualIndex = HexGridCentralProvider<int>.ToIndex(point);

                Assert.Equal(index, actualIndex);
            }

            var usedIndexes = new HashSet<int>();

            // 验证所有合法 Point -> Index -> Point。
            for (int q = -radius; q <= radius; q++)
            {
                for (int r = -radius; r <= radius; r++)
                {
                    int s = -q - r;

                    if (Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(s))) > radius)
                    {
                        continue;
                    }

                    var point = new HexagonalCubePoint(q, r);

                    int index = HexGridCentralProvider<int>.ToIndex(point);
                    HexagonalCubePoint actualPoint = HexGridCentralProvider<int>.FromIndex(index);

                    Assert.Equal(point, actualPoint);

                    // 同时检查不同坐标不会映射到同一个索引。
                    Assert.True(
                        usedIndexes.Add(index),
                        $"Radius={radius}, Point={point}, Index={index}");
                }
            }

            Assert.Equal(map.Count, usedIndexes.Count);

            for (int index = 0; index < map.Count; index++)
            {
                Assert.Contains(index, usedIndexes);
            }
        }
    }
}
