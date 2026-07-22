using Jarfter.Hexagonal.Direction;
using Jarfter.Hexagonal.Grid;
using Jarfter.Hexagonal.Layout;

namespace Jarfter.Hexagonal.xUnit;

public sealed class HexagonalLayoutTests
{
    private const double Tolerance = 0.000000001;

    [Fact]
    public void Constructor_WhenSizeComponentIsNotPositive_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexagonalLayout(
            HexagonalOrientation.PointyTop,
            new HexagonalPoint(0, 1),
            HexagonalPoint.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(() => new HexagonalLayout(
            HexagonalOrientation.PointyTop,
            new HexagonalPoint(1, 0),
            HexagonalPoint.Zero));
    }

    [Fact]
    public void Constructor_WhenOrientationIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexagonalLayout(
            (HexagonalOrientation)2,
            new HexagonalPoint(1, 1),
            HexagonalPoint.Zero));
    }

    [Fact]
    public void Constructor_WhenSizeOrOriginIsNotFinite_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexagonalLayout(
            HexagonalOrientation.PointyTop,
            new HexagonalPoint(double.NaN, 1),
            HexagonalPoint.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexagonalLayout(
            HexagonalOrientation.PointyTop,
            new HexagonalPoint(1, double.PositiveInfinity),
            HexagonalPoint.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HexagonalLayout(
            HexagonalOrientation.PointyTop,
            new HexagonalPoint(1, 1),
            new HexagonalPoint(double.NegativeInfinity, 0)));
    }

    [Fact]
    public void ToFractionalGrid_WhenPointWasCreatedFromPointyTopCell_ShouldRoundTrip()
    {
        HexagonalLayout layout = HexagonalLayout.CreatePointyTop(10, new HexagonalPoint(5, -3));
        HexagonalGrid<int> cell = new HexagonalGrid<int>(2, -1);

        HexagonalGrid<double> fractionalCell = layout.ToFractionalGrid(layout.ToPoint(cell));

        AssertClose(cell.Q, fractionalCell.Q);
        AssertClose(cell.R, fractionalCell.R);
        Assert.Equal(cell, layout.ToRoundedGrid(layout.ToPoint(cell)));
    }

    [Fact]
    public void ToFractionalGrid_WhenPointWasCreatedFromFlatTopCell_ShouldRoundTrip()
    {
        HexagonalLayout layout = HexagonalLayout.CreateFlatTop(8, new HexagonalPoint(-2, 7));
        HexagonalGrid<int> cell = new HexagonalGrid<int>(-3, 4);

        HexagonalGrid<double> fractionalCell = layout.ToFractionalGrid(layout.ToPoint(cell));

        AssertClose(cell.Q, fractionalCell.Q);
        AssertClose(cell.R, fractionalCell.R);
        Assert.Equal(cell, layout.ToRoundedGrid(layout.ToPoint(cell)));
    }

    [Fact]
    public void ToRoundedGrid_WhenPointIsNotFiniteOrResultOverflows_ShouldThrow()
    {
        HexagonalLayout layout = HexagonalLayout.CreatePointyTop(1, HexagonalPoint.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.ToRoundedGrid(new HexagonalPoint(double.NaN, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.ToRoundedGrid(new HexagonalPoint(0, double.PositiveInfinity)));
        Assert.Throws<OverflowException>(() => layout.ToRoundedGrid(new HexagonalPoint(double.MaxValue, 0)));
    }

    [Fact]
    public void ToPoint_WhenFlatTopCellMovesInPositiveQDirection_ShouldMoveSouthEast()
    {
        HexagonalLayout layout = HexagonalLayout.CreateFlatTop(2, HexagonalPoint.Zero);

        HexagonalPoint point = layout.ToPoint(HexagonalGrid<int>.Direction(HexagonalDirection.PositiveQ));

        AssertClose(3, point.X);
        AssertClose(Math.Sqrt(3), point.Y);
    }

    [Fact]
    public void CopyCornersTo_WhenDestinationIsLargeEnough_ShouldWriteSixCornersAroundCenter()
    {
        HexagonalLayout layout = HexagonalLayout.CreateFlatTop(2, HexagonalPoint.Zero);
        HexagonalPoint center = new HexagonalPoint(10, -5);
        HexagonalPoint[] corners = new HexagonalPoint[6];

        layout.CopyCornersTo(center, corners);

        Assert.Equal(center + layout.GetCornerOffset(0), corners[0]);
        Assert.Equal(center + layout.GetCornerOffset(5), corners[5]);
        Assert.Equal(6, corners.Distinct().Count());
    }

    [Fact]
    public void Corners_WhenDirectlyEnumerated_ShouldUseValueTypeEnumerableAndMatchCopyResult()
    {
        HexagonalLayout layout = HexagonalLayout.CreateFlatTop(2, HexagonalPoint.Zero);
        HexagonalGrid<int> cell = new HexagonalGrid<int>(1, -1);
        HexagonalLayout.CornerEnumerable<int> corners = layout.Corners(cell);
        HexagonalLayout.CornerEnumerable<int>.Enumerator enumerator = corners.GetEnumerator();
        HexagonalPoint[] expected = new HexagonalPoint[6];

        layout.CopyCornersTo(cell, expected);

        Assert.Equal(expected, corners);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(expected[0], enumerator.Current);
    }

    [Fact]
    public void GetCornerOffset_WhenCornerIndexIsOutsideRange_ShouldThrowArgumentOutOfRangeException()
    {
        HexagonalLayout layout = HexagonalLayout.CreatePointyTop(1, HexagonalPoint.Zero);

        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetCornerOffset(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetCornerOffset(6));
    }

    [Fact]
    public void CopyCornersTo_WhenDestinationIsTooSmall_ShouldThrowArgumentException()
    {
        HexagonalLayout layout = HexagonalLayout.CreatePointyTop(1, HexagonalPoint.Zero);
        HexagonalPoint[] destination = new HexagonalPoint[5];

        Assert.Throws<ArgumentException>(() => layout.CopyCornersTo(HexagonalPoint.Zero, destination));
    }

    private static void AssertClose(double expected, double actual)
    {
        Assert.InRange(actual, expected - Tolerance, expected + Tolerance);
    }
}
