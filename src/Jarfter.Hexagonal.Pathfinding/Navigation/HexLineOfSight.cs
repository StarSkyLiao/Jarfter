using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 提供基于导航快照的保守六边形障碍视线检测.
/// 检测仅查询线段附近的格子, 并将障碍按移动足迹和安全边距膨胀.
/// </summary>
public static class HexLineOfSight
{
    /// <summary>
    /// 判断指定线段是否不接触快照中的任何膨胀后障碍.
    /// 线段贴公共边或顶点时会额外检查附近格子, 以避免从零宽缝隙通过.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">线段起点.</param>
    /// <param name="end">线段终点.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略; 为 <see langword="null"/> 时使用默认地形倍率策略.</param>
    /// <returns>当线段不接触任何膨胀后障碍时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="snapshot"/> 或 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当足迹、边距或快照障碍尺寸无效时抛出.</exception>
    public static bool HasLineOfSight(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        IHexTraversalCostPolicy? costPolicy = null)
    {
        return TryGetTraversalCost(
            snapshot,
            layout,
            start,
            end,
            footprint,
            out _,
            clearanceApothemScale,
            costPolicy);
    }

    /// <summary>
    /// 尝试获取线段在快照中的可通行累计成本.
    /// 成本按线段在每个主穿格内的实际长度和指定策略累计.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">线段起点.</param>
    /// <param name="end">线段终点.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="cost">线段可通行时得到的累计移动成本.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略; 为 <see langword="null"/> 时使用默认地形倍率策略.</param>
    /// <returns>当线段位于快照范围内且不接触任何膨胀后障碍时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="snapshot"/> 或 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当足迹、边距或快照障碍尺寸无效时抛出.</exception>
    public static bool TryGetTraversalCost(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalFootprint footprint,
        out double cost,
        double clearanceApothemScale = 0,
        IHexTraversalCostPolicy? costPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(layout);

        IHexTraversalCostPolicy actualCostPolicy = costPolicy ?? HexTraversalMultiplierCostPolicy.Instance;

        if (!double.IsFinite(actualCostPolicy.MinimumCostPerUnitLength) || actualCostPolicy.MinimumCostPerUnitLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        }

        if (!double.IsFinite(footprint.ApothemScale) || footprint.ApothemScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(footprint));
        }

        if (!double.IsFinite(clearanceApothemScale) || clearanceApothemScale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clearanceApothemScale));
        }

        if (!double.IsFinite(snapshot.MaximumObstacleApothemScale) || snapshot.MaximumObstacleApothemScale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshot));
        }

        int queryRadius = GetQueryRadius(
            snapshot.MaximumObstacleApothemScale,
            footprint.ApothemScale,
            clearanceApothemScale);
        double segmentLength = start.DistanceTo(end);
        double totalCost = 0;

        foreach (HexagonalSegmentCell traversedCell in HexNavigationGeometry.TraverseSegment(layout, start, end))
        {
            if (!snapshot.TryGetCell(traversedCell.Point, out HexNavigationCell traversedCellData))
            {
                cost = 0;
                return false;
            }

            foreach (HexagonalCubePoint candidate in traversedCell.Point.RangeIn(queryRadius))
            {
                if (!snapshot.TryGetCell(candidate, out HexNavigationCell cell) || !cell.HasObstacle)
                {
                    continue;
                }

                if (HexNavigationGeometry.SegmentIntersectsInflatedHexagonUnchecked(
                        layout,
                        start,
                        end,
                        candidate,
                        cell.ObstacleApothemScale,
                        footprint.ApothemScale,
                        clearanceApothemScale))
                {
                    cost = 0;
                    return false;
                }
            }

            double sectionLength = segmentLength * (traversedCell.EndFraction - traversedCell.StartFraction);
            double sectionCost = actualCostPolicy.GetTraversalCost(sectionLength, traversedCellData);

            if (!double.IsFinite(sectionCost) || sectionCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(costPolicy));
            }

            totalCost += sectionCost;
        }

        if (!double.IsFinite(totalCost))
        {
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        }

        cost = totalCost;
        return true;
    }

    private static int GetQueryRadius(
        double maximumObstacleApothemScale,
        double footprintApothemScale,
        double clearanceApothemScale)
    {
        double effectiveApothemScale = maximumObstacleApothemScale
            + footprintApothemScale
            + clearanceApothemScale;
        double radius = Math.Ceiling(2 * (1 + effectiveApothemScale) / 3);

        return checked((int)Math.Max(1, radius));
    }
}
