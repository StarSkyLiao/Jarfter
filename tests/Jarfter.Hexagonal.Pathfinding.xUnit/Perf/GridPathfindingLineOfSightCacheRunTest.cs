using Jarfter.Core.Diagnostics;
using Jarfter.Hexagonal.Pathfinding.Grid;

namespace Jarfter.Hexagonal.Pathfinding.xUnit.Perf;

/// <summary>
/// 比较 Theta* 启用与关闭单次搜索直视缓存时的性能, 并提供对应的工作量统计入口.
/// </summary>
public static class GridPathfindingLineOfSightCacheRunTest
{
    private static readonly HexPathfindingRequestOptions s_UncachedRequestOptions = new HexPathfindingRequestOptions
    {
        LineOfSightCacheMode = HexLineOfSightCacheMode.Disabled
    };
    private static readonly HexPathfindingRequestOptions s_CachedRequestOptions = new HexPathfindingRequestOptions
    {
        LineOfSightCacheMode = HexLineOfSightCacheMode.Enabled
    };
    private static readonly HexPathfindingRequestOptions s_UncachedDiagnosticRequestOptions = new HexPathfindingRequestOptions
    {
        CollectStatistics = true,
        LineOfSightCacheMode = HexLineOfSightCacheMode.Disabled
    };
    private static readonly HexPathfindingRequestOptions s_CachedDiagnosticRequestOptions = new HexPathfindingRequestOptions
    {
        CollectStatistics = true,
        LineOfSightCacheMode = HexLineOfSightCacheMode.Enabled
    };

    /// <summary>
    /// 运行 Theta* 未缓存与直视缓存的时间和托管内存分配对比.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.2) }, [
            new MethodWrapper<int>(FindPathWithoutCache),
            new MethodWrapper<int>(FindPathWithCache)
        ]);
    }

    /// <summary>
    /// 输出 Theta* 未缓存与直视缓存的搜索工作量统计, 用于定位缓存收益来源.
    /// </summary>
    public static void RunDiagnostics()
    {
        WriteDiagnostics("Theta*（无缓存）", s_UncachedDiagnosticRequestOptions);
        WriteDiagnostics("Theta*（直视缓存）", s_CachedDiagnosticRequestOptions);
    }

    private static int FindPathWithoutCache() => FindPath(s_UncachedRequestOptions);

    private static int FindPathWithCache() => FindPath(s_CachedRequestOptions);

    private static int FindPath(HexPathfindingRequestOptions requestOptions)
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            GridPathfindingBenchmarkScenario.Snapshot,
            GridPathfindingBenchmarkScenario.Layout,
            GridPathfindingBenchmarkScenario.Start,
            GridPathfindingBenchmarkScenario.Goal,
            GridPathfindingBenchmarkScenario.Footprint,
            requestOptions: requestOptions);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 Theta* 路径.");
    }

    private static void WriteDiagnostics(string algorithmName, HexPathfindingRequestOptions requestOptions)
    {
        HexGridPath path = HexGridThetaStar.Instance.FindPath(
            GridPathfindingBenchmarkScenario.Snapshot,
            GridPathfindingBenchmarkScenario.Layout,
            GridPathfindingBenchmarkScenario.Start,
            GridPathfindingBenchmarkScenario.Goal,
            GridPathfindingBenchmarkScenario.Footprint,
            requestOptions: requestOptions)
            ?? throw new InvalidOperationException($"基准地图必须存在 {algorithmName} 路径.");
        HexPathfindingStatistics statistics = path.Statistics
            ?? throw new InvalidOperationException("诊断请求必须返回搜索统计.");

        Console.WriteLine(
            $"{algorithmName}: 航点={path.Points.Length}, 展开节点={statistics.ExpandedNodeCount}, "
            + $"直视检测={statistics.LineOfSightQueryCount}, 父节点直视={statistics.ParentLineOfSightQueryCount}, "
            + $"父节点直视成功={statistics.SuccessfulParentLineOfSightQueryCount}, 穿格={statistics.TraversedCellCount}, "
            + $"附近格查询={statistics.NearbyCellQueryCount}, 障碍相交测试={statistics.ObstacleIntersectionTestCount}, "
            + $"空障碍块跳过={statistics.ObstacleFreeChunkRangeSkipCount}, "
            + $"直视缓存命中={statistics.LineOfSightCacheHitCount}, 直视缓存未命中={statistics.LineOfSightCacheMissCount}");
    }
}
