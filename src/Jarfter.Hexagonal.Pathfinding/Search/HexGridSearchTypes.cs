using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 定义格心搜索在稀疏记录与直视缓存之间传递的轻量内部数据结构.
/// </summary>
internal static class HexGridSearchTypes
{
    /// <summary>
    /// 表示稀疏搜索中一个格心的当前最低成本和回溯父节点.
    /// </summary>
    internal readonly record struct SparseNodeRecord(double Cost, HexagonalCubePoint Parent, bool HasParent);

    /// <summary>
    /// 表示一个格心的当前最低成本和回溯父节点.
    /// </summary>
    internal readonly record struct NodeRecord(double Cost, HexagonalCubePoint Parent, bool HasParent);

    /// <summary>
    /// 表示有序格心对的单次搜索直视缓存键.
    /// </summary>
    internal readonly record struct LineOfSightCacheKey(HexagonalCubePoint Start, HexagonalCubePoint End);

    /// <summary>
    /// 表示缓存的直视可通行性及其累计成本.
    /// </summary>
    internal readonly record struct LineOfSightCacheEntry(bool IsTraversable, double Cost);
}
