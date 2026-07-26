using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 定义六边形网格的路径规划能力.
/// </summary>
public partial interface IPathfinder
{
    /// <summary>
    /// 从给定的起点, 搜索一条通向指定终点的路径.
    /// </summary>
    /// <param name="start">路径的起点.</param>
    /// <param name="goal">路径的终点.</param>
    /// <returns>包含路径及其总代价的搜索结果; 不存在可达路径时返回 <see cref="PathfindingResult.Empty"/>.</returns>
    PathfindingResult FindPath(HexCubeGridPoint start, HexCubeGridPoint goal);
}
