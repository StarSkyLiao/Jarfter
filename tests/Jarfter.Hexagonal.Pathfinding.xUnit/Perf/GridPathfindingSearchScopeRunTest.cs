using Jarfter.Core.Diagnostics;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Grid;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Grid;

namespace Jarfter.Hexagonal.Pathfinding.xUnit.Perf;

/// <summary>
/// 比较短距离局部绕行时无限制搜索与范围扩张策略的性能.
/// 大地图仅用于保留无限制搜索的扩张空间; 起终点相距四格, 且中间短墙迫使路径在附近绕行.
/// </summary>
public static class GridPathfindingSearchScopeRunTest
{
    private const int MapRadius = 32;
    private static readonly HexagonalCubePoint s_Start = new HexagonalCubePoint(-2, 0);
    private static readonly HexagonalCubePoint s_Goal = new HexagonalCubePoint(2, 0);
    private static readonly HexagonalLayout s_Layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
    private static readonly HexagonalFootprint s_Footprint = new HexagonalFootprint(0.25);
    private static readonly HexGridCentralNavigationSnapshot s_Snapshot = CreateSnapshot();
    private static readonly HexPathfindingRequestOptions s_ScopedRequestOptions = new HexPathfindingRequestOptions
    {
        SearchScopeStrategy = HexPathSearchScopeStrategies.ExpandingDetour
    };

    /// <summary>
    /// 运行 A* 与 Theta* 在无限制和范围扩张模式下的时间及托管内存分配对比.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.2) }, [
            new MethodWrapper<int>(FindPathWithAStar),
            new MethodWrapper<int>(FindPathWithScopedAStar),
            new MethodWrapper<int>(FindPathWithThetaStar),
            new MethodWrapper<int>(FindPathWithScopedThetaStar)
        ]);
    }

    private static int FindPathWithAStar() => FindAStarPath(requestOptions: null);

    private static int FindPathWithScopedAStar() => FindAStarPath(s_ScopedRequestOptions);

    private static int FindPathWithThetaStar() => FindThetaStarPath(requestOptions: null);

    private static int FindPathWithScopedThetaStar() => FindThetaStarPath(s_ScopedRequestOptions);

    private static int FindAStarPath(HexPathfindingRequestOptions? requestOptions)
    {
        HexGridPath? path = HexGridAStar.Instance.FindPath(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint,
            requestOptions: requestOptions);

        return path?.Points.Length ?? throw new InvalidOperationException("局部基准地图必须存在 A* 路径.");
    }

    private static int FindThetaStarPath(HexPathfindingRequestOptions? requestOptions)
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint,
            requestOptions: requestOptions);

        return path?.Points.Length ?? throw new InvalidOperationException("局部基准地图必须存在 Theta* 路径.");
    }

    private static HexGridCentralNavigationSnapshot CreateSnapshot()
    {
        HexGridCentral<HexNavigationCell> map = new HexGridCentral<HexNavigationCell>(MapRadius);

        // 直线路径被短墙阻挡, 但在起终点附近即可完成绕行.
        for (int r = -1; r <= 1; r++)
        {
            map[new HexagonalCubePoint(0, r)] = new HexNavigationCell(1, 1);
        }

        return new HexGridCentralNavigationSnapshot(map, 0);
    }
}
