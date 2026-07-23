using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 提供以六边形格心为搜索节点的 A* 寻路.
/// A* 仅使用当前格心到相邻格心的连接, 不尝试跨越中间格心的直接视线连接.
/// </summary>
public sealed class HexGridAStar : IHexGridPathfinder
{
    /// <summary>
    /// 获取内置 A* 格心寻路器的无状态单例实例.
    /// </summary>
    public static HexGridAStar Instance { get; } = new HexGridAStar();

    private HexGridAStar()
    {
    }

    /// <inheritdoc />
    public HexGridPath? FindPath(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        IHexTraversalCostPolicy? costPolicy = null,
        HexPathfindingRequestOptions? requestOptions = null)
    {
        return HexGridSearch.FindPath(
            HexGridSearchMode.AStar,
            snapshot,
            layout,
            start,
            goal,
            footprint,
            clearanceApothemScale,
            costPolicy,
            requestOptions);
    }

    /// <inheritdoc />
    public ValueTask<HexGridPath?> FindPathAsync(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        IHexTraversalCostPolicy? costPolicy = null,
        HexPathfindingRequestOptions? requestOptions = null)
    {
        return HexGridSearch.FindPathAsync(
            HexGridSearchMode.AStar,
            snapshot,
            layout,
            start,
            goal,
            footprint,
            clearanceApothemScale,
            costPolicy,
            requestOptions);
    }
}
