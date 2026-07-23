using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.MapProvider;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 表示从中心六边形稠密地图复制得到的不可变导航快照.
/// 快照保存连续数组副本, 适用于中小规模、半径已知的有限地图.
/// </summary>
public sealed class HexGridCentralNavigationSnapshot : IHexNavigationSnapshot
{
    private readonly HexNavigationCell[] m_Cells;

    /// <summary>
    /// 从指定中心稠密地图创建导航快照.
    /// 源地图后续的写入不会影响此快照.
    /// </summary>
    /// <param name="map">要复制的中心六边形导航地图.</param>
    /// <param name="version">创建此快照时的导航地图版本, 必须为非负数.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="map"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="version"/> 为负数时抛出.</exception>
    public HexGridCentralNavigationSnapshot(HexGridCentralProvider<HexNavigationCell> map, long version)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentOutOfRangeException.ThrowIfNegative(version);

        Radius = map.Radius;
        Version = version;
        m_Cells = map.Elements.ToArray();
    }

    /// <summary>
    /// 获取中心六边形区域的半径.
    /// </summary>
    public int Radius { get; }

    /// <summary>
    /// 获取快照中包含的格子数量.
    /// </summary>
    public int Count => m_Cells.Length;

    /// <inheritdoc />
    public long Version { get; }

    /// <inheritdoc />
    public bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell)
    {
        if (HexagonalCubePoint.Zero.DistanceTo(point) > Radius)
        {
            cell = default;
            return false;
        }

        cell = m_Cells[HexGridCentralProvider<HexNavigationCell>.ToIndex(point)];
        return true;
    }
}
