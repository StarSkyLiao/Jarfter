using Jarfter.Hexagonal.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 表示在连续二维平面中供移动对象跟随的路径.
/// 航点按起点到终点顺序排列, 首尾始终是调用方指定的真实世界坐标.
/// </summary>
public sealed class HexWorldPath
{
    private readonly HexagonalWorldPoint[] m_Waypoints;

    /// <summary>
    /// 使用已按起点到终点排序的航点数组、累计成本和导航地图版本初始化连续路径.
    /// </summary>
    /// <param name="waypoints">按起点到终点顺序排列的连续世界坐标航点.</param>
    /// <param name="cost">路径累计移动成本.</param>
    /// <param name="navigationVersion">计算路径时使用的导航地图版本.</param>
    internal HexWorldPath(HexagonalWorldPoint[] waypoints, double cost, long navigationVersion)
    {
        m_Waypoints = waypoints;
        Cost = cost;
        NavigationVersion = navigationVersion;
    }

    /// <summary>
    /// 获取连续世界坐标航点, 顺序为真实起点到真实终点.
    /// </summary>
    public ReadOnlySpan<HexagonalWorldPoint> Waypoints => m_Waypoints;

    /// <summary>
    /// 获取路径的累计移动成本.
    /// </summary>
    public double Cost { get; }

    /// <summary>
    /// 获取计算此路径时使用的导航地图版本.
    /// 调用方可在跟随路径前或动态地图更新后与当前版本比较.
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
