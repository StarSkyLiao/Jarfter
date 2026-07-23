using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class GridAStarTests
{
    [Fact]
    public void FindPath_WhenStraightRouteIsTraversable_ShouldReturnLowestCostPath()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25));

        Assert.NotNull(path);
        Assert.Equal(6, path.Cost, 12);
        Assert.Equal(
            [
                new HexagonalCubePoint(0, 0),
                new HexagonalCubePoint(1, 0),
                new HexagonalCubePoint(2, 0),
                new HexagonalCubePoint(3, 0)
            ],
            path.Points.ToArray());
    }

    [Fact]
    public void FindPath_WhenStraightRouteContainsObstacle_ShouldFindDetour()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25));

        Assert.NotNull(path);
        Assert.True(path.Cost > 6);
        Assert.DoesNotContain(new HexagonalCubePoint(1, 0), path.Points.ToArray());

        for (int index = 1; index < path.Points.Length; index++)
        {
            Assert.Equal(1, path.Points[index - 1].DistanceTo(path.Points[index]));
        }
    }

    [Fact]
    public void FindPath_WhenDirectTerrainIsExpensive_ShouldPreferLowerCostDetour()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(2);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.FlatTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(2, 0),
            new HexagonalFootprint(0.25));

        Assert.NotNull(path);
        Assert.Equal(6, path.Cost, 12);
        Assert.DoesNotContain(new HexagonalCubePoint(1, 0), path.Points.ToArray());
    }

    [Fact]
    public void FindPath_WhenEndpointHasObstacle_ShouldReturnNull()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(1, 0),
            new HexagonalFootprint(0.25));

        Assert.Null(path);
    }

    [Fact]
    public void FindPath_WhenCustomCostPolicyIgnoresTerrain_ShouldUseShortestRoute()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(2);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 7);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.FlatTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(2, 0),
            new HexagonalFootprint(0.25),
            costPolicy: UnitDistanceCostPolicy.Instance);

        Assert.NotNull(path);
        Assert.Equal(4, path.Cost, 12);
        Assert.Equal(7, path.NavigationVersion);
        Assert.True(path.IsCurrent(7));
        Assert.False(path.IsCurrent(8));
        Assert.Contains(new HexagonalCubePoint(1, 0), path.Points.ToArray());
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
