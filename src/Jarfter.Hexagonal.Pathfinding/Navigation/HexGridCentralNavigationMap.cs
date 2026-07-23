using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.MapProvider;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 提供可更新的中心稠密导航地图, 并在需要寻路时生成版本化的不可变快照.
/// 所有读写与快照复制使用同一把锁, 因此后台寻路只读取快照而不会与地图更新竞争.
/// </summary>
public sealed class HexGridCentralNavigationMap
{
    private readonly Lock m_SyncRoot = new Lock();
    private readonly HexGridCentralProvider<HexNavigationCell> m_Cells;
    private readonly HexGridCentralNavigationBake m_Bake;
    private long m_Version;

    /// <summary>
    /// 使用指定半径创建初始版本为 0 的可更新导航地图.
    /// </summary>
    /// <param name="radius">中心六边形区域的非负半径.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="radius"/> 为负数时抛出.</exception>
    public HexGridCentralNavigationMap(int radius)
    {
        m_Cells = new HexGridCentralProvider<HexNavigationCell>(radius);
        m_Bake = new HexGridCentralNavigationBake(radius);
    }

    /// <summary>
    /// 获取中心六边形区域的半径.
    /// </summary>
    public int Radius => m_Cells.Radius;

    /// <summary>
    /// 获取当前可变地图的版本. 每次实际格子数据变化时递增.
    /// </summary>
    public long Version
    {
        get
        {
            lock (m_SyncRoot)
            {
                return m_Version;
            }
        }
    }

    /// <summary>
    /// 尝试读取当前可变地图中的导航格子数据.
    /// </summary>
    /// <param name="point">要读取的格子坐标.</param>
    /// <param name="cell">读取成功时得到的导航格子数据.</param>
    /// <returns>当坐标位于地图范围内时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    public bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell)
    {
        lock (m_SyncRoot)
        {
            return m_Cells.TryGetValue(point, out cell);
        }
    }

    /// <summary>
    /// 尝试更新指定格子的导航数据. 仅当数据实际变化时递增地图版本.
    /// </summary>
    /// <param name="point">要更新的格子坐标.</param>
    /// <param name="cell">新的导航格子数据.</param>
    /// <returns>当坐标位于地图范围内且数据发生变化时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    public bool TrySetCell(HexagonalCubePoint point, HexNavigationCell cell)
    {
        lock (m_SyncRoot)
        {
            if (!m_Cells.TryGetValue(point, out HexNavigationCell currentCell))
            {
                return false;
            }

            if (currentCell == cell)
            {
                return false;
            }

            m_Cells[point] = cell;
            m_Version++;
            return true;
        }
    }

    /// <summary>
    /// 复制当前地图数据并创建可供并发寻路读取的不可变快照.
    /// </summary>
    /// <returns>包含当前版本号和完整格子副本的导航快照.</returns>
    public HexGridCentralNavigationSnapshot CaptureSnapshot()
    {
        lock (m_SyncRoot)
        {
            return new HexGridCentralNavigationSnapshot(m_Cells, m_Version, m_Bake);
        }
    }
}
