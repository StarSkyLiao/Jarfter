using Jarfter.Core.Collections.Extensions;
using Jarfter.Core.Diagnostics;
using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

internal static class HexAStar
{
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.5) }, [
            new MethodWrapper<IReadOnlyList<HexCubePoint>>(Run)
        ]);
    }

    public static void RunResult()
    {
        Console.WriteLine(Run().View());
    }

    public static IReadOnlyList<HexCubePoint> Run()
    {
        HexGridCentral<HexNavigationCell> map = CreateSnapshot();
        IPathfinder pathfinder = new IPathfinder.AStar(IHeuristic.Default.Instance, point =>
        {
            HexNavigationCell hexNavigationCell = map[point];
            if (hexNavigationCell.obstacleApothemScale > 0) return -1;
            return hexNavigationCell.traversalMultiplier;
        });
        return pathfinder.FindPath(new HexCubePoint(-20, 0), new HexCubePoint(20, 0));
    }

    private static HexGridCentral<HexNavigationCell> CreateSnapshot()
    {
        HexGridCentral<HexNavigationCell> map = new HexGridCentral<HexNavigationCell>(32);

        AddHighCostArea(map);
        AddBarrier(map, -10, -20, 15, -9, -5);
        AddBarrier(map, 0, -24, 24, 8, 12);
        AddBarrier(map, 10, -16, 20, -6, -2);

        return map;
    }

    private static void AddHighCostArea(HexGridCentral<HexNavigationCell> map)
    {
        // 中央区域的高成本地形会排除部分较短但代价更高的绕行路线.
        for (int q = -6; q <= 6; q++)
        {
            for (int r = -3; r <= 3; r++)
            {
                HexCubePoint point = new HexCubePoint(q, r);

                if (map.Contains(point))
                {
                    map[point] = new HexNavigationCell(3);
                }
            }
        }
    }

    private static void AddBarrier(HexGridCentral<HexNavigationCell> map, int q, int minimumR, int maximumR, int gapMinimumR, int gapMaximumR)
    {
        // 三道墙的缺口交错分布, 使路径需要反复改变行进方向而不能只做一次绕行.
        for (int r = minimumR; r <= maximumR; r++)
        {
            if (r >= gapMinimumR && r <= gapMaximumR)
            {
                continue;
            }

            HexCubePoint point = new HexCubePoint(q, r);

            if (map.Contains(point))
            {
                map[point] = new HexNavigationCell(1, 1);
            }
        }
    }

    private record struct HexNavigationCell(double traversalMultiplier = 1, double obstacleApothemScale = 0);

}
