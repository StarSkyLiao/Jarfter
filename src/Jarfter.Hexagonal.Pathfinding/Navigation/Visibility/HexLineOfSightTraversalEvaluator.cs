using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 执行已验证线段的穿格、障碍检测和移动成本累计.
/// 该类型仅承载热路径计算, 不负责公共 API 参数校验.
/// </summary>
internal static partial class HexLineOfSightTraversalEvaluator
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
        if (snapshot is HexGridCentralNavigationSnapshot centralSnapshot)
        {
            return metrics is null
                ? TryGetTraversalCost(
                    new CentralNavigationCellAccessor(centralSnapshot),
                    default(NoLineOfSightMetrics),
                    centralSnapshot,
                    layout,
                    start,
                    end,
                    footprint,
                    out cost,
                    clearanceApothemScale,
                    costPolicy,
                    useObstacleChunkAcceleration)
                : TryGetTraversalCost(
                    new CentralNavigationCellAccessor(centralSnapshot),
                    new CollectingLineOfSightMetrics(metrics),
                    centralSnapshot,
                    layout,
                    start,
                    end,
                    footprint,
                    out cost,
                    clearanceApothemScale,
                    costPolicy,
                    useObstacleChunkAcceleration);
        }

        return metrics is null
            ? TryGetTraversalCost(
                new NavigationSnapshotCellAccessor(snapshot),
                default(NoLineOfSightMetrics),
                null,
                layout,
                start,
                end,
                footprint,
                out cost,
                clearanceApothemScale,
                costPolicy,
                useObstacleChunkAcceleration)
            : TryGetTraversalCost(
                new NavigationSnapshotCellAccessor(snapshot),
                new CollectingLineOfSightMetrics(metrics),
                null,
                layout,
                start,
                end,
                footprint,
                out cost,
                clearanceApothemScale,
                costPolicy,
                useObstacleChunkAcceleration);
    }

    private static bool TryGetTraversalCost<TCellAccessor, TMetrics>(
        TCellAccessor cellAccessor,
        TMetrics metrics,
        HexGridCentralNavigationSnapshot? centralSnapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalFootprint footprint,
        out double cost,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        bool useObstacleChunkAcceleration)
        where TCellAccessor : struct, INavigationCellAccessor
        where TMetrics : struct, ILineOfSightMetrics
    {
        double maximumObstacleApothemScale = centralSnapshot?.MaximumObstacleApothemScale ?? cellAccessor.MaximumObstacleApothemScale;
        int queryRadius = GetQueryRadius(
            maximumObstacleApothemScale,
            footprint.ApothemScale,
            clearanceApothemScale);
        double segmentLength = start.DistanceTo(end);
        double deltaX = end.X - start.X;
        double deltaY = end.Y - start.Y;
        ReadOnlySpan<HexagonalWorldPoint> sideNormals = HexagonalGeometry.GetSideNormals(layout.Orientation);
        double totalCost = 0;

        foreach (HexagonalSegmentCell traversedCell in HexagonalGeometry.TraverseSegment(layout, start, end))
        {
            metrics.AddTraversedCell();

            if (!cellAccessor.TryGetCell(traversedCell.Point, out HexNavigationCell traversedCellData))
            {
                cost = 0;
                return false;
            }

            if (useObstacleChunkAcceleration
                && centralSnapshot is not null
                && !centralSnapshot.HasObstacleInChunkRange(
                    traversedCell.Point.Q - queryRadius,
                    traversedCell.Point.Q + queryRadius,
                    traversedCell.Point.R - queryRadius,
                    traversedCell.Point.R + queryRadius))
            {
                // 块范围完全为空时, 六边形查询范围内也不可能存在障碍.
                metrics.AddObstacleFreeChunkRangeSkip();
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
                metrics.AddNearbyCellQuery();

                if (!cellAccessor.TryGetCell(candidate, out HexNavigationCell cell) || !cell.HasObstacle)
                {
                    continue;
                }

                metrics.AddObstacleIntersectionTest();

                double obstacleApothemScale = cell.ObstacleApothemScale
                    + footprint.ApothemScale
                    + clearanceApothemScale;

                if (HexagonalGeometry.SegmentIntersectsHexagonUnchecked(
                        layout,
                        start.X,
                        start.Y,
                        deltaX,
                        deltaY,
                        sideNormals,
                        candidate,
                        obstacleApothemScale))
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

}
