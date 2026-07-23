using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class WorldThetaStarTests
{
    [Fact]
    public void TryFindPath_WhenContinuousEndpointsHaveDirectLineOfSight_ShouldReturnSingleSegment()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 9);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalWorldPoint start = new HexagonalWorldPoint(-0.2, 0.1);
        HexagonalWorldPoint goal = new HexagonalWorldPoint(4.6, 1.4);

        bool found = HexWorldThetaStar.TryFindPath(
            snapshot,
            layout,
            start,
            goal,
            new HexagonalFootprint(0.25),
            out HexWorldPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.Equal([start, goal], path.Waypoints.ToArray());
        Assert.Equal(start.DistanceTo(goal), path.Cost, 12);
        Assert.Equal(9, path.NavigationVersion);
    }

    [Fact]
    public void TryFindPath_WhenContinuousEndpointsAreBlockedByObstacle_ShouldReturnWaypointPath()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);
        HexagonalWorldPoint start = layout.GetCenter(HexagonalCubePoint.Zero);
        HexagonalWorldPoint goal = layout.GetCenter(new HexagonalCubePoint(3, 0));

        bool found = HexWorldThetaStar.TryFindPath(
            snapshot,
            layout,
            start,
            goal,
            footprint,
            out HexWorldPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.Equal(start, path.Waypoints[0]);
        Assert.Equal(goal, path.Waypoints[^1]);
        Assert.True(path.Waypoints.Length > 2);

        for (int index = 1; index < path.Waypoints.Length; index++)
        {
            Assert.True(HexLineOfSight.HasLineOfSight(
                snapshot,
                layout,
                path.Waypoints[index - 1],
                path.Waypoints[index],
                footprint));
        }
    }
}
