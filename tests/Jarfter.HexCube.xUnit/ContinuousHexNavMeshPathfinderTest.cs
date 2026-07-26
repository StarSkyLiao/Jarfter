using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding.Continuous;
using Xunit;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证规则六边形 NavMesh 寻路器的统一代价路径规划.
/// </summary>
public sealed class ContinuousHexNavMeshPathfinderTest
{
    /// <summary>
    /// 验证障碍物阻断直线时, NavMesh 会返回由多个可通行线段构成的绕行路径.
    /// </summary>
    [Fact]
    public void FindPath_WhenObstacleBlocksDirectLine_ShouldReturnPassableDetour()
    {
        ContinuousNavigationSnapshot snapshot = new ContinuousNavigationSnapshot(
            1,
            [new HexCubeArea2D(HexCubePoint.Zero, 0.4)],
            []);
        IContinuousPathfinder pathfinder = new ContinuousHexNavMeshPathfinder(
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 5)));
        ContinuousPathRequest request = new ContinuousPathRequest(new HexCubePoint(-3, 0), new HexCubePoint(3, 0), Clearance: 0);

        ContinuousPathResult result = pathfinder.FindPath(request, snapshot);

        Assert.True(result.IsSuccess);
        Assert.True(result.Path.Count > 2);

        for (int index = 1; index < result.Path.Count; index++)
        {
            Assert.True(snapshot.HasLineOfSight(new HexCubeLine2D(result.Path[index - 1], result.Path[index]), request.AgentRadius, request.Clearance));
        }
    }

    /// <summary>
    /// 验证高代价单元会引导 NavMesh 搜索绕开直穿中心区域的路线.
    /// </summary>
    [Fact]
    public void FindPath_WhenHighCostAreaBlocksShortRoute_ShouldReturnCheaperDetour()
    {
        ContinuousNavigationSnapshot snapshot = new ContinuousNavigationSnapshot(
            1,
            [],
            [new ContinuousTraversalArea(new HexCubeArea2D(HexCubePoint.Zero, 0.5), 10)]);
        IContinuousPathfinder pathfinder = new ContinuousHexNavMeshPathfinder(
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 5)));
        ContinuousPathRequest request = new ContinuousPathRequest(new HexCubePoint(-3, 0), new HexCubePoint(3, 0), Clearance: 0);
        HexCubeLine2D directLine = new HexCubeLine2D(request.Start, request.Goal);

        ContinuousPathResult result = pathfinder.FindPath(request, snapshot);

        Assert.True(result.IsSuccess);
        Assert.True(result.TotalCost < snapshot.GetLineCost(directLine));
    }

    /// <summary>
    /// 验证重复查询同一高代价地图时, 有向边代价缓存不会改变路径及其精确总代价.
    /// </summary>
    [Fact]
    public void FindPath_WhenRepeatedOnSameHighCostSnapshot_ShouldReturnIdenticalResult()
    {
        ContinuousNavigationSnapshot snapshot = new ContinuousNavigationSnapshot(
            1,
            [new HexCubeArea2D(HexCubePoint.Zero, 0.4)],
            [new ContinuousTraversalArea(new HexCubeArea2D(new HexCubePoint(0, 1), 0.8), 4)]);
        IContinuousPathfinder pathfinder = new ContinuousHexNavMeshPathfinder(
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 5)));
        ContinuousPathRequest request = new ContinuousPathRequest(new HexCubePoint(-3, 0), new HexCubePoint(3, 0), Clearance: 0);

        ContinuousPathResult firstResult = pathfinder.FindPath(request, snapshot);
        ContinuousPathResult secondResult = pathfinder.FindPath(request, snapshot);

        Assert.True(firstResult.IsSuccess);
        Assert.Equal(firstResult.TotalCost, secondResult.TotalCost);
        Assert.Equal(firstResult.Path, secondResult.Path);
    }

    /// <summary>
    /// 验证半径为 1 的单位能够穿过连续空间中实际可通行、但规则 NavMesh 单元无法连通的窄通道.
    /// </summary>
    [Fact]
    public void FindPath_WhenRegularNavMeshCannotConnectNarrowRoute_ShouldUseLocalRefinement()
    {
        IContinuousNavigationSnapshot snapshot = ContinuousHexNavMeshRunTest.Snapshot;
        IContinuousPathfinder pathfinder = new ContinuousHexNavMeshPathfinder(
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 32)));
        ContinuousPathRequest request = ContinuousHexNavMeshRunTest.Request with
        {
            AgentRadius = 1,
            Clearance = 1e-9
        };

        ContinuousPathResult result = pathfinder.FindPath(request, snapshot);

        Assert.True(result.IsSuccess);

        for (int index = 1; index < result.Path.Count; index++)
        {
            Assert.True(snapshot.HasLineOfSight(
                new HexCubeLine2D(result.Path[index - 1], result.Path[index]),
                request.AgentRadius,
                request.Clearance));
        }
    }

    /// <summary>
    /// 验证较小单位的候选空间包含较大单位的候选空间, 不会因局部细化拓扑变化而返回更高代价的路径.
    /// </summary>
    [Fact]
    public void FindPath_WhenAgentRadiusDecreases_ShouldNotIncreaseTotalCost()
    {
        IContinuousNavigationSnapshot snapshot = ContinuousHexNavMeshRunTest.Snapshot;
        IContinuousPathfinder pathfinder = new ContinuousHexNavMeshPathfinder(
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 32)));
        ContinuousPathRequest largerRequest = ContinuousHexNavMeshRunTest.Request with
        {
            AgentRadius = 1,
            Clearance = 1e-9
        };
        ContinuousPathRequest smallerRequest = largerRequest with { AgentRadius = 0.6 };

        ContinuousPathResult largerResult = pathfinder.FindPath(largerRequest, snapshot);
        ContinuousPathResult smallerResult = pathfinder.FindPath(smallerRequest, snapshot);

        Assert.True(largerResult.IsSuccess);
        Assert.True(smallerResult.IsSuccess);
        Assert.True(smallerResult.TotalCost <= largerResult.TotalCost);
    }

}
