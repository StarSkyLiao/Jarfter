using Jarfter.Hexagonal.Direction;

namespace Jarfter.Hexagonal.xUnit;

public sealed class HexagonalDirectionTests
{
    [Fact]
    public void Opposite_WhenDirectionIsValid_ShouldReturnDirectionThreeStepsAway()
    {
        Assert.Equal(HexagonalDirection.NegativeQ, HexagonalDirection.PositiveQ.Opposite());
        Assert.Equal(HexagonalDirection.PositiveR, HexagonalDirection.NegativeR.Opposite());
        Assert.Equal(HexagonalDirection.PositiveS, HexagonalDirection.NegativeS.Opposite());
    }

    [Fact]
    public void RotateLeft_WhenDirectionIsLast_ShouldWrapToFirstDirection()
    {
        Assert.Equal(HexagonalDirection.PositiveQ, HexagonalDirection.NegativeS.RotateLeft());
    }

    [Fact]
    public void RotateRight_WhenDirectionIsFirst_ShouldWrapToLastDirection()
    {
        Assert.Equal(HexagonalDirection.NegativeS, HexagonalDirection.PositiveQ.RotateRight());
    }

    [Fact]
    public void ToCompassDirection_WhenOrientationIsPointyTop_ShouldUsePointyTopMapping()
    {
        Assert.Equal(HexagonalCompassDirection.East, HexagonalDirection.PositiveQ.ToCompassDirection(HexagonalOrientation.PointyTop));
        Assert.Equal(HexagonalCompassDirection.NorthEast, HexagonalDirection.NegativeR.ToCompassDirection(HexagonalOrientation.PointyTop));
        Assert.Equal(HexagonalCompassDirection.NorthWest, HexagonalDirection.PositiveS.ToCompassDirection(HexagonalOrientation.PointyTop));
        Assert.Equal(HexagonalCompassDirection.West, HexagonalDirection.NegativeQ.ToCompassDirection(HexagonalOrientation.PointyTop));
        Assert.Equal(HexagonalCompassDirection.SouthWest, HexagonalDirection.PositiveR.ToCompassDirection(HexagonalOrientation.PointyTop));
        Assert.Equal(HexagonalCompassDirection.SouthEast, HexagonalDirection.NegativeS.ToCompassDirection(HexagonalOrientation.PointyTop));
    }

    [Fact]
    public void ToCompassDirection_WhenOrientationIsFlatTop_ShouldUseFlatTopMapping()
    {
        Assert.Equal(HexagonalCompassDirection.SouthEast, HexagonalDirection.PositiveQ.ToCompassDirection(HexagonalOrientation.FlatTop));
        Assert.Equal(HexagonalCompassDirection.NorthEast, HexagonalDirection.NegativeR.ToCompassDirection(HexagonalOrientation.FlatTop));
        Assert.Equal(HexagonalCompassDirection.North, HexagonalDirection.PositiveS.ToCompassDirection(HexagonalOrientation.FlatTop));
        Assert.Equal(HexagonalCompassDirection.NorthWest, HexagonalDirection.NegativeQ.ToCompassDirection(HexagonalOrientation.FlatTop));
        Assert.Equal(HexagonalCompassDirection.SouthWest, HexagonalDirection.PositiveR.ToCompassDirection(HexagonalOrientation.FlatTop));
        Assert.Equal(HexagonalCompassDirection.South, HexagonalDirection.NegativeS.ToCompassDirection(HexagonalOrientation.FlatTop));
    }

    [Fact]
    public void TryToHexagonalDirection_WhenCompassDirectionIsUnavailable_ShouldReturnFalse()
    {
        Assert.False(HexagonalCompassDirection.North.TryToHexagonalDirection(
            HexagonalOrientation.PointyTop,
            out HexagonalDirection pointyTopDirection));
        Assert.Equal(default, pointyTopDirection);

        Assert.False(HexagonalCompassDirection.East.TryToHexagonalDirection(
            HexagonalOrientation.FlatTop,
            out HexagonalDirection flatTopDirection));
        Assert.Equal(default, flatTopDirection);
    }

    [Fact]
    public void ToHexagonalDirection_WhenCompassDirectionMatchesOrientation_ShouldRoundTripFromHexDirection()
    {
        foreach (HexagonalDirection direction in HexagonalDirectionExtensions.All)
        {
            HexagonalCompassDirection pointyTopCompass = direction.ToCompassDirection(HexagonalOrientation.PointyTop);
            HexagonalCompassDirection flatTopCompass = direction.ToCompassDirection(HexagonalOrientation.FlatTop);

            Assert.Equal(direction, pointyTopCompass.ToHexagonalDirection(HexagonalOrientation.PointyTop));
            Assert.Equal(direction, flatTopCompass.ToHexagonalDirection(HexagonalOrientation.FlatTop));
        }
    }

    [Fact]
    public void DirectionExtensions_WhenDirectionIsInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        HexagonalDirection direction = (HexagonalDirection)6;

        Assert.Throws<ArgumentOutOfRangeException>(() => direction.Opposite());
        Assert.Throws<ArgumentOutOfRangeException>(() => direction.RotateLeft());
        Assert.Throws<ArgumentOutOfRangeException>(() => direction.RotateRight());
        Assert.Throws<ArgumentOutOfRangeException>(() => direction.ToCompassDirection(HexagonalOrientation.PointyTop));
    }
}
