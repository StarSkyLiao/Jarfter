using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 执行已验证线段的穿格、障碍检测和移动成本累计.
/// 该类型仅承载热路径计算, 不负责公共 API 参数校验.
/// </summary>
internal static class HexLineOfSightTraversalEvaluator
{
    /// <summary>
    /// 尝试计算线段的可通行累计成本.
    /// </summary>
    internal static bool TryGetTraversalCost(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalFootprint footprint,
        out double cost,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        HexLineOfSightMetrics? metrics,
        bool useObstacleChunkAcceleration)
    {
        int queryRadius = GetQueryRadius(
            snapshot.MaximumObstacleApothemScale,
            footprint.ApothemScale,
            clearanceApothemScale);
        double segmentLength = start.DistanceTo(end);
        double totalCost = 0;

        foreach (HexagonalSegmentCell traversedCell in HexNavigationGeometry.TraverseSegment(layout, start, end))
        {
            metrics?.AddTraversedCell();

            if (!snapshot.TryGetCell(traversedCell.Point, out HexNavigationCell traversedCellData))
            {
                cost = 0;
                return false;
            }

            if (useObstacleChunkAcceleration
                && snapshot is HexGridCentralNavigationSnapshot centralSnapshot
                && !centralSnapshot.HasObstacleInChunkRange(
                    traversedCell.Point.Q - queryRadius,
                    traversedCell.Point.Q + queryRadius,
                    traversedCell.Point.R - queryRadius,
                    traversedCell.Point.R + queryRadius))
            {
                // 块范围完全为空时, 六边形查询范围内也不可能存在障碍.
                metrics?.AddObstacleFreeChunkRangeSkip();
                double emptySectionLength = segmentLength * (traversedCell.EndFraction - traversedCell.StartFraction);
                double emptySectionCost = costPolicy.GetTraversalCost(emptySectionLength, traversedCellData);

                if (!double.IsFinite(emptySectionCost) || emptySectionCost < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(costPolicy));
                }

                totalCost += emptySectionCost;
                continue;
            }

            foreach (HexagonalCubePoint candidate in traversedCell.Point.RangeIn(queryRadius))
            {
                metrics?.AddNearbyCellQuery();

                if (!snapshot.TryGetCell(candidate, out HexNavigationCell cell) || !cell.HasObstacle)
                {
                    continue;
                }

                metrics?.AddObstacleIntersectionTest();

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
            double sectionCost = costPolicy.GetTraversalCost(sectionLength, traversedCellData);

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
