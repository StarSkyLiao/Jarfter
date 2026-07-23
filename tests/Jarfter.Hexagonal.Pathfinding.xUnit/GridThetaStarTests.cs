using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class GridThetaStarTests
{
    [Fact]
    public void TryFindPath_WhenDirectLineIsTraversable_ShouldUseStartAndGoalAsWaypoints()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalCubePoint goal = new HexagonalCubePoint(2, 1);

        IHexGridPathfinder pathfinder = HexGridThetaStar.Instance;

        Assert.Same(HexGridThetaStar.Instance, pathfinder);

        bool found = pathfinder.TryFindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            new HexagonalFootprint(0.25),
            out HexGridPath? path);
        bool aStarFound = HexGridAStar.TryFindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            out HexGridPath? aStarPath);

        Assert.True(found);
        Assert.True(aStarFound);
        Assert.NotNull(path);
        Assert.NotNull(aStarPath);
        Assert.Equal([HexagonalCubePoint.Zero, goal], path.Points.ToArray());
        Assert.True(path.Cost < aStarPath.Cost);
    }

    [Fact]
    public void TryFindPath_WhenDirectLineIsBlocked_ShouldReturnCollisionFreeWaypointSegments()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);
        HexagonalCubePoint goal = new HexagonalCubePoint(3, 0);

        bool found = HexGridThetaStar.Instance.TryFindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint,
            out HexGridPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.True(path.Points.Length > 2);
        Assert.DoesNotContain(new HexagonalCubePoint(1, 0), path.Points.ToArray());

        for (int index = 1; index < path.Points.Length; index++)
        {
            Assert.True(HexLineOfSight.HasLineOfSight(
                snapshot,
                layout,
                layout.GetCenter(path.Points[index - 1]),
                layout.GetCenter(path.Points[index]),
                footprint));
        }
    }

    [Fact]
    public void TryFindPath_WhenEndpointHasObstacle_ShouldReturnFalse()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        bool found = HexGridThetaStar.Instance.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.FlatTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(1, 0),
            new HexagonalFootprint(0.25),
            out HexGridPath? path);

        Assert.False(found);
        Assert.Null(path);
    }

    [Fact]
    public void TryFindPath_WhenExpandedNodeBudgetIsExhausted_ShouldReturnFalse()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        bool found = HexGridThetaStar.Instance.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25),
            out HexGridPath? path,
            requestOptions: new HexPathfindingRequestOptions { MaximumExpandedNodeCount = 1 });

        Assert.False(found);
        Assert.Null(path);
    }

    [Fact]
    public void TryFindPath_WhenRequestIsCancelled_ShouldThrow()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        using CancellationTokenSource cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.Throws<OperationCanceledException>(() => HexGridThetaStar.Instance.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(1, 0),
            new HexagonalFootprint(0.25),
            out _,
            requestOptions: new HexPathfindingRequestOptions { CancellationToken = cancellationSource.Token }));
    }

    [Fact]
    public void TryFindPath_WhenSearchTimesOut_ShouldReturnFalse()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        bool found = HexGridThetaStar.Instance.TryFindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25),
            out HexGridPath? path,
            requestOptions: new HexPathfindingRequestOptions { Timeout = TimeSpan.FromTicks(1) });

        Assert.False(found);
        Assert.Null(path);
    }
}
