using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 表示以六边形格心为节点的离散路径.
/// 路径坐标按从起点到终点的顺序排列, 相邻节点可以是直接可见但不相邻的格心.
/// 总成本使用布局的连续平面长度和地形倍率计算.
/// </summary>
public sealed class HexGridPath
{
    private readonly HexagonalCubePoint[] m_Points;

    /// <summary>
    /// 使用已按起点到终点排序的坐标数组、累计成本和导航地图版本初始化离散路径.
    /// </summary>
    /// <param name="points">按起点到终点顺序排列的格心坐标数组.</param>
    /// <param name="cost">路径累计移动成本.</param>
    /// <param name="navigationVersion">计算路径时使用的导航地图版本.</param>
    internal HexGridPath(HexagonalCubePoint[] points, double cost, long navigationVersion)
    {
        m_Points = points;
        Cost = cost;
        NavigationVersion = navigationVersion;
    }

    /// <summary>
    /// 获取路径中的格心坐标, 顺序为起点到终点.
    /// </summary>
    public ReadOnlySpan<HexagonalCubePoint> Points => m_Points;

    /// <summary>
    /// 获取路径的累计移动成本.
    /// </summary>
    public double Cost { get; }

    /// <summary>
    /// 获取计算此路径时使用的导航地图版本.
    /// </summary>
    public long NavigationVersion { get; }

    /// <summary>
    /// 判断此路径是否仍基于指定的导航地图版本.
    /// </summary>
    /// <param name="navigationVersion">要比较的当前导航地图版本.</param>
    /// <returns>当版本与 <see cref="NavigationVersion"/> 相同时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="navigationVersion"/> 为负数时抛出.</exception>
    public bool IsCurrent(long navigationVersion)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(navigationVersion);
        return NavigationVersion == navigationVersion;
    }
}
