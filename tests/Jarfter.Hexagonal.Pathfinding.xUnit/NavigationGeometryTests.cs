using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class NavigationGeometryTests
{
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
