using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.MapProvider;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.xUnit.Perf;

/// <summary>
/// 提供多个格心寻路性能入口共用的稳定地图、起终点和足迹.
/// 地图包含错位缺口障碍墙与高成本区域, 用于放大不同搜索优化的性能差异.
/// </summary>
internal static class GridPathfindingBenchmarkScenario
{
    private const int MapRadius = 32;

    internal static readonly HexagonalCubePoint Start = new HexagonalCubePoint(-20, 0);
    internal static readonly HexagonalCubePoint Goal = new HexagonalCubePoint(20, 0);
    internal static readonly HexagonalLayout Layout = new HexagonalLayout(HexagonalOrientation.PointyTop, 1);
    internal static readonly HexagonalFootprint Footprint = new HexagonalFootprint(0.25);
    internal static readonly HexGridCentralNavigationSnapshot Snapshot = CreateSnapshot();

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
