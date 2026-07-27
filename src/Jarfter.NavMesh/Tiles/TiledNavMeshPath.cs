using Jarfter.NavMesh.Geometry;

namespace Jarfter.NavMesh.Tiles;

/// <summary>
/// 一次跨 tile 查询产生的不可变二维路径.
/// </summary>
public sealed class TiledNavMeshPath
{
    internal TiledNavMeshPath(NavMeshPoint[] points, TiledNavMeshPolygon[] corridor,
        TiledNavMeshJumpTraversal[] jumps, double searchCost, double totalCost, double heuristicWeight)
    {
        Points = points;
        Corridor = corridor;
        Jumps = jumps;
        SearchCost = searchCost;
        TotalCost = totalCost;
        HeuristicWeight = heuristicWeight;
    }

    /// <summary>
    /// 获取经跨 tile funnel 平滑后的路径航点.
    /// 跳跃连接的起点和终点会作为不可省略的连续航点保留.
    /// </summary>
    public IReadOnlyList<NavMeshPoint> Points { get; }

    /// <summary>
    /// 获取按行进顺序穿越的 tile polygon.
    /// </summary>
    public IReadOnlyList<TiledNavMeshPolygon> Corridor { get; }

    /// <summary>
    /// 获取按路径顺序实际使用的 tile 内跳跃连接.
    /// </summary>
    public IReadOnlyList<TiledNavMeshJumpTraversal> Jumps { get; }

    /// <summary>
    /// 获取 tile polygon 图搜索累计成本.
    /// 该值按 polygon 中心距离与跳跃固定开销计算, 不等同于 funnel 后几何路径的实际移动距离.
    /// </summary>
    public double SearchCost { get; }

    /// <summary>
    /// 获取 funnel 平滑后路径按实际穿越 polygon 长度计算的加权移动代价.
    /// 跳跃段只计入其固定开销.
    /// </summary>
    public double TotalCost { get; }

    /// <summary>
    /// 获取本次查询开始时捕获的启发式权重.
    /// 当组合快照没有跳跃连接时, 大于 1 的权重会以可能牺牲最优性换取更快搜索.
    /// </summary>
    public double HeuristicWeight { get; }

    /// <summary>
    /// 获取本次 corridor 搜索是否使用最优 A* 权重.
    /// </summary>
    public bool IsSearchOptimal => HeuristicWeight == 1d;
}

/// <summary>
/// 表示跨 tile 路径实际使用的一次 tile 内跳跃连接.
/// </summary>
public readonly record struct TiledNavMeshJumpTraversal(
    TiledNavMeshPolygon From,
    TiledNavMeshPolygon To,
    NavMeshPoint Start,
    NavMeshPoint End,
    double FixedCost);
