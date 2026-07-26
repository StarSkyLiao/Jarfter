using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;
using Xunit;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证默认 Theta* 导航数据提供程序的地图快照和几何规则.
/// </summary>
public sealed class HexGridThetaStarNavigationProviderTest
{
    /// <summary>
    /// 验证构造后修改源地图不会影响已创建的导航快照.
    /// </summary>
    [Fact]
    public void Constructor_WhenSourceMapChanges_ShouldKeepOriginalSnapshot()
    {
        HexGridCentral<HexNavigationCell> map = new HexGridCentral<HexNavigationCell>(1);
        map.InitializeCell(new HexNavigationCell());
        HexCubePoint destination = new HexCubePoint(1, 0);
        HexGridThetaStarNavigationProvider provider = new HexGridThetaStarNavigationProvider(map);

        map[destination] = new HexNavigationCell(3, 1);

        Assert.Equal(1, provider.GetMoveCost(destination));
        Assert.True(provider.UsesUniformTraversalCost);
        HexCubeLine2D line = new HexCubeLine2D(HexCubePoint.Zero, destination);
        Assert.True(provider.TryGetLineCost(line, out double cost));
        Assert.Equal(line.Length, cost);
    }

    /// <summary>
    /// 验证障碍物单元会同时阻断进入该单元和穿过该单元的直线.
    /// </summary>
    [Fact]
    public void Constructor_WhenCellIsObstacle_ShouldBlockMoveAndLineOfSight()
    {
        HexGridCentral<HexNavigationCell> map = new HexGridCentral<HexNavigationCell>(2);
        map.InitializeCell(new HexNavigationCell());
        HexCubePoint obstacle = new HexCubePoint(1, 0);
        map[obstacle] = new HexNavigationCell(1, 1);
        HexGridThetaStarNavigationProvider provider = new HexGridThetaStarNavigationProvider(map);

        Assert.Equal(-1, provider.GetMoveCost(obstacle));
        Assert.False(provider.TryGetLineCost(new HexCubeLine2D(HexCubePoint.Zero, new HexCubePoint(2, 0)), out _));
    }

}
