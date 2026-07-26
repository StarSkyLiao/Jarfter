namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 定义在连续六边形平面中规划任意坐标路径的能力.
/// 实现必须只读取 <see cref="IContinuousNavigationSnapshot"/> 提供的不可变地图数据, 不应在一次搜索期间访问可变地图状态.
/// </summary>
public interface IContinuousPathfinder
{
    /// <summary>
    /// 使用指定地图快照搜索一条从起点通向终点的连续路径.
    /// </summary>
    /// <param name="request">包含起点、终点、移动单位半径和搜索质量参数的请求.</param>
    /// <param name="snapshot">本次搜索使用的不可变地图快照.</param>
    /// <returns>包含连续路径、总代价和规划地图版本的搜索结果.</returns>
    ContinuousPathResult FindPath(ContinuousPathRequest request, IContinuousNavigationSnapshot snapshot);
}
