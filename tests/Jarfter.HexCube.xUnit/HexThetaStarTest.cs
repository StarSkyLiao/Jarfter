using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;
using Xunit;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证 Theta* 能够使用六边形区域障碍物移除不必要的网格拐点.
/// </summary>
public sealed class HexThetaStarTest
{
    /// <summary>
    /// 验证无障碍物时 Theta* 返回起点和终点构成的直接路径.
    /// </summary>
    [Fact]
    public void FindPath_WhenDirectLineIsClear_ShouldReturnDirectPath()
    {
        HexCubeGridPoint start = new HexCubeGridPoint(0, 0);
        HexCubeGridPoint goal = new HexCubeGridPoint(6, -2);
        TestNavigationProvider provider = new TestNavigationProvider(new HashSet<HexCubeGridPoint>());
        IPathfinder pathfinder = new IPathfinder.ThetaStar(IHeuristic.Euclidean.Instance, provider);

        PathfindingResult result = pathfinder.FindPath(start, goal);

        Assert.True(result.IsSuccess);
        Assert.Equal([start, goal], result.Path);
        Assert.Equal(new HexCubeLine2D(start, goal).Length, result.TotalCost);
    }

    /// <summary>
    /// 验证直接线段穿过障碍物时 Theta* 会返回可通行的绕行路径.
    /// </summary>
    [Fact]
    public void FindPath_WhenDirectLineIsBlocked_ShouldReturnDetour()
    {
        HexCubeGridPoint start = new HexCubeGridPoint(0, 0);
        HexCubeGridPoint goal = new HexCubeGridPoint(6, -2);
        TestNavigationProvider provider = new TestNavigationProvider(new HashSet<HexCubeGridPoint> { new HexCubeGridPoint(3, -1) });
        IPathfinder pathfinder = new IPathfinder.ThetaStar(IHeuristic.Euclidean.Instance, provider);

        PathfindingResult result = pathfinder.FindPath(start, goal);
        IReadOnlyList<HexCubeGridPoint> path = result.Path;

        Assert.True(result.IsSuccess);
        Assert.True(path.Count > 2);

        for (int index = 1; index < path.Count; index++)
        {
            Assert.True(provider.HasLineOfSight(new HexCubeLine2D(path[index - 1], path[index])));
        }
    }

    /// <summary>
    /// 验证无障碍物时 Lazy Theta* 返回起点和终点构成的直接路径.
    /// </summary>
    [Fact]
    public void LazyThetaStar_FindPath_WhenDirectLineIsClear_ShouldReturnDirectPath()
    {
        HexCubeGridPoint start = new HexCubeGridPoint(0, 0);
        HexCubeGridPoint goal = new HexCubeGridPoint(6, -2);
        TestNavigationProvider provider = new TestNavigationProvider(new HashSet<HexCubeGridPoint>());
        IPathfinder pathfinder = new IPathfinder.LazyThetaStar(IHeuristic.Euclidean.Instance, provider);

        PathfindingResult result = pathfinder.FindPath(start, goal);

        Assert.True(result.IsSuccess);
        Assert.Equal([start, goal], result.Path);
        Assert.Equal(new HexCubeLine2D(start, goal).Length, result.TotalCost);
    }

    /// <summary>
    /// 验证直接线段穿过障碍物时 Lazy Theta* 会在延迟验证后返回可通行的绕行路径.
    /// </summary>
    [Fact]
    public void LazyThetaStar_FindPath_WhenDirectLineIsBlocked_ShouldReturnDetour()
    {
        HexCubeGridPoint start = new HexCubeGridPoint(0, 0);
        HexCubeGridPoint goal = new HexCubeGridPoint(6, -2);
        TestNavigationProvider provider = new TestNavigationProvider(new HashSet<HexCubeGridPoint> { new HexCubeGridPoint(3, -1) });
        IPathfinder pathfinder = new IPathfinder.LazyThetaStar(IHeuristic.Euclidean.Instance, provider);

        PathfindingResult result = pathfinder.FindPath(start, goal);
        IReadOnlyList<HexCubeGridPoint> path = result.Path;

        Assert.True(result.IsSuccess);
        Assert.True(path.Count > 2);

        for (int index = 1; index < path.Count; index++)
        {
            Assert.True(provider.HasLineOfSight(new HexCubeLine2D(path[index - 1], path[index])));
        }
    }

    /// <summary>
    /// 验证可见直达线段穿过高成本区域时, Theta* 会保留代价更低的网格绕行路径.
    /// </summary>
    [Fact]
    public void FindPath_WhenDirectLineHasHigherCost_ShouldKeepCheaperDetour()
    {
        HexCubeGridPoint start = new HexCubeGridPoint(0, 0);
        HexCubeGridPoint goal = new HexCubeGridPoint(2, 0);
        HexCubeArea2D highCostArea = new HexCubeArea2D(new HexCubePoint(1, 0), 0.25);
        TestNavigationProvider provider = new TestNavigationProvider(new HashSet<HexCubeGridPoint>(), highCostArea);
        IPathfinder pathfinder = new IPathfinder.ThetaStar(IHeuristic.Euclidean.Instance, provider);

        PathfindingResult result = pathfinder.FindPath(start, goal);
        IReadOnlyList<HexCubeGridPoint> path = result.Path;

        Assert.True(path.Count > 2);
        Assert.True(result.IsSuccess);
        Assert.True(result.TotalCost < provider.GetLineCost(new HexCubeLine2D(start, goal)));
    }

    private sealed class TestNavigationProvider(IReadOnlySet<HexCubeGridPoint> obstacles, HexCubeArea2D? highCostArea = null) : IThetaStarNavigationProvider
    {
        /// <inheritdoc />
        public double GetMoveCost(HexCubeGridPoint destination)
        {
            if (highCostArea is { } area && area.Contains(destination)) return 3;
            return obstacles.Contains(destination) ? -1 : 1;
        }

        /// <inheritdoc />
        public bool HasLineOfSight(HexCubeLine2D line)
        {
            foreach (HexCubeGridPoint obstacle in obstacles)
            {
                HexCubeArea2D area = new HexCubeArea2D(obstacle, 1);
                if (area.IntersectsHex(line)) return false;
            }

            return true;
        }

        /// <inheritdoc />
        public double GetLineCost(HexCubeLine2D line)
        {
            return highCostArea is { } area && area.IntersectsHex(line) ? line.Length + 100 : line.Length;
        }
    }
}
