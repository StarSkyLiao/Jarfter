using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;
using Xunit;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证 A* 启发函数权重的取值规则.
/// </summary>
public sealed class HexAStarWeightTest
{
    /// <summary>
    /// 验证小于 1 的正权重可用于降低启发函数强度.
    /// </summary>
    [Fact]
    public void FindPath_WhenHeuristicWeightIsPositiveAndLessThanOne_ShouldFindPath()
    {
        IPathfinder pathfinder = new IPathfinder.AStar(IHeuristic.Default.Instance, new PassableMoveCostProvider(), 0.5);

        PathfindingResult result = pathfinder.FindPath(HexCubeGridPoint.Zero, new HexCubeGridPoint(1, 0));

        Assert.True(result.IsSuccess);
        Assert.Equal([HexCubeGridPoint.Zero, new HexCubeGridPoint(1, 0)], result.Path);
    }

    /// <summary>
    /// 验证非正权重和非有限权重不能创建寻路器.
    /// </summary>
    [Fact]
    public void Constructor_WhenHeuristicWeightIsNotFinitePositive_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(static () => new IPathfinder.AStar(IHeuristic.Default.Instance, new PassableMoveCostProvider(), 0));
        Assert.Throws<ArgumentOutOfRangeException>(static () => new IPathfinder.AStar(IHeuristic.Default.Instance, new PassableMoveCostProvider(), double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(static () => new IPathfinder.AStar(IHeuristic.Default.Instance, new PassableMoveCostProvider(), double.PositiveInfinity));
    }

    private sealed class PassableMoveCostProvider : IMoveCostProvider
    {
        /// <inheritdoc />
        public double GetMoveCost(HexCubeGridPoint destination) => 1;
    }
}
