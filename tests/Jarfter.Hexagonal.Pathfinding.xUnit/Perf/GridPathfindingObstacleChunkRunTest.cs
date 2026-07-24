using Jarfter.Core.Diagnostics;
using Jarfter.Hexagonal.Pathfinding.Grid;

namespace Jarfter.Hexagonal.Pathfinding.xUnit.Perf;

/// <summary>
/// 比较中心稠密导航快照的障碍块粗筛开启与关闭时的性能.
/// 两组请求均使用相同的精确六边形障碍相交检测, 仅比较粗筛是否跳过空区域的附近格枚举.
/// </summary>
public static class GridPathfindingObstacleChunkRunTest
{
    private static readonly HexPathfindingRequestOptions s_UnchunkedRequestOptions = new HexPathfindingRequestOptions
    {
        UseObstacleChunkAcceleration = false
    };
    private static readonly HexPathfindingRequestOptions s_UnchunkedCachedRequestOptions = new HexPathfindingRequestOptions
    {
        LineOfSightCacheMode = HexLineOfSightCacheMode.Enabled,
        UseObstacleChunkAcceleration = false
    };
    private static readonly HexPathfindingRequestOptions s_CachedRequestOptions = new HexPathfindingRequestOptions
    {
        LineOfSightCacheMode = HexLineOfSightCacheMode.Enabled
    };

    /// <summary>
    /// 运行 A* 与缓存 Theta* 启用和关闭障碍块粗筛时的时间及托管内存分配对比.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.2) }, [
            new MethodWrapper<int>(FindPathWithUnchunkedAStar),
            new MethodWrapper<int>(FindPathWithChunkedAStar),
            new MethodWrapper<int>(FindPathWithUnchunkedCachedThetaStar),
            new MethodWrapper<int>(FindPathWithChunkedCachedThetaStar)
        ]);
    }

    private static int FindPathWithUnchunkedAStar() => FindAStarPath(s_UnchunkedRequestOptions);

    private static int FindPathWithChunkedAStar() => FindAStarPath(requestOptions: null);

    private static int FindPathWithUnchunkedCachedThetaStar() => FindThetaStarPath(s_UnchunkedCachedRequestOptions);

    private static int FindPathWithChunkedCachedThetaStar() => FindThetaStarPath(s_CachedRequestOptions);

    private static int FindAStarPath(HexPathfindingRequestOptions? requestOptions)
    {
        HexGridPath? path = HexGridAStar.Instance.FindPath(
            GridPathfindingBenchmarkScenario.Snapshot,
            GridPathfindingBenchmarkScenario.Layout,
            GridPathfindingBenchmarkScenario.Start,
            GridPathfindingBenchmarkScenario.Goal,
            GridPathfindingBenchmarkScenario.Footprint,
            requestOptions: requestOptions);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 A* 路径.");
    }

    private static int FindThetaStarPath(HexPathfindingRequestOptions requestOptions)
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
}
