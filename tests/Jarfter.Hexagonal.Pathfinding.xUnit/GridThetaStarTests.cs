using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class GridThetaStarTests
{
    [Fact]
    public void FindPath_WhenDirectLineIsTraversable_ShouldUseStartAndGoalAsWaypoints()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalCubePoint goal = new HexagonalCubePoint(2, 1);

        IHexGridPathfinder pathfinder = HexGridThetaStar.Instance;

        Assert.Same(HexGridThetaStar.Instance, pathfinder);

        HexGridPath? path = pathfinder.FindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            new HexagonalFootprint(0.25));
        HexGridPath? aStarPath = HexGridAStar.Instance.FindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            new HexagonalFootprint(0.25));

        Assert.NotNull(path);
        Assert.NotNull(aStarPath);
        Assert.Equal([HexagonalCubePoint.Zero, goal], path.Points.ToArray());
        Assert.True(path.Cost < aStarPath.Cost);
    }

    [Fact]
    public void FindPath_WhenDirectLineIsBlocked_ShouldReturnCollisionFreeWaypointSegments()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);
        HexagonalCubePoint goal = new HexagonalCubePoint(3, 0);

        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            footprint);

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
    public void FindPath_WhenEndpointHasObstacle_ShouldReturnNull()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.FlatTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(1, 0),
            new HexagonalFootprint(0.25));

        Assert.Null(path);
    }

    [Fact]
    public void FindPath_WhenExpandedNodeBudgetIsExhausted_ShouldReturnNull()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions { MaximumExpandedNodeCount = 1 });

        Assert.Null(path);
    }

    [Fact]
    public void FindPath_WhenRequestIsCancelled_ShouldThrow()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        using CancellationTokenSource cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
        {
            HexGridThetaStar.Instance.FindPath(
                snapshot,
                new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
                HexagonalCubePoint.Zero,
                new HexagonalCubePoint(1, 0),
                new HexagonalFootprint(0.25),
                requestOptions: new HexPathfindingRequestOptions { CancellationToken = cancellationSource.Token });
        });
    }

    [Fact]
    public void FindPath_WhenSearchTimesOut_ShouldReturnNull()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions { Timeout = TimeSpan.FromTicks(1) });

        Assert.Null(path);
    }

    [Fact]
    public void FindPath_WhenStatisticsAreRequested_ShouldAttachSearchWorkload()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(2, 1),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions { CollectStatistics = true });

        Assert.NotNull(path);
        HexPathfindingStatistics statistics = Assert.IsType<HexPathfindingStatistics>(path.Statistics);
        Assert.True(statistics.ExpandedNodeCount > 0);
        Assert.True(statistics.LineOfSightQueryCount > 0);
        Assert.True(statistics.ParentLineOfSightQueryCount > 0);
        Assert.True(statistics.SuccessfulParentLineOfSightQueryCount > 0);
        Assert.True(statistics.TraversedCellCount > 0);
        Assert.True(statistics.NearbyCellQueryCount > 0);
    }

    [Fact]
    public void FindPath_WhenLineOfSightCacheIsEnabled_ShouldPreservePath()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);

        HexGridPath? uncachedPath = HexGridThetaStar.Instance.FindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            footprint,
            requestOptions: new HexPathfindingRequestOptions
            {
                LineOfSightCacheMode = HexLineOfSightCacheMode.Disabled
            });
        HexGridPath? cachedPath = HexGridThetaStar.Instance.FindPath(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            footprint,
            requestOptions: new HexPathfindingRequestOptions
            {
                CollectStatistics = true,
                LineOfSightCacheMode = HexLineOfSightCacheMode.Enabled
            });

        Assert.NotNull(uncachedPath);
        Assert.NotNull(cachedPath);
        Assert.Equal(uncachedPath.Points.ToArray(), cachedPath.Points.ToArray());
        Assert.Equal(uncachedPath.Cost, cachedPath.Cost, 12);
        HexPathfindingStatistics statistics = Assert.IsType<HexPathfindingStatistics>(cachedPath.Statistics);
        Assert.True(statistics.LineOfSightCacheMissCount > 0);
    }
}
