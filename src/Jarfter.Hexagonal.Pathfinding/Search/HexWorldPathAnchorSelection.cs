namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 指定连续端点连接格心搜索图时的锚点选择规则.
/// </summary>
public enum HexWorldPathAnchorSelection
{
    /// <summary>
    /// 选择连接线段移动成本最低的可见格心.
    /// </summary>
    LowestTraversalCost,

    /// <summary>
    /// 选择与连续端点世界距离最近的可见格心.
    /// </summary>
    NearestWorldDistance
}
