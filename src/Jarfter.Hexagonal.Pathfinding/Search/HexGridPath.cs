using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 表示以六边形格心为节点的离散路径.
/// 路径坐标按从起点到终点的顺序排列, 总成本使用布局的连续平面长度和地形倍率计算.
/// </summary>
public sealed class HexGridPath
{
    private readonly HexagonalCubePoint[] m_Points;

    /// <summary>
    /// 使用已按起点到终点排序的坐标数组和累计成本初始化离散路径.
    /// </summary>
    /// <param name="points">按起点到终点顺序排列的格心坐标数组.</param>
    /// <param name="cost">路径累计移动成本.</param>
    internal HexGridPath(HexagonalCubePoint[] points, double cost)
    {
        m_Points = points;
        Cost = cost;
    }

    /// <summary>
    /// 获取路径中的格心坐标, 顺序为起点到终点.
    /// </summary>
    public ReadOnlySpan<HexagonalCubePoint> Points => m_Points;

    /// <summary>
    /// 获取路径的累计移动成本.
    /// </summary>
    public double Cost { get; }
}
