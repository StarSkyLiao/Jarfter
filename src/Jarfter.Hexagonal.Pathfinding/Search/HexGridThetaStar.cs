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

    /// <summary>
    /// 使用可复用工作区在当前线程中同步查找路径.
    /// 工作区必须由 <paramref name="snapshot"/> 的 <see cref="HexGridCentralNavigationSnapshot.Bake"/> 创建.
    /// </summary>
    /// <param name="snapshot">要读取的中心稠密导航地图快照.</param>
    /// <param name="workspace">同一烘焙地图上的高频同步寻路可复用该工作区以减少分配; 单个实例不可并发使用.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略; 为 <see langword="null"/> 时使用默认地形倍率策略.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时、取消与缓存策略; 为 <see langword="null"/> 时使用默认策略.</param>
    /// <returns>成功时得到格心航点路径; 不可达、超时或超出节点预算时返回 <see langword="null"/>.</returns>
    public HexGridPath? FindPath(
        HexGridCentralNavigationSnapshot snapshot,
        HexGridPathfindingWorkspace workspace,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        IHexTraversalCostPolicy? costPolicy = null,
        HexPathfindingRequestOptions? requestOptions = null)
    {
        return HexGridSearch.FindPath(
            HexGridSearchMode.ThetaStar,
            snapshot,
            workspace,
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
