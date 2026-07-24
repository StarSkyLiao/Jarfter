using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Grid.Results;
using Jarfter.Hexagonal.Pathfinding.Grid.Runtime;
using Jarfter.Hexagonal.Pathfinding.Navigation.Central;
using Jarfter.Hexagonal.Pathfinding.Navigation.Model;
using Jarfter.Hexagonal.Pathfinding.Navigation.Visibility;

namespace Jarfter.Hexagonal.Pathfinding.Grid.Internal;

/// <summary>
/// 评估格心之间的 A* 相邻连接与 Theta* 父节点直视连接.
/// 该类型集中处理直视缓存、成本计算和诊断计数; 搜索引擎只负责节点展开、记录更新和优先队列维护.
/// </summary>
internal static class HexGridConnectionEvaluator
{
    /// <summary>
    /// 为中心稠密工作区搜索后端计算到相邻节点的最低成本连接.
    /// </summary>
    internal static bool TryGetBakedBestConnection(
        HexGridSearchMode mode,
        HexGridCentralNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexGridPathfindingWorkspace workspace,
        int currentIndex,
        double currentCost,
        int currentParentIndex,
        int neighborIndex,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        HexPathfindingStatisticsCollector? statisticsCollector,
        bool useObstacleChunkAcceleration,
        out int parentIndex,
        out double cost)
    {
        HexGridCentralNavigationBake bake = workspace.Bake;

        if (mode == HexGridSearchMode.ThetaStar && currentParentIndex >= 0)
        {
            workspace.TryGetRecord(currentParentIndex, out double parentCost, out _);
            statisticsCollector?.AddLineOfSightQuery(true);

            if (TryGetBakedTraversalCost(
                    snapshot,
                    layout,
                    workspace,
                    currentParentIndex,
                    neighborIndex,
                    footprint,
                    out double parentConnectionCost,
                    clearanceApothemScale,
                    costPolicy,
                    statisticsCollector,
                    useObstacleChunkAcceleration))
            {
                statisticsCollector?.AddSuccessfulParentLineOfSightQuery();
                parentIndex = currentParentIndex;
                cost = parentCost + parentConnectionCost;
                return true;
            }
        }

        statisticsCollector?.AddLineOfSightQuery(false);

        if (TryGetBakedTraversalCost(
                snapshot,
                layout,
                workspace,
                currentIndex,
                neighborIndex,
                footprint,
                out double connectionCost,
                clearanceApothemScale,
                costPolicy,
                statisticsCollector,
                useObstacleChunkAcceleration))
        {
            parentIndex = currentIndex;
            cost = currentCost + connectionCost;
            return true;
        }

        parentIndex = -1;
        cost = 0;
        return false;
    }

    /// <summary>
    /// 为稀疏搜索后端计算到相邻节点的最低成本连接.
    /// </summary>
    internal static bool TryGetBestConnection(
        HexGridSearchMode mode,
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        IReadOnlyDictionary<HexagonalCubePoint, HexGridSearch.SparseNodeRecord> records,
        HexagonalCubePoint current,
        HexGridSearch.SparseNodeRecord currentRecord,
        HexagonalCubePoint neighbor,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        HexPathfindingStatisticsCollector? statisticsCollector,
        Dictionary<HexGridSearch.LineOfSightCacheKey, HexGridSearch.LineOfSightCacheEntry>? lineOfSightCache,
        bool useObstacleChunkAcceleration,
        out HexagonalCubePoint parent,
        out double cost)
    {
        if (mode == HexGridSearchMode.ThetaStar && currentRecord.HasParent)
        {
            HexGridSearch.SparseNodeRecord parentRecord = records[currentRecord.Parent];
            statisticsCollector?.AddLineOfSightQuery(true);

            if (TryGetTraversalCost(
                    snapshot,
                    layout,
                    currentRecord.Parent,
                    neighbor,
                    footprint,
                    out double parentConnectionCost,
                    clearanceApothemScale,
                    costPolicy,
                    statisticsCollector,
                    lineOfSightCache,
                    useObstacleChunkAcceleration))
            {
                statisticsCollector?.AddSuccessfulParentLineOfSightQuery();
                parent = currentRecord.Parent;
                cost = parentRecord.Cost + parentConnectionCost;
                return true;
            }
        }

        statisticsCollector?.AddLineOfSightQuery(false);

        if (TryGetTraversalCost(
                snapshot,
                layout,
                current,
                neighbor,
                footprint,
                out double connectionCost,
                clearanceApothemScale,
                costPolicy,
                statisticsCollector,
                lineOfSightCache,
                useObstacleChunkAcceleration))
        {
            parent = current;
            cost = currentRecord.Cost + connectionCost;
            return true;
        }

        parent = default;
        cost = 0;
        return false;
    }

    private static bool TryGetBakedTraversalCost(
        HexGridCentralNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexGridPathfindingWorkspace workspace,
        int startIndex,
        int endIndex,
        HexagonalFootprint footprint,
        out double cost,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        HexPathfindingStatisticsCollector? statisticsCollector,
        bool useObstacleChunkAcceleration)
    {
        HexGridCentralNavigationBake bake = workspace.Bake;
        HexagonalCubePoint start = bake.GetPoint(startIndex);
        HexagonalCubePoint end = bake.GetPoint(endIndex);

        if (workspace.TryGetLineOfSightCache(start, end, out bool cachedTraversable, out cost))
        {
            statisticsCollector?.AddLineOfSightCacheHit();
            return cachedTraversable;
        }

        if (workspace.UsesLineOfSightCache)
        {
            statisticsCollector?.AddLineOfSightCacheMiss();
        }

        bool isTraversable = HexLineOfSight.TryGetTraversalCost(
            snapshot,
            layout,
            layout.GetCenter(start),
            layout.GetCenter(end),
            footprint,
            out cost,
            clearanceApothemScale,
            costPolicy,
            statisticsCollector?.LineOfSightMetrics,
            useObstacleChunkAcceleration);

        workspace.SetLineOfSightCache(start, end, isTraversable, cost);
        return isTraversable;
    }

    private static bool TryGetTraversalCost(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint end,
        HexagonalFootprint footprint,
        out double cost,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        HexPathfindingStatisticsCollector? statisticsCollector,
        Dictionary<HexGridSearch.LineOfSightCacheKey, HexGridSearch.LineOfSightCacheEntry>? lineOfSightCache,
        bool useObstacleChunkAcceleration)
    {
        HexGridSearch.LineOfSightCacheKey key = new HexGridSearch.LineOfSightCacheKey(start, end);

        if (lineOfSightCache is not null && lineOfSightCache.TryGetValue(key, out HexGridSearch.LineOfSightCacheEntry cacheEntry))
        {
            statisticsCollector?.AddLineOfSightCacheHit();
            cost = cacheEntry.Cost;
            return cacheEntry.IsTraversable;
        }

        if (lineOfSightCache is not null)
        {
            statisticsCollector?.AddLineOfSightCacheMiss();
        }

        bool isTraversable = HexLineOfSight.TryGetTraversalCost(
            snapshot,
            layout,
            layout.GetCenter(start),
            layout.GetCenter(end),
            footprint,
            out cost,
            clearanceApothemScale,
            costPolicy,
            statisticsCollector?.LineOfSightMetrics,
            useObstacleChunkAcceleration);

        lineOfSightCache?.Add(key, new HexGridSearch.LineOfSightCacheEntry(isTraversable, cost));
        return isTraversable;
    }
}
