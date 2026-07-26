using Jarfter.HexCube.Numerics;
using Xunit;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证整数六边形网格坐标的转换和拓扑运算.
/// </summary>
public sealed class HexCubeGridPointTest
{
    /// <summary>
    /// 验证整数网格坐标可隐式转换为相同位置的几何坐标.
    /// </summary>
    [Fact]
    public void Conversion_WhenGridPointIsConvertedToGeometryPoint_ShouldKeepCoordinates()
    {
        HexCubeGridPoint gridPoint = new HexCubeGridPoint(2, -3);

        HexCubePoint point = gridPoint;

        Assert.Equal(new HexCubePoint(2, -3), point);
    }

    /// <summary>
    /// 验证网格中心几何坐标可显式转换为相同位置的整数网格坐标.
    /// </summary>
    [Fact]
    public void Conversion_WhenGeometryPointIsGridCenter_ShouldCreateGridPoint()
    {
        HexCubePoint point = new HexCubePoint(2, -3);

        HexCubeGridPoint gridPoint = (HexCubeGridPoint)point;

        Assert.Equal(new HexCubeGridPoint(2, -3), gridPoint);
    }

    /// <summary>
    /// 验证分数几何坐标不能通过显式转换截断为整数网格坐标.
    /// </summary>
    [Fact]
    public void Conversion_WhenGeometryPointIsNotGridCenter_ShouldThrow()
    {
        HexCubePoint point = new HexCubePoint(2.5, -3);

        Assert.Throws<ArgumentException>(() => _ = (HexCubeGridPoint)point);
    }

    /// <summary>
    /// 验证几何坐标可按分量向下或向上取整为整数网格坐标.
    /// </summary>
    [Fact]
    public void Rounding_WhenFloorOrCeilIsRequested_ShouldRoundComponentsInRequestedDirection()
    {
        HexCubePoint point = new HexCubePoint(2.3, -3.7);

        Assert.Equal(new HexCubeGridPoint(2, -4), point.AsFloor());
        Assert.Equal(new HexCubeGridPoint(3, -3), point.AsCeil());
    }

    /// <summary>
    /// 验证几何坐标舍入时会修正立方坐标约束, 得到最近的网格中心.
    /// </summary>
    [Fact]
    public void Rounding_WhenGridCenterIsRequested_ShouldKeepCubeCoordinateConstraint()
    {
        HexCubePoint point = new HexCubePoint(0.4, 0.4);

        HexCubeGridPoint gridPoint = point.AsRound();

        Assert.Equal(new HexCubeGridPoint(0, 1), gridPoint);
        Assert.Equal(0, gridPoint.Q + gridPoint.R + gridPoint.S);
    }

    /// <summary>
    /// 验证整数网格坐标的邻居和环坐标保持正确的拓扑距离.
    /// </summary>
    [Fact]
    public void Topology_WhenGettingNeighborsAndRing_ShouldKeepExpectedDistance()
    {
        HexCubeGridPoint point = new HexCubeGridPoint(2, -3);

        Assert.Equal(6, point.Neighbors.Count);
        Assert.All(point.Neighbors, neighbor => Assert.Equal(1, point.HexDistanceTo(neighbor)));
        Assert.Equal(12, point.RingAt(2).Count);
        Assert.All(point.RingAt(2), ringPoint => Assert.Equal(2, point.HexDistanceTo(ringPoint)));
    }
}
