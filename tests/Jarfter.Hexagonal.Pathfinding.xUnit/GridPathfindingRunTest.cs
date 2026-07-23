using Jarfter.Core.Diagnostics;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;
using Jarfter.Hexagonal.Pathfinding.Search;

namespace Jarfter.Hexagonal.Pathfinding.xUnit;

/// <summary>
/// 提供格心寻路器端到端性能基准的手动运行入口.
/// 基准使用三道错位缺口障碍墙、高成本地形区、固定起终点和固定足迹, 比较 A* 与 Theta* 的异步公开 API 成本.
/// </summary>
public static class GridPathfindingRunTest
{
    private const int MapRadius = 32;
    private static readonly HexagonalCubePoint s_Start = new HexagonalCubePoint(-20, 0);
    private static readonly HexagonalCubePoint s_Goal = new HexagonalCubePoint(20, 0);
    private static readonly HexagonalLayout s_Layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
    private static readonly HexagonalFootprint s_Footprint = new HexagonalFootprint(0.25);
    private static readonly HexGridCentralNavigationSnapshot s_Snapshot = CreateSnapshot();

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

    private static int FindPathWithAStar()
    {
        HexGridPath? path = HexGridAStar.Instance.FindPathAsync(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint).GetAwaiter().GetResult();

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 A* 路径.");
    }

    private static int FindPathWithThetaStar()
    {
        HexGridPath? path = HexGridThetaStar.Instance.FindPathAsync(
            s_Snapshot,
            s_Layout,
            s_Start,
            s_Goal,
            s_Footprint).GetAwaiter().GetResult();

        return path?.Points.Length ?? throw new InvalidOperationException("基准地图必须存在 Theta* 路径.");
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
