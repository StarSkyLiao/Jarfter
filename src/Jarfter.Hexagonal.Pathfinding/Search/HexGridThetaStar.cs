using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 提供以六边形格心为搜索节点的 Theta* 寻路.
/// Theta* 会在父节点与候选邻居具有直接视线时跳过中间格心, 以减少不必要的航点.
/// </summary>
public sealed class HexGridThetaStar : IHexGridPathfinder
{
    /// <summary>
    /// 获取内置 Theta* 格心寻路器的无状态单例实例.
    /// </summary>
    public static HexGridThetaStar Instance { get; } = new HexGridThetaStar();

    private HexGridThetaStar()
    {
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
            HexGridSearchMode.ThetaStar,
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
