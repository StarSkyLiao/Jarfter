namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 指定共享格心搜索引擎使用的连接规则.
/// </summary>
internal enum HexGridSearchMode
{
    /// <summary>
    /// 仅连接当前格心与相邻格心.
    /// </summary>
    AStar,

    /// <summary>
    /// 优先连接当前节点的父格心与相邻格心.
    /// </summary>
    ThetaStar
}
