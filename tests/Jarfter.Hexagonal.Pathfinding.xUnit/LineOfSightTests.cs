using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class LineOfSightTests
{
    [Fact]
    public void HasLineOfSight_WhenSegmentCrossesInflatedObstacle_ShouldReturnFalse()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(2);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool actual = HexLineOfSight.HasLineOfSight(
            snapshot,
            layout,
            new HexagonalWorldPoint(-4, 0),
            new HexagonalWorldPoint(4, 0),
            new HexagonalFootprint(0.25),
            0.25);

        Assert.False(actual);
    }

    [Fact]
    public void HasLineOfSight_WhenSegmentAvoidsAllObstacles_ShouldReturnTrue()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(2);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool actual = HexLineOfSight.HasLineOfSight(
            snapshot,
            layout,
            new HexagonalWorldPoint(-4, 2),
            new HexagonalWorldPoint(4, 2),
            new HexagonalFootprint(0.25));

        Assert.True(actual);
    }

    [Fact]
    public void HasLineOfSight_WhenObstacleIsBesideTraversedMainCell_ShouldCheckItConservatively()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(2);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool actual = HexLineOfSight.HasLineOfSight(
            snapshot,
            layout,
            new HexagonalWorldPoint(1, -2),
            new HexagonalWorldPoint(1, 2),
            new HexagonalFootprint(0.25),
            0.25);

        Assert.False(actual);
    }

    [Fact]
    public void TryGetTraversalCost_WhenLineCrossesTerrain_ShouldAccumulateLengthWeightedCost()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool traversable = HexLineOfSight.TryGetTraversalCost(
            snapshot,
            layout,
            layout.GetCenter(HexagonalCubePoint.Zero),
            layout.GetCenter(new HexagonalCubePoint(1, 0)),
            new HexagonalFootprint(0.25),
            out double cost);

        Assert.True(traversable);
        Assert.Equal(4, cost, 12);
    }

    [Fact]
    public void TryGetTraversalCost_WhenCustomCostPolicyIsSupplied_ShouldUseIt()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);

        bool traversable = HexLineOfSight.TryGetTraversalCost(
            snapshot,
            layout,
            layout.GetCenter(HexagonalCubePoint.Zero),
            layout.GetCenter(new HexagonalCubePoint(1, 0)),
            new HexagonalFootprint(0.25),
            out double cost,
            costPolicy: UnitDistanceCostPolicy.Instance);

        Assert.True(traversable);
        Assert.Equal(2, cost, 12);
    }

    private sealed class UnitDistanceCostPolicy : IHexTraversalCostPolicy
    {
        public static UnitDistanceCostPolicy Instance { get; } = new UnitDistanceCostPolicy();

        private UnitDistanceCostPolicy()
        {
        }

        public double MinimumCostPerUnitLength => 1;

        public double GetTraversalCost(double length, HexNavigationCell cell)
        {
            return length;
        }
    }
}
