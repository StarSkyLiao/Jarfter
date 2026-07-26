using Jarfter.Core.Diagnostics;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 在相同的基准选项下比较 A*、Theta* 与 Lazy Theta* 的路径搜索性能.
/// 三个算法分别使用等价的静态地图快照, 基准过程仅包含路径搜索本身.
/// </summary>
public static class HexPathfindingComparisonRunTest
{
    /// <summary>
    /// 运行三种寻路算法的时间和托管内存分配对比.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(10) { TargetTime = TimeSpan.FromSeconds(0.15) }, [
            new MethodWrapper<PathfindingResult>(HexAStarRunTest.Run),
            new MethodWrapper<PathfindingResult>(HexWeightedAStarRunTest.Run),
            new MethodWrapper<PathfindingResult>(HexThetaStarRunTest.Run),
            new MethodWrapper<PathfindingResult>(HexThetaStarWeightedRunTest.Run),
            new MethodWrapper<PathfindingResult>(HexLazyThetaStarRunTest.Run),
            new MethodWrapper<PathfindingResult>(HexLazyThetaStarWeightedRunTest.Run),
        ]);
    }

    /// <summary>
    /// 依次运行所有路径搜索器并导出对应的路径图像.
    /// </summary>
    public static void RunResult()
    {
        HexAStarRunTest.RunResult();
        HexWeightedAStarRunTest.RunResult();
        HexThetaStarRunTest.RunResult();
        HexThetaStarWeightedRunTest.RunResult();
        HexLazyThetaStarRunTest.RunResult();
        HexLazyThetaStarWeightedRunTest.RunResult();
    }
}
