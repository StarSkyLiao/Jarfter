using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示一次连续路径搜索的结果.
/// 当 <see cref="IsSuccess"/> 为 false 时, <see cref="Path"/> 为空且 <see cref="TotalCost"/> 为正无穷大.
/// </summary>
/// <param name="Path">从起点到终点的连续折点序列.</param>
/// <param name="TotalCost">路径的总移动代价. 不存在高代价区域时等于路径长度.</param>
/// <param name="MapRevision">规划该路径时使用的地图版本.</param>
/// <param name="AgentRadius">规划时使用的移动单位半径.</param>
/// <param name="Clearance">规划时使用的额外安全距离.</param>
public readonly record struct ContinuousPathResult(
    IReadOnlyList<HexCubePoint> Path,
    double TotalCost,
    long MapRevision,
    double AgentRadius,
    double Clearance)
{
    /// <summary>
    /// 获取一个值, 指示是否已找到可达路径.
    /// </summary>
    public bool IsSuccess => Path.Count != 0;

    /// <summary>
    /// 创建表示未找到路径的结果.
    /// </summary>
    /// <param name="request">本次搜索的请求参数.</param>
    /// <param name="mapRevision">本次搜索使用的地图版本.</param>
    /// <returns>空路径结果.</returns>
    public static ContinuousPathResult Empty(ContinuousPathRequest request, long mapRevision)
    {
        return new ContinuousPathResult(
            Array.Empty<HexCubePoint>(),
            double.PositiveInfinity,
            mapRevision,
            request.AgentRadius,
            request.Clearance);
    }
}
