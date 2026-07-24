using Jarfter.Core.Diagnostics;
using Jarfter.Hexagonal.Pathfinding.Grid;
using Jarfter.Hexagonal.Pathfinding.Grid.Algorithms;
using Jarfter.Hexagonal.Pathfinding.Grid.Results;
using Jarfter.Hexagonal.Pathfinding.Grid.Runtime;

namespace Jarfter.Hexagonal.Pathfinding.xUnit.Perf;

/// <summary>
/// 比较常规无状态寻路与复用预分配工作区时的性能和托管内存分配.
/// 工作区实例按算法分离, 且每个基准入口仅在当前线程顺序使用它们.
/// </summary>
public static class GridPathfindingWorkspaceRunTest
{
    private static readonly HexGridPathfindingWorkspace s_AStarWorkspace = new HexGridPathfindingWorkspace(GridPathfindingBenchmarkScenario.Snapshot);
    private static readonly HexGridPathfindingWorkspace s_ThetaStarWorkspace = new HexGridPathfindingWorkspace(GridPathfindingBenchmarkScenario.Snapshot);

    /// <summary>
    /// 运行 A* 与 Theta* 在无状态和复用工作区模式下的时间和托管内存分配对比.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.2) }, [
            new MethodWrapper<int>(FindPathWithAStar),
            new MethodWrapper<int>(FindPathWithReusableWorkspaceAStar),
            new MethodWrapper<int>(FindPathWithThetaStar),
            new MethodWrapper<int>(FindPathWithReusableWorkspaceThetaStar)
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

    private static int FindPathWithReusableWorkspaceAStar()
    {
        HexGridPath? path = HexGridAStar.Instance.FindPath(
            GridPathfindingBenchmarkScenario.Snapshot,
            s_AStarWorkspace,
            GridPathfindingBenchmarkScenario.Layout,
            GridPathfindingBenchmarkScenario.Start,
            GridPathfindingBenchmarkScenario.Goal,
            GridPathfindingBenchmarkScenario.Footprint);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在复用工作区的 A* 路径.");
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

    private static int FindPathWithReusableWorkspaceThetaStar()
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPath(
            GridPathfindingBenchmarkScenario.Snapshot,
            s_ThetaStarWorkspace,
            GridPathfindingBenchmarkScenario.Layout,
            GridPathfindingBenchmarkScenario.Start,
            GridPathfindingBenchmarkScenario.Goal,
            GridPathfindingBenchmarkScenario.Footprint);

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在复用工作区的 Theta* 路径.");
    }
}
