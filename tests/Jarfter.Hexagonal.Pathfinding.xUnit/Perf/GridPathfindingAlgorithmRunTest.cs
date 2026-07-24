using Jarfter.Core.Diagnostics;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit.Perf;

/// <summary>
/// 比较相同地图上 A* 与 Theta* 的基础端到端性能.
/// 此入口用于确定算法选择的基线, 不针对某一项缓存或预计算优化.
/// </summary>
public static class GridPathfindingAlgorithmRunTest
{
    /// <summary>
    /// 运行 A* 与 Theta* 的时间和托管内存分配对比.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.5) }, [
            new MethodWrapper<int>(FindPathWithAStar),
            new MethodWrapper<int>(FindPathWithThetaStar)
        ]);
    }

    private static int FindPathWithAStar()
    {
        HexGridPath? path = HexGridAStar.Instance.FindPath(
            GridPathfindingBenchmarkScenario.Snapshot,
            GridPathfindingBenchmarkScenario.Layout,
            GridPathfindingBenchmarkScenario.Start,
            GridPathfindingBenchmarkScenario.Goal,
            GridPathfindingBenchmarkScenario.Footprint);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 A* 路径.");
    }

    private static int FindPathWithThetaStar()
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            GridPathfindingBenchmarkScenario.Snapshot,
            GridPathfindingBenchmarkScenario.Layout,
            GridPathfindingBenchmarkScenario.Start,
            GridPathfindingBenchmarkScenario.Goal,
            GridPathfindingBenchmarkScenario.Footprint);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 Theta* 路径.");
    }
}
