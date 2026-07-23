using System.Diagnostics.CodeAnalysis;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

public sealed class WorldPathfinderTests
{
    [Fact]
    public void TryFindPath_WhenContinuousEndpointsHaveDirectLineOfSight_ShouldReturnSingleSegment()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 9);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalWorldPoint start = new HexagonalWorldPoint(-0.2, 0.1);
        HexagonalWorldPoint goal = new HexagonalWorldPoint(4.6, 1.4);

        HexWorldPathfinder pathfinder = new HexWorldPathfinder(HexGridThetaStar.Instance);

        bool found = pathfinder.TryFindPath(
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
    public void TryFindPath_WhenCustomCostPolicyIsConfigured_ShouldUseItForDirectPathCost()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalWorldPoint start = layout.GetCenter(HexagonalCubePoint.Zero);
        HexagonalWorldPoint goal = layout.GetCenter(new HexagonalCubePoint(1, 0));
        HexWorldPathfinder pathfinder = new HexWorldPathfinder(
            HexGridThetaStar.Instance,
            new HexWorldPathfinderOptions { CostPolicy = DoubleDistanceCostPolicy.Instance });

        bool found = pathfinder.TryFindPath(
            snapshot,
            layout,
            start,
            goal,
            new HexagonalFootprint(0.25),
            out HexWorldPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.Equal(4, path.Cost, 12);
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

        HexWorldPathfinder pathfinder = new HexWorldPathfinder(HexGridThetaStar.Instance);

        bool found = pathfinder.TryFindPath(
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

    [Fact]
    public void TryFindPath_WhenCustomGridPathfinderIsSupplied_ShouldUseItForAnchoredSearch()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(3);
        map[new HexagonalCubePoint(1, 0)] = new HexNavigationCell(1, 1);
        HexGridCentralNavigationSnapshot snapshot = new HexGridCentralNavigationSnapshot(map, 0);
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalFootprint footprint = new HexagonalFootprint(0.25);
        RecordingGridPathfinder pathfinder = new RecordingGridPathfinder(HexGridThetaStar.Instance);

        HexWorldPathfinder worldPathfinder = new HexWorldPathfinder(pathfinder);

        bool found = worldPathfinder.TryFindPath(
            snapshot,
            layout,
            layout.GetCenter(HexagonalCubePoint.Zero),
            layout.GetCenter(new HexagonalCubePoint(3, 0)),
            footprint,
            out HexWorldPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.True(pathfinder.WasUsed);
    }

    [Fact]
    public void Constructor_WhenOptionsAreConfigured_ShouldExposeConfiguredDependencies()
    {
        HexWorldPathfinderOptions options = new HexWorldPathfinderOptions
        {
            AnchorSearchRadius = 2,
            AnchorSelection = HexWorldPathAnchorSelection.NearestWorldDistance,
            PathSmoothingMode = HexPathSmoothingMode.None
        };
        HexWorldPathfinder pathfinder = new HexWorldPathfinder(HexGridThetaStar.Instance, options);

        Assert.Same(HexGridThetaStar.Instance, pathfinder.GridPathfinder);
        Assert.Same(options, pathfinder.Options);
        Assert.Equal(2, pathfinder.Options.AnchorSearchRadius);
        Assert.Equal(HexWorldPathAnchorSelection.NearestWorldDistance, pathfinder.Options.AnchorSelection);
        Assert.Equal(HexPathSmoothingMode.None, pathfinder.Options.PathSmoothingMode);
    }

    [Fact]
    public void Constructor_WhenAnchorSearchRadiusIsLessThanOne_ShouldThrow()
    {
        HexWorldPathfinderOptions options = new HexWorldPathfinderOptions { AnchorSearchRadius = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new HexWorldPathfinder(HexGridThetaStar.Instance, options));
    }

    [Fact]
    public void TryFindPath_WhenMapVersionChanges_ShouldMarkPriorPathAsOutdated()
    {
        HexGridCentralNavigationMap map = new HexGridCentralNavigationMap(3);
        HexGridCentralNavigationSnapshot firstSnapshot = map.CaptureSnapshot();
        HexagonalLayout layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
        HexagonalWorldPoint start = layout.GetCenter(HexagonalCubePoint.Zero);
        HexagonalWorldPoint goal = layout.GetCenter(new HexagonalCubePoint(3, 0));
        HexWorldPathfinder pathfinder = new HexWorldPathfinder(HexGridThetaStar.Instance);

        bool found = pathfinder.TryFindPath(
            firstSnapshot,
            layout,
            start,
            goal,
            new HexagonalFootprint(0.25),
            out HexWorldPath? path);

        Assert.True(found);
        Assert.NotNull(path);
        Assert.True(path.IsCurrent(map.Version));
        Assert.True(map.TrySetCell(new HexagonalCubePoint(1, 0), new HexNavigationCell(1, 1)));
        Assert.False(path.IsCurrent(map.Version));

        HexGridCentralNavigationSnapshot secondSnapshot = map.CaptureSnapshot();
        Assert.True(HexLineOfSight.HasLineOfSight(
            firstSnapshot,
            layout,
            start,
            goal,
            new HexagonalFootprint(0.25)));
        Assert.False(HexLineOfSight.HasLineOfSight(
            secondSnapshot,
            layout,
            start,
            goal,
            new HexagonalFootprint(0.25)));
    }

    private sealed class RecordingGridPathfinder(IHexGridPathfinder inner) : IHexGridPathfinder
    {
        public bool WasUsed { get; private set; }

        public bool TryFindPath(
            IHexNavigationSnapshot snapshot,
            HexagonalLayout layout,
            HexagonalCubePoint start,
            HexagonalCubePoint goal,
            HexagonalFootprint footprint,
            [NotNullWhen(true)] out HexGridPath? path,
            double clearanceApothemScale = 0,
            IHexTraversalCostPolicy? costPolicy = null,
            HexPathfindingRequestOptions? requestOptions = null)
        {
            WasUsed = true;
            return inner.TryFindPath(
                snapshot,
                layout,
                start,
                goal,
                footprint,
                out path,
                clearanceApothemScale,
                costPolicy,
                requestOptions);
        }
    }

    private sealed class DoubleDistanceCostPolicy : IHexTraversalCostPolicy
    {
        public static DoubleDistanceCostPolicy Instance { get; } = new DoubleDistanceCostPolicy();

        private DoubleDistanceCostPolicy()
        {
        }

        public double MinimumCostPerUnitLength => 2;

        public double GetTraversalCost(double length, HexNavigationCell cell)
        {
            return length * 2;
        }
    }
}
