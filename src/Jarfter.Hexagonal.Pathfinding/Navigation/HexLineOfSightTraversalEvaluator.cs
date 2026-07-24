using System.Runtime.CompilerServices;
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
        ReadOnlySpan<HexagonalWorldPoint> sideNormals = HexNavigationGeometry.GetSideNormals(layout.Orientation);
        double totalCost = 0;

        foreach (HexagonalSegmentCell traversedCell in HexNavigationGeometry.TraverseSegment(layout, start, end))
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

                if (HexNavigationGeometry.SegmentIntersectsInflatedHexagonUnchecked(
                        layout,
                        start.X,
                        start.Y,
                        deltaX,
                        deltaY,
                        sideNormals,
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

    private interface ILineOfSightMetrics
    {
        void AddTraversedCell();

        void AddNearbyCellQuery();

        void AddObstacleIntersectionTest();

        void AddObstacleFreeChunkRangeSkip();
    }

    private readonly struct NoLineOfSightMetrics : ILineOfSightMetrics
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTraversedCell()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNearbyCellQuery()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddObstacleIntersectionTest()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddObstacleFreeChunkRangeSkip()
        {
        }
    }

    private readonly struct CollectingLineOfSightMetrics : ILineOfSightMetrics
    {
        private readonly HexLineOfSightMetrics m_Metrics;

        internal CollectingLineOfSightMetrics(HexLineOfSightMetrics metrics)
        {
            m_Metrics = metrics;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddTraversedCell()
        {
            m_Metrics.AddTraversedCell();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNearbyCellQuery()
        {
            m_Metrics.AddNearbyCellQuery();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddObstacleIntersectionTest()
        {
            m_Metrics.AddObstacleIntersectionTest();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddObstacleFreeChunkRangeSkip()
        {
            m_Metrics.AddObstacleFreeChunkRangeSkip();
        }
    }

    private interface INavigationCellAccessor
    {
        double MaximumObstacleApothemScale { get; }

        bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell);
    }

    private readonly struct CentralNavigationCellAccessor : INavigationCellAccessor
    {
        private readonly HexGridCentralNavigationSnapshot m_Snapshot;

        internal CentralNavigationCellAccessor(HexGridCentralNavigationSnapshot snapshot)
        {
            m_Snapshot = snapshot;
        }

        public double MaximumObstacleApothemScale => m_Snapshot.MaximumObstacleApothemScale;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell)
        {
            return m_Snapshot.TryGetCell(point, out cell);
        }
    }

    private readonly struct NavigationSnapshotCellAccessor : INavigationCellAccessor
    {
        private readonly IHexNavigationSnapshot m_Snapshot;

        internal NavigationSnapshotCellAccessor(IHexNavigationSnapshot snapshot)
        {
            m_Snapshot = snapshot;
        }

        public double MaximumObstacleApothemScale => m_Snapshot.MaximumObstacleApothemScale;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetCell(HexagonalCubePoint point, out HexNavigationCell cell)
        {
            return m_Snapshot.TryGetCell(point, out cell);
        }
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
