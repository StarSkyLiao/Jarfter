using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Grid;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Grid;
using Jarfter.Hexagonal.Pathfinding.Grid.Algorithms;
using Jarfter.Hexagonal.Pathfinding.Grid.Requests;
using Jarfter.Hexagonal.Pathfinding.Grid.Results;
using Jarfter.Hexagonal.Pathfinding.Grid.Runtime;
using Jarfter.Hexagonal.Pathfinding.Navigation.Central;
using Jarfter.Hexagonal.Pathfinding.Navigation.Model;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class GridSearchScopeTests
{
    [Fact]
    public void GetMaximumDistanceSum_WhenUsingExpandingDetour_ShouldExpandThenRemoveBound()
    {
        IHexPathSearchScopeStrategy strategy = HexPathSearchScopeStrategies.ExpandingDetour;

        Assert.Equal(4, strategy.GetMaximumDistanceSum(2, 0));
        Assert.Equal(8, strategy.GetMaximumDistanceSum(2, 1));
        Assert.Equal(16, strategy.GetMaximumDistanceSum(2, 2));
        Assert.Null(strategy.GetMaximumDistanceSum(2, 3));
    }

    [Fact]
    public void FindPath_WhenFirstScopeCannotDetour_ShouldFallBackToNextScope()
    {
        HexGridCentral<HexNavigationCell> map = CreateBlockedShortRouteMap();
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        RecordingScopeStrategy strategy = new RecordingScopeStrategy(2, null);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(2, 0),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions
            {
                CollectStatistics = true,
                SearchScopeStrategy = strategy
            });

        Assert.NotNull(path);
        Assert.Equal(2, strategy.CallCount);
        Assert.DoesNotContain(new HexagonalCubePoint(1, 0), path.Points.ToArray());
        Assert.True(Assert.IsType<HexPathfindingStatistics>(path.Statistics).ExpandedNodeCount > 0);
    }

    [Fact]
    public void FindPath_WhenSearchScopeIsEnabled_ShouldApplyNodeBudgetAcrossAllAttempts()
    {
        HexGridCentral<HexNavigationCell> map = CreateBlockedShortRouteMap();
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        RecordingScopeStrategy strategy = new RecordingScopeStrategy(2, null);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(2, 0),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions
            {
                MaximumExpandedNodeCount = 4,
                SearchScopeStrategy = strategy
            });

        Assert.Null(path);
        Assert.Equal(2, strategy.CallCount);
    }

    [Fact]
    public void FindPathWithWorkspace_WhenFirstScopeCannotDetour_ShouldFallBackToNextScope()
    {
        HexGridCentral<HexNavigationCell> map = CreateBlockedShortRouteMap();
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexGridPathfindingWorkspace workspace = new HexGridPathfindingWorkspace(snapshot);
        RecordingScopeStrategy strategy = new RecordingScopeStrategy(2, null);

        HexGridPath? path = HexGridAStar.Instance.FindPath(
            snapshot,
            workspace,
            new HexagonalLayout(HexagonalOrientation.PointyTop, 1),
            HexagonalCubePoint.Zero,
            new HexagonalCubePoint(2, 0),
            new HexagonalFootprint(0.25),
            requestOptions: new HexPathfindingRequestOptions { SearchScopeStrategy = strategy });

        Assert.NotNull(path);
        Assert.Equal(2, strategy.CallCount);
        Assert.DoesNotContain(new HexagonalCubePoint(1, 0), path.Points.ToArray());
    }

    private static HexGridCentral<HexNavigationCell> CreateBlockedShortRouteMap()
    {
        HexGridCentral<HexNavigationCell> map = new HexGridCentral<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 1);
        return map;
    }

    private sealed class RecordingScopeStrategy(params int?[] maximumDistanceSums) : IHexPathSearchScopeStrategy
    {
        public int CallCount { get; private set; }

        public int? GetMaximumDistanceSum(int directDistance, int attemptIndex)
        {
            CallCount++;
            return maximumDistanceSums[attemptIndex];
        }
    }
}
