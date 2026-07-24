using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;

namespace Jarfter.Hexagonal.xUnit.Geometry;

public sealed class HexagonalGeometryTests
{
    [Fact]
    public void TraverseSegment_WhenCrossingPointyTopCenters_ShouldReturnEachMainCellInOrder()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        List<HexagonalSegmentCell> cells =
        [
            .. HexagonalGeometry.TraverseSegment(
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
            .. HexagonalGeometry.TraverseSegment(
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
        List<HexagonalSegmentCell> cells = [.. HexagonalGeometry.TraverseSegment(layout, point, point)];

        HexagonalSegmentCell cell = Assert.Single(cells);

        Assert.Equal(new HexagonalCubePoint(-2, 1), cell.Point);
        Assert.Equal(0, cell.StartFraction);
        Assert.Equal(1, cell.EndFraction);
    }

    [Fact]
    public void SegmentIntersectsHexagon_WhenSegmentPassesThroughHexagon_ShouldReturnTrue()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        bool actual = HexagonalGeometry.SegmentIntersectsHexagon(
            layout,
            new HexagonalWorldPoint(-2, 0),
            new HexagonalWorldPoint(2, 0),
            HexagonalCubePoint.Zero,
            1);

        Assert.True(actual);
    }

    [Fact]
    public void SegmentIntersectsHexagon_WhenSegmentTouchesBoundary_ShouldReturnTrue()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool actual = HexagonalGeometry.SegmentIntersectsHexagon(
            layout,
            new HexagonalWorldPoint(1, -2),
            new HexagonalWorldPoint(1, 2),
            HexagonalCubePoint.Zero,
            1);

        Assert.True(actual);
    }

    [Fact]
    public void SegmentIntersectsHexagon_WhenSegmentMissesFlatTopHexagon_ShouldReturnFalse()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.FlatTop, 1);

        bool actual = HexagonalGeometry.SegmentIntersectsHexagon(
            layout,
            new HexagonalWorldPoint(-2, 1.01),
            new HexagonalWorldPoint(2, 1.01),
            HexagonalCubePoint.Zero,
            1);

        Assert.False(actual);
    }

    [Fact]
    public void SegmentIntersectsHexagon_WhenApothemScaleIsZero_ShouldReturnFalse()
    {
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool actual = HexagonalGeometry.SegmentIntersectsHexagon(
            layout,
            new HexagonalWorldPoint(-2, 0),
            new HexagonalWorldPoint(2, 0),
            HexagonalCubePoint.Zero,
            0);

        Assert.False(actual);
    }
}
