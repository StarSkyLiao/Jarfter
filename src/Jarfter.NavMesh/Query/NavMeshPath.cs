using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Query;

/// <summary>
/// 一次成功寻路产生的不可变路径.
/// </summary>
public sealed class NavMeshPath
{
    internal NavMeshPath(NavMeshPoint[] points, int[] corridor, NavMeshJumpTraversal[] jumps, double searchCost,
        double totalCost, double heuristicWeight)
    {
        Points = points;
        Corridor = corridor;
        Jumps = jumps;
        SearchCost = searchCost;
        TotalCost = totalCost;
        HeuristicWeight = heuristicWeight;
    }

    /// <summary>
    /// 获取经 funnel 平滑后的路径航点.
    /// </summary>
    public IReadOnlyList<NavMeshPoint> Points { get; }

    /// <summary>
    /// 获取按行进顺序穿越的凸多边形索引.
    /// </summary>
    public IReadOnlyList<int> Corridor { get; }

    /// <summary>
    /// 获取按路径顺序实际经过的跳跃连接.
    /// </summary>
    public IReadOnlyList<NavMeshJumpTraversal> Jumps { get; }

    /// <summary>
    /// 获取 A* polygon 图搜索累计成本.
    /// 当 <see cref="HeuristicWeight"/> 为 1 时, 该值在当前搜索图模型下最优.
    /// </summary>
    public double SearchCost { get; }

    /// <summary>
    /// 获取 funnel 平滑后路径按实际穿越区域长度计算的加权移动代价.
    /// </summary>
    public double TotalCost { get; }

    /// <summary>
    /// 获取本次查询开始时捕获的启发式权重.
    /// </summary>
    public double HeuristicWeight { get; }

    /// <summary>
    /// 获取本次 corridor 搜索是否使用最优 A* 权重.
    /// </summary>
    public bool IsSearchOptimal => HeuristicWeight == 1;
}
