using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 定义以任意连续世界坐标作为起终点的六边形路径查找器.
/// </summary>
public interface IHexWorldPathfinder
{
    /// <summary>
    /// 在指定不可变导航快照中尝试查找从连续起点到连续终点的低成本可见路径.
    /// 返回路径的首尾航点分别等于传入的 <paramref name="start"/> 和 <paramref name="goal"/>.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">移动对象的连续世界坐标起点.</param>
    /// <param name="goal">移动对象的连续世界坐标终点.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时与取消限制; 为 <see langword="null"/> 时不限制.</param>
    /// <returns>表示异步搜索操作的值任务. 成功时结果为连续世界坐标路径; 不可达、超时或超出节点预算时结果为 <see langword="null"/>.</returns>
    ValueTask<HexWorldPath?> FindPathAsync(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        HexPathfindingRequestOptions? requestOptions = null);
}
