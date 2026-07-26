using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding.Continuous;
using Xunit;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证规则六边形 NavMesh 的基础单元构建逻辑.
/// </summary>
public sealed class ContinuousHexNavMeshTest
{
    /// <summary>
    /// 验证无障碍地图会生成包含中心位置的可通行单元.
    /// </summary>
    [Fact]
    public void Build_WhenMapHasNoObstacles_ShouldCreateCenterCell()
    {
        ContinuousHexNavMesh navMesh = ContinuousHexNavMesh.Build(
            ContinuousNavigationSnapshot.Empty,
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 4)),
            0,
            0);

        Assert.NotEmpty(navMesh.Cells);
        Assert.True(navMesh.TryGetContainingCellIndex(HexCubePoint.Zero, out _));
    }

    /// <summary>
    /// 验证与扩大后障碍物相交的单元不会被纳入 NavMesh.
    /// </summary>
    [Fact]
    public void Build_WhenObstacleBlocksCenter_ShouldExcludeCenterCell()
    {
        ContinuousNavigationSnapshot snapshot = new ContinuousNavigationSnapshot(
            1,
            [new HexCubeArea2D(HexCubePoint.Zero, 0.4)],
            []);

        ContinuousHexNavMesh navMesh = ContinuousHexNavMesh.Build(
            snapshot,
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 4)),
            0,
            0);

        Assert.False(navMesh.TryGetContainingCellIndex(HexCubePoint.Zero, out _));
    }

    /// <summary>
    /// 验证位置位于两个单元的共享边时, 即使最近单元被障碍物移除, 仍可定位到相邻的可通行单元.
    /// </summary>
    [Fact]
    public void TryGetContainingCellIndex_WhenNearestCellIsBlockedAtSharedEdge_ShouldReturnAdjacentPassableCell()
    {
        ContinuousNavigationSnapshot snapshot = new ContinuousNavigationSnapshot(
            1,
            [new HexCubeArea2D(HexCubePoint.Zero, 0.4)],
            []);
        ContinuousHexNavMesh navMesh = ContinuousHexNavMesh.Build(
            snapshot,
            new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 4)),
            0,
            0);

        bool found = navMesh.TryGetContainingCellIndex(new HexCubePoint(0.5, 0), out int cellIndex);

        Assert.True(found);
        Assert.NotEqual(HexCubePoint.Zero, navMesh.Cells[cellIndex].Position);
    }
}
