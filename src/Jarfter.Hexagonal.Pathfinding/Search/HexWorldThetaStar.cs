using System.Diagnostics.CodeAnalysis;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 提供以任意连续世界坐标作为起终点的六边形 Theta* 寻路.
/// 在直接可见时返回单段路径; 否则通过附近可见格心锚点接入离散 Theta* 搜索.
/// </summary>
public static class HexWorldThetaStar
{
    /// <summary>
    /// 在指定不可变导航快照中尝试查找从连续起点到连续终点的低成本可见路径.
    /// 返回路径的首尾航点分别等于传入的 <paramref name="start"/> 和 <paramref name="goal"/>.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">移动对象的连续世界坐标起点.</param>
    /// <param name="goal">移动对象的连续世界坐标终点.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="path">查找成功时得到的连续世界坐标路径.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <returns>当起终点可通行且存在可达路径时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="snapshot"/> 或 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当足迹、边距或坐标无效时抛出.</exception>
    public static bool TryFindPath(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint goal,
        HexagonalFootprint footprint,
        [NotNullWhen(true)] out HexWorldPath? path,
        double clearanceApothemScale = 0)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(layout);

        if (HexLineOfSight.TryGetTraversalCost(
                snapshot,
                layout,
                start,
                goal,
                footprint,
                out double directCost,
                clearanceApothemScale))
        {
            path = new HexWorldPath([start, goal], directCost, snapshot.Version);
            return true;
        }

        if (!TryGetAnchor(
                snapshot,
                layout,
                start,
                footprint,
                clearanceApothemScale,
                out HexagonalCubePoint startAnchor,
                out double startAnchorCost)
            || !TryGetAnchor(
                snapshot,
                layout,
                goal,
                footprint,
                clearanceApothemScale,
                out HexagonalCubePoint goalAnchor,
                out double goalAnchorCost)
            || !HexGridThetaStar.TryFindPath(
                snapshot,
                layout,
                startAnchor,
                goalAnchor,
                footprint,
                out HexGridPath? gridPath,
                clearanceApothemScale))
        {
            path = null;
            return false;
        }

        List<HexagonalWorldPoint> waypoints = [start];

        foreach (HexagonalCubePoint point in gridPath.Points)
        {
            AddWaypoint(waypoints, layout.GetCenter(point));
        }

        AddWaypoint(waypoints, goal);
        path = new HexWorldPath([.. waypoints], startAnchorCost + gridPath.Cost + goalAnchorCost, snapshot.Version);
        return true;
    }

    private static bool TryGetAnchor(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint position,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        out HexagonalCubePoint anchor,
        out double cost)
    {
        // 零长度查询会验证对象当前位置未与附近膨胀障碍重叠, 且位置位于快照范围内.
        if (!HexLineOfSight.TryGetTraversalCost(
                snapshot,
                layout,
                position,
                position,
                footprint,
                out _,
                clearanceApothemScale))
        {
            anchor = default;
            cost = 0;
            return false;
        }

        HexagonalCubePoint nearest = layout.GetNearestPoint(position);
        double bestCost = double.PositiveInfinity;
        HexagonalCubePoint bestAnchor = default;

        // 仅枚举最近格及其六个邻居, 既能绕过部分格心障碍, 又保持固定的小常数开销.
        foreach (HexagonalCubePoint candidate in nearest.RangeIn(1))
        {
            if (!snapshot.TryGetCell(candidate, out HexNavigationCell cell) || cell.HasObstacle)
            {
                continue;
            }

            if (!HexLineOfSight.TryGetTraversalCost(
                    snapshot,
                    layout,
                    position,
                    layout.GetCenter(candidate),
                    footprint,
                    out double candidateCost,
                    clearanceApothemScale)
                || candidateCost >= bestCost)
            {
                continue;
            }

            bestAnchor = candidate;
            bestCost = candidateCost;
        }

        if (double.IsPositiveInfinity(bestCost))
        {
            anchor = default;
            cost = 0;
            return false;
        }

        anchor = bestAnchor;
        cost = bestCost;
        return true;
    }

    private static void AddWaypoint(List<HexagonalWorldPoint> waypoints, HexagonalWorldPoint point)
    {
        if (waypoints[^1] != point)
        {
            waypoints.Add(point);
        }
    }
}
