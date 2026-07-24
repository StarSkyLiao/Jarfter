using System.Runtime.CompilerServices;
using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Pathfinding.Navigation;

internal static partial class HexLineOfSightTraversalEvaluator
{
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
