using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding.Continuous;
using Xunit;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证连续 NavMesh 导航边界的基础几何语义.
/// </summary>
public sealed class ContinuousNavigationBoundsTest
{
    /// <summary>
    /// 验证边界能够判断任意位置和完全位于凸边界内的线段.
    /// </summary>
    [Fact]
    public void Contains_WhenPositionAndLineAreInside_ShouldReturnTrue()
    {
        ContinuousNavigationBounds bounds = new ContinuousNavigationBounds(new HexCubeArea2D(new HexCubePoint(1, -1), 4));

        Assert.True(bounds.Contains(new HexCubePoint(1.5, -1.5)));
        Assert.True(bounds.Contains(new HexCubeLine2D(new HexCubePoint(0, -1), new HexCubePoint(2, -1))));
        Assert.False(bounds.Contains(new HexCubePoint(6, -1)));
        Assert.False(bounds.Contains(new HexCubeLine2D(new HexCubePoint(0, -1), new HexCubePoint(6, -1))));
    }

    /// <summary>
    /// 验证边界拒绝非有限坐标和非正半径.
    /// </summary>
    [Fact]
    public void Constructor_WhenShapeIsInvalid_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContinuousNavigationBounds(new HexCubeArea2D(new HexCubePoint(double.NaN, 0), 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 0)));
    }
}
