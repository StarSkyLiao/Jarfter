using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 定义六边形网格的路径规划能力.
/// </summary>
public partial interface IPathfinder
{
    /// <summary>
    /// 从给定的起点, 搜索一条通向指定重点的路径.
    /// </summary>
    IReadOnlyList<HexCubePoint> FindPath(HexCubePoint start, HexCubePoint goal);
}
