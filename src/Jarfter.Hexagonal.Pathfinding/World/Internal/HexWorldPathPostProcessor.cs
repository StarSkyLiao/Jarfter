using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation.Model;
using Jarfter.Hexagonal.Pathfinding.Navigation.Visibility;

namespace Jarfter.Hexagonal.Pathfinding.World.Internal;

/// <summary>
/// 对已连接的连续世界路径执行可选平滑, 并重新验证各段可通行性及累计成本.
/// </summary>
internal static class HexWorldPathPostProcessor
{
    /// <summary>
    /// 使用贪心直视检测移除不必要的中间航点.
    /// </summary>
    internal static List<HexagonalWorldPoint> SmoothWaypoints(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        List<HexagonalWorldPoint> waypoints,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy)
    {
        List<HexagonalWorldPoint> smoothedWaypoints = [waypoints[0]];
        int currentIndex = 0;

        while (currentIndex < waypoints.Count - 1)
        {
            int nextIndex = waypoints.Count - 1;

            while (nextIndex > currentIndex + 1
                && !HexLineOfSight.HasLineOfSight(
                    snapshot,
                    layout,
                    waypoints[currentIndex],
                    waypoints[nextIndex],
                    footprint,
                    clearanceApothemScale,
                    costPolicy))
            {
                nextIndex--;
            }

            smoothedWaypoints.Add(waypoints[nextIndex]);
            currentIndex = nextIndex;
        }

        return smoothedWaypoints;
    }

    /// <summary>
    /// 尝试重新计算连续路径每个线段的可通行成本.
    /// </summary>
    internal static bool TryGetPathCost(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        List<HexagonalWorldPoint> waypoints,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        out double cost)
    {
        double totalCost = 0;

        for (int index = 1; index < waypoints.Count; index++)
        {
            if (!HexLineOfSight.TryGetTraversalCost(
                    snapshot,
                    layout,
                    waypoints[index - 1],
                    waypoints[index],
                    footprint,
                    out double segmentCost,
                    clearanceApothemScale,
                    costPolicy))
            {
                cost = 0;
                return false;
            }

            totalCost += segmentCost;
        }

        cost = totalCost;
        return true;
    }
}
