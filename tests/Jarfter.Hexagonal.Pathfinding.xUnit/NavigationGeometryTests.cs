using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class NavigationGeometryTests
{
    [Fact]
    public void TraverseSegment_WhenCrossingPointyTopCenters_ShouldReturnEachMainCellInOrder()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        List<HexagonalSegmentCell> cells =
        [
            .. HexNavigationGeometry.TraverseSegment(
                layout,
                HexagonalWorldPoint.Zero,
                new HexagonalWorldPoint(6, 0))
        ];

        Assert.Equal(
            [
                new HexagonalCubePoint(0, 0),
                new HexagonalCubePoint(1, 0),
                new HexagonalCubePoint(2, 0),
                new HexagonalCubePoint(3, 0)
            ],
            cells.Select(static cell => cell.Point));
        Assert.Equal(0, cells[0].StartFraction, 12);
        Assert.Equal(1, cells[^1].EndFraction, 12);
    }

    [Fact]
    public void TraverseSegment_WhenCrossingFlatTopCenters_ShouldReturnEachMainCellInOrder()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.FlatTop, 1);
        List<HexagonalSegmentCell> cells =
        [
            .. HexNavigationGeometry.TraverseSegment(
                layout,
                HexagonalWorldPoint.Zero,
                new HexagonalWorldPoint(0, 6))
        ];

        Assert.Equal(
            [
                new HexagonalCubePoint(0, 0),
                new HexagonalCubePoint(0, 1),
                new HexagonalCubePoint(0, 2),
                new HexagonalCubePoint(0, 3)
            ],
            cells.Select(static cell => cell.Point));
    }

    [Fact]
    public void TraverseSegment_WhenSegmentHasNoLength_ShouldReturnContainingCellOnce()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalWorldPoint point = layout.GetCenter(new HexagonalCubePoint(-2, 1));
        List<HexagonalSegmentCell> cells = [.. HexNavigationGeometry.TraverseSegment(layout, point, point)];

        HexagonalSegmentCell cell = Assert.Single(cells);

        Assert.Equal(new HexagonalCubePoint(-2, 1), cell.Point);
        Assert.Equal(0, cell.StartFraction);
        Assert.Equal(1, cell.EndFraction);
    }

    [Fact]
    public void SegmentIntersectsInflatedHexagon_WhenSegmentPassesThroughObstacle_ShouldReturnTrue()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);

        bool actual = HexNavigationGeometry.SegmentIntersectsInflatedHexagon(
            layout,
            new HexagonalWorldPoint(-2, 0),
            new HexagonalWorldPoint(2, 0),
            HexagonalCubePoint.Zero,
            0.5,
            footprint,
            0.25);

        Assert.True(actual);
    }

    [Fact]
    public void SegmentIntersectsInflatedHexagon_WhenSegmentTouchesBoundary_ShouldReturnTrue()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool actual = HexNavigationGeometry.SegmentIntersectsInflatedHexagon(
            layout,
            new HexagonalWorldPoint(1, -2),
            new HexagonalWorldPoint(1, 2),
            HexagonalCubePoint.Zero,
            0.5,
            new HexagonalFootprint(0.25),
            0.25);

        Assert.True(actual);
    }

    [Fact]
    public void SegmentIntersectsInflatedHexagon_WhenSegmentMissesFlatTopObstacle_ShouldReturnFalse()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.FlatTop, 1);

        bool actual = HexNavigationGeometry.SegmentIntersectsInflatedHexagon(
            layout,
            new HexagonalWorldPoint(-2, 1.01),
            new HexagonalWorldPoint(2, 1.01),
            HexagonalCubePoint.Zero,
            0.5,
            new HexagonalFootprint(0.25),
            0.25);

        Assert.False(actual);
    }

    [Fact]
    public void SegmentIntersectsInflatedHexagon_WhenObstacleScaleIsZero_ShouldReturnFalse()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool actual = HexNavigationGeometry.SegmentIntersectsInflatedHexagon(
            layout,
            new HexagonalWorldPoint(-2, 0),
            new HexagonalWorldPoint(2, 0),
            HexagonalCubePoint.Zero,
            0,
            HexagonalFootprint.Unit);

        Assert.False(actual);
    }
}
