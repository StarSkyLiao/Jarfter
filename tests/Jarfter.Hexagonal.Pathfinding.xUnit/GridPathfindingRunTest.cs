using Jarfter.Core.Diagnostics;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

/// <summary>
/// 提供格心寻路器端到端性能基准的手动运行入口.
/// 基准使用三道错位缺口障碍墙、高成本地形区、固定起终点和固定足迹, 比较 A* 与 Theta* 的同步公开 API 成本.
/// </summary>
public static class GridPathfindingRunTest
{
    private const int MapRadius = 32;
    private static readonly HexagonalCubePoint s_Start = new HexagonalCubePoint(-20, 0);
    private static readonly HexagonalCubePoint s_Goal = new HexagonalCubePoint(20, 0);
    private static readonly HexagonalLayout s_Layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
    private static readonly HexagonalFootprint s_Footprint = new HexagonalFootprint(0.25);
    private static readonly HexGridCentralNavigationSnapshot s_Snapshot = CreateSnapshot();
    private static readonly HexPathfindingRequestOptions s_DiagnosticRequestOptions = new HexPathfindingRequestOptions
    {
        CollectStatistics = true,
        LineOfSightCacheMode = HexLineOfSightCacheMode.Disabled
    };
    private static readonly HexPathfindingRequestOptions s_CachedDiagnosticRequestOptions = new HexPathfindingRequestOptions
    {
        CollectStatistics = true,
        LineOfSightCacheMode = HexLineOfSightCacheMode.Enabled
    };
    private static readonly HexPathfindingRequestOptions s_CachedRequestOptions = new HexPathfindingRequestOptions
    {
        LineOfSightCacheMode = HexLineOfSightCacheMode.Enabled
    };
    private static readonly HexPathfindingRequestOptions s_UncachedRequestOptions = new HexPathfindingRequestOptions
    {
        LineOfSightCacheMode = HexLineOfSightCacheMode.Disabled
    };

    /// <summary>
    /// 运行 A* 与 Theta* 的端到端快速性能测试.
    /// </summary>
    public static void Run()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.2) }, [
            new MethodWrapper<int>(FindPathWithAStar),
            new MethodWrapper<int>(FindPathWithThetaStar)
        ]);
    }

    /// <summary>
    /// 运行一次 A* 与 Theta* 并输出搜索工作量统计, 用于定位性能热点.
    /// </summary>
    public static void RunDiagnostics()
    {
        WriteDiagnostics("A*", HexGridAStar.Instance, s_DiagnosticRequestOptions);
        WriteDiagnostics("Theta*（无缓存）", HexGridThetaStar.Instance, s_DiagnosticRequestOptions);
        WriteDiagnostics("Theta*（直视缓存）", HexGridThetaStar.Instance, s_CachedDiagnosticRequestOptions);
    }

    /// <summary>
    /// 运行 Theta* 启用与不启用单次搜索直视缓存的端到端快速性能对比.
    /// </summary>
    public static void RunLineOfSightCacheComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.2) }, [
            new MethodWrapper<int>(FindPathWithUncachedThetaStar),
            new MethodWrapper<int>(FindPathWithCachedThetaStar)
        ]);
    }

    private static int FindPathWithAStar()
    {
        HexGridPath? path = HexGridAStar.Instance.FindPath(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 A* 路径.");
    }

    private static int FindPathWithThetaStar()
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 Theta* 路径.");
    }

    private static int FindPathWithUncachedThetaStar()
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint,
            requestOptions: s_UncachedRequestOptions);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在未启用缓存的 Theta* 路径.");
    }

    private static int FindPathWithCachedThetaStar()
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint,
            requestOptions: s_CachedRequestOptions);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在启用缓存的 Theta* 路径.");
    }

    private static void WriteDiagnostics(
        string algorithmName,
        IHexGridPathfinder pathfinder,
        HexPathfindingRequestOptions requestOptions)
    {
        HexGridPath path = pathfinder.FindPath(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint,
            requestOptions: requestOptions)
            ?? throw new InvalidOperationException($"基准地图必须存在 {algorithmName} 路径.");
        HexPathfindingStatistics statistics = path.Statistics
            ?? throw new InvalidOperationException("诊断请求必须返回搜索统计.");

        Console.WriteLine(
            $"{algorithmName}: 航点={path.Points.Length}, 展开节点={statistics.ExpandedNodeCount}, "
            + $"直视检测={statistics.LineOfSightQueryCount}, 父节点直视={statistics.ParentLineOfSightQueryCount}, "
            + $"父节点直视成功={statistics.SuccessfulParentLineOfSightQueryCount}, 穿格={statistics.TraversedCellCount}, "
            + $"附近格查询={statistics.NearbyCellQueryCount}, 障碍相交测试={statistics.ObstacleIntersectionTestCount}, "
            + $"直视缓存命中={statistics.LineOfSightCacheHitCount}, 直视缓存未命中={statistics.LineOfSightCacheMissCount}");
    }

    private static HexGridCentralNavigationSnapshot CreateSnapshot()
    {
        HexGridCentralProvider<HexNavigationCell> map = new HexGridCentralProvider<HexNavigationCell>(MapRadius);

        AddHighCostArea(map);
        AddBarrier(map, -10, -20, 15, -9, -5);
        AddBarrier(map, 0, -24, 24, 8, 12);
        AddBarrier(map, 10, -16, 20, -6, -2);

        return new HexGridCentralNavigationSnapshot(map, 0);
    }

    private static void AddHighCostArea(HexGridCentralProvider<HexNavigationCell> map)
    {
        // 中央区域的高成本地形会排除部分较短但代价更高的绕行路线.
        for (int q = -6; q <= 6; q++)
        {
            for (int r = -3; r <= 3; r++)
            {
                HexagonalCubePoint point = new HexagonalCubePoint(q, r);

                if (map.Contains(point))
                {
                    map[point] = new HexNavigationCell(3);
                }
            }
        }
    }

    private static void AddBarrier(
        HexGridCentralProvider<HexNavigationCell> map,
        int q,
        int minimumR,
        int maximumR,
        int gapMinimumR,
        int gapMaximumR)
    {
        // 三道墙的缺口交错分布, 使路径需要反复改变行进方向而不能只做一次绕行.
        for (int r = minimumR; r <= maximumR; r++)
        {
            if (r >= gapMinimumR && r <= gapMaximumR)
            {
                continue;
            }

            HexagonalCubePoint point = new HexagonalCubePoint(q, r);

            if (map.Contains(point))
            {
                map[point] = new HexNavigationCell(1, 1);
            }
        }
    }
}
