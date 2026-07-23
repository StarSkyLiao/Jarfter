using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class GridThetaStarTests
{
    [Fact]
    public async Task FindPathAsync_WhenDirectLineIsTraversable_ShouldUseStartAndGoalAsWaypoints()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalCubePoint goal = new HexagonalCubePoint(2, 1);

        IHexGridPathfinder pathfinder = HexGridThetaStar.Instance;

        Assert.Same(HexGridThetaStar.Instance, pathfinder);

        HexGridPath? path = await pathfinder.FindPathAsync(
            snapshot,
            layout,
            HexagonalCubePoint.Zero,
            goal,
            new HexagonalFootprint(0.25));
        HexGridPath? aStarPath = await HexGridAStar.Instance.FindPathAsync(
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
    public async Task FindPathAsync_WhenDirectLineIsBlocked_ShouldReturnCollisionFreeWaypointSegments()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);
        HexagonalCubePoint goal = new HexagonalCubePoint(3, 0);

        HexGridPath? path = await HexGridThetaStar.Instance.FindPathAsync(
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
    public async Task FindPathAsync_WhenEndpointHasObstacle_ShouldReturnNull()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        map[HexagonalCubePoint.Zero] = new HexNavigationCell(1, 0.5);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = await HexGridThetaStar.Instance.FindPathAsync(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.FlatTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(1, 0),
            new HexagonalFootprint(0.25));

        Assert.Null(path);
    }

    [Fact]
    public async Task FindPathAsync_WhenExpandedNodeBudgetIsExhausted_ShouldReturnNull()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = await HexGridThetaStar.Instance.FindPathAsync(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions { MaximumExpandedNodeCount = 1 });

        Assert.Null(path);
    }

    [Fact]
    public async Task FindPathAsync_WhenRequestIsCancelled_ShouldThrow()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        using CancellationTokenSource cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await HexGridThetaStar.Instance.FindPathAsync(
                snapshot,
                new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
                HexagonalCubePoint.Zero,
                new HexagonalCubePoint(1, 0),
                new HexagonalFootprint(0.25),
                requestOptions: new HexPathfindingRequestOptions { CancellationToken = cancellationSource.Token });
        });
    }

    [Fact]
    public async Task FindPathAsync_WhenSearchTimesOut_ShouldReturnNull()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);

        HexGridPath? path = await HexGridThetaStar.Instance.FindPathAsync(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(3, 0),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions { Timeout = TimeSpan.FromTicks(1) });

        Assert.Null(path);
    }
}
