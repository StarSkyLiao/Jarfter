using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 定义以六边形格心为搜索节点的路径查找器.
/// 实现可以使用 A*、Theta* 或调用方自定义的其他算法, 但返回路径中的相邻航点必须可由实现声明的移动模型安全连接.
/// </summary>
public interface IHexGridPathfinder
{
    /// <summary>
    /// 在当前线程中同步查找从起点格心到终点格心的路径.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略; 为 <see langword="null"/> 时使用默认地形倍率策略.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时、取消与缓存策略; 为 <see langword="null"/> 时使用默认策略.</param>
    /// <returns>成功时得到格心航点路径; 不可达、超时或超出节点预算时返回 <see langword="null"/>.</returns>
    HexGridPath? FindPath(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        IHexTraversalCostPolicy? costPolicy = null,
        HexPathfindingRequestOptions? requestOptions = null);

    /// <summary>
    /// 在指定不可变导航快照中尝试查找从起点格心到终点格心的路径.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略; 为 <see langword="null"/> 时使用默认地形倍率策略.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时、取消与缓存策略; 为 <see langword="null"/> 时使用默认策略.</param>
    /// <returns>表示异步搜索操作的值任务. 成功时结果为格心航点路径; 不可达、超时或超出节点预算时结果为 <see langword="null"/>.</returns>
    ValueTask<HexGridPath?> FindPathAsync(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        IHexTraversalCostPolicy? costPolicy = null,
        HexPathfindingRequestOptions? requestOptions = null);
}
