using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class GridAStarTests
{
    [Fact]
    public void TryFindPath_WhenStraightRouteIsTraversable_ShouldReturnLowestCostPath()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        bool found = HexGridAStar.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            out HexGridPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.Equal(6, path.Cost);
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
    public void TryFindPath_WhenStraightRouteContainsObstacle_ShouldFindDetour()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        bool found = HexGridAStar.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            out HexGridPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.True(path.Cost > 6);
        Assert.DoesNotContain(new HexagonalCubePoint(1, 0), path.Points.ToArray());

        for (int index = 1; index < path.Points.Length; index++)
        {
            Assert.Equal(1, path.Points[index - 1].DistanceTo(path.Points[index]));
        }
    }

    [Fact]
    public void TryFindPath_WhenDirectTerrainIsExpensive_ShouldPreferLowerCostDetour()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(2);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        bool found = HexGridAStar.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.FlatTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(2, 0),
            out HexGridPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.Equal(6, path.Cost);
        Assert.DoesNotContain(new HexagonalCubePoint(1, 0), path.Points.ToArray());
    }

    [Fact]
    public void TryFindPath_WhenEndpointHasObstacle_ShouldReturnFalse()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        bool found = HexGridAStar.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(1, 0),
            out HexGridPath? path);

        Assert.False(found);
        Assert.Null(path);
    }
}
