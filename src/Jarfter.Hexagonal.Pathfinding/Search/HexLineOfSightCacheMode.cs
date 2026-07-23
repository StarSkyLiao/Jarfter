namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 指定单次格心搜索的直视结果缓存策略.
/// </summary>
public enum HexLineOfSightCacheMode
{
    /// <summary>
    /// 由算法决定. Theta* 启用缓存, A* 关闭缓存.
    /// </summary>
    Automatic,

    /// <summary>
    /// 启用单次搜索内的直视结果与成本缓存.
    /// </summary>
    Enabled,

    /// <summary>
    /// 禁用单次搜索内的直视缓存.
    /// </summary>
    Disabled
}
