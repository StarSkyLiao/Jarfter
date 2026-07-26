using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 定义支持版本化变更记录的连续导航地图.
/// 寻路器通过 <see cref="CaptureSnapshot"/> 获取稳定数据, 调用方通过 <see cref="ValidateRemainingPath"/> 决定地图变化后是否重新规划.
/// </summary>
public interface IContinuousNavigationMap
{
    /// <summary>
    /// 当地图障碍物发生变化时触发.
    /// </summary>
    event Action<ContinuousMapChange>? Changed;

    /// <summary>
    /// 获取当前地图版本.
    /// </summary>
    long CurrentRevision { get; }

    /// <summary>
    /// 捕获当前地图的不可变导航快照.
    /// </summary>
    /// <returns>可供一次完整寻路使用的导航快照.</returns>
    IContinuousNavigationSnapshot CaptureSnapshot();

    /// <summary>
    /// 判断已规划路径从指定线段开始的剩余部分是否仍可通行.
    /// </summary>
    /// <param name="path">待校验的连续路径.</param>
    /// <param name="remainingSegmentIndex">剩余路径的首个线段索引.</param>
    /// <returns>剩余路径的有效性校验结果.</returns>
    ContinuousPathValidationResult ValidateRemainingPath(ContinuousPathResult path, int remainingSegmentIndex);

    /// <summary>
    /// 新增或替换指定标识的高代价区域.
    /// </summary>
    /// <param name="id">高代价区域的稳定标识.</param>
    /// <param name="area">高代价区域的连续六边形形状.</param>
    /// <param name="traversalMultiplier">穿过区域时的移动代价倍率. 必须大于 1.</param>
    void SetTraversalArea(long id, HexCubeArea2D area, double traversalMultiplier);

    /// <summary>
    /// 移除指定标识的高代价区域.
    /// </summary>
    /// <param name="id">要移除的高代价区域标识.</param>
    /// <returns>找到并移除高代价区域时返回 true, 否则返回 false.</returns>
    bool RemoveTraversalArea(long id);
}
