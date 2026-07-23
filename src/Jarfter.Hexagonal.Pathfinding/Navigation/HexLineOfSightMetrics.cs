namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 收集一次搜索期间由直视检测产生的底层工作量.
/// </summary>
internal sealed class HexLineOfSightMetrics
{
    /// <summary>
    /// 获取已遍历的主穿格数量.
    /// </summary>
    internal long TraversedCellCount { get; private set; }

    /// <summary>
    /// 获取为检查附近障碍而查询的格子数量.
    /// </summary>
    internal long NearbyCellQueryCount { get; private set; }

    /// <summary>
    /// 获取实际执行的膨胀障碍相交测试数量.
    /// </summary>
    internal long ObstacleIntersectionTestCount { get; private set; }

    /// <summary>
    /// 记录一个主穿格.
    /// </summary>
    internal void AddTraversedCell()
    {
        TraversedCellCount++;
    }

    /// <summary>
    /// 记录一个附近格子查询.
    /// </summary>
    internal void AddNearbyCellQuery()
    {
        NearbyCellQueryCount++;
    }

    /// <summary>
    /// 记录一次膨胀障碍相交测试.
    /// </summary>
    internal void AddObstacleIntersectionTest()
    {
        ObstacleIntersectionTestCount++;
    }
}
