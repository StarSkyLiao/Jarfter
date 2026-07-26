using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 表示一次路径搜索的路径和总代价.
/// 当 <see cref="IsSuccess"/> 为 false 时, <see cref="Path"/> 为空且 <see cref="TotalCost"/> 为正无穷大.
/// </summary>
/// <param name="Path">从起点到终点的路径坐标序列.</param>
/// <param name="TotalCost">路径的总代价.</param>
public readonly record struct PathfindingResult(IReadOnlyList<HexCubePoint> Path, double TotalCost)
{
    /// <summary>
    /// 获取一个值, 指示是否已找到可达路径.
    /// </summary>
    public bool IsSuccess => Path.Count != 0;

    /// <summary>
    /// 获取表示未找到路径的结果.
    /// </summary>
    public static PathfindingResult Empty => new PathfindingResult(Array.Empty<HexCubePoint>(), double.PositiveInfinity);
}
