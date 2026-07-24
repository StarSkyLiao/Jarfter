using System.Diagnostics;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 提供 A* 与 Theta* 共用的格心搜索循环、成本模型和运行限制处理.
/// </summary>
internal static class HexGridSearch
{
    /// <summary>
    /// 异步执行指定连接规则的格心搜索.
    /// </summary>
    /// <param name="mode">要使用的连接规则.</param>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时与取消限制.</param>
    /// <returns>表示异步搜索操作的值任务. 成功时结果为格心航点路径; 失败时结果为 <see langword="null"/>.</returns>
    internal static ValueTask<HexGridPath?> FindPathAsync(
        HexGridSearchMode mode,
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy? costPolicy,
        HexPathfindingRequestOptions? requestOptions)
    {
        return new ValueTask<HexGridPath?>(Task.Run(
            () => FindPath(mode, snapshot, layout, start, goal, footprint, clearanceApothemScale, costPolicy, requestOptions),
            requestOptions?.CancellationToken ?? CancellationToken.None)
        );
    }

    /// <summary>
    /// 在当前线程中执行指定连接规则的格心搜索.
    /// </summary>
    /// <param name="mode">要使用的连接规则.</param>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时、取消与缓存策略.</param>
    /// <param name="maximumDistanceSum">当前内部搜索阶段允许的节点到起终点距离和上限; 为 <see langword="null"/> 时不限制.</param>
    /// <param name="runState">由范围扩张调用共享的累计运行状态; 常规单阶段搜索传入 <see langword="null"/>.</param>
    /// <returns>成功时得到格心航点路径; 失败时返回 <see langword="null"/>.</returns>
    internal static HexGridPath? FindPath(
        HexGridSearchMode mode,
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy? costPolicy,
        HexPathfindingRequestOptions? requestOptions,
        int? maximumDistanceSum = null,
        HexGridSearchRunState? runState = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(layout);

        IHexTraversalCostPolicy actualCostPolicy = costPolicy ?? HexTraversalMultiplierCostPolicy.Instance;
        HexGridSearchRuntime.ValidateCostPolicy(actualCostPolicy, nameof(costPolicy));
        requestOptions?.Validate();
        requestOptions?.CancellationToken.ThrowIfCancellationRequested();

        if (maximumDistanceSum is null
            && runState is null
            && requestOptions?.SearchScopeStrategy is IHexPathSearchScopeStrategy scopeStrategy)
        {
            return HexGridSearchScopeRunner.FindPath(
                mode,
                snapshot,
                layout,
                start,
                goal,
                footprint,
                clearanceApothemScale,
                costPolicy,
                requestOptions,
                scopeStrategy);
        }

        HexPathfindingStatisticsCollector? statisticsCollector = runState?.StatisticsCollector ?? (requestOptions?.CollectStatistics == true
            ? new HexPathfindingStatisticsCollector()
            : null);
        Dictionary<HexGridSearchTypes.LineOfSightCacheKey, HexGridSearchTypes.LineOfSightCacheEntry>? lineOfSightCache = runState?.LineOfSightCache ?? (HexGridSearchRuntime.ShouldEnableLineOfSightCache(mode, requestOptions)
            ? new Dictionary<HexGridSearchTypes.LineOfSightCacheKey, HexGridSearchTypes.LineOfSightCacheEntry>()
            : null);
        bool useObstacleChunkAcceleration = requestOptions?.UseObstacleChunkAcceleration ?? true;

        if (!TryGetTraversableCell(snapshot, start) || !TryGetTraversableCell(snapshot, goal))
        {
            return null;
        }

        if (start == goal)
        {
            return new HexGridPath([start], 0, snapshot.Version, statisticsCollector?.CreateStatistics());
        }

        HexagonalWorldPoint goalCenter = layout.GetCenter(goal);
        Dictionary<HexagonalCubePoint, HexGridSearchTypes.SparseNodeRecord> records = new Dictionary<HexagonalCubePoint, HexGridSearchTypes.SparseNodeRecord>();
        PriorityQueue<OpenNode, double> openSet = new PriorityQueue<OpenNode, double>();
        HashSet<HexagonalCubePoint> closedSet = new HashSet<HexagonalCubePoint>();
        records.Add(start, new HexGridSearchTypes.SparseNodeRecord(0, default, false));
        openSet.Enqueue(
            new OpenNode(start, 0),
            HexGridSearchRuntime.GetHeuristicCost(layout.GetCenter(start), goalCenter, actualCostPolicy.MinimumCostPerUnitLength));
        long startTimestamp = runState?.StartTimestamp ?? Stopwatch.GetTimestamp();
        int expandedNodeCount = 0;

        while (openSet.TryDequeue(out OpenNode openNode, out _))
        {
            if (!records.TryGetValue(openNode.Point, out HexGridSearchTypes.SparseNodeRecord currentRecord)
                || openNode.Cost != currentRecord.Cost
                || !closedSet.Add(openNode.Point))
            {
                continue;
            }

            requestOptions?.CancellationToken.ThrowIfCancellationRequested();

            if (HexGridSearchRuntime.IsTimeoutExpired(requestOptions, startTimestamp))
            {
                return null;
            }

            if (openNode.Point == goal)
            {
                return HexGridPathReconstructor.ReconstructSparsePath(records, goal, currentRecord.Cost, snapshot.Version, statisticsCollector);
            }

            if (!HexGridSearchRuntime.TryConsumeExpansionBudget(requestOptions, runState, ref expandedNodeCount, statisticsCollector))
            {
                return null;
            }

            for (int direction = 0; direction < 6; direction++)
            {
                HexagonalCubePoint neighbor = openNode.Point.NeighborAtUnchecked(direction);
                if (closedSet.Contains(neighbor)
                    || !HexGridSearchRuntime.IsWithinSearchScope(start, goal, neighbor, maximumDistanceSum)
                    || !TryGetTraversableCell(snapshot, neighbor))
                {
                    continue;
                }

                if (!HexGridConnectionEvaluator.TryGetBestConnection(
                        mode,
                        snapshot,
                        layout,
                        records,
                        openNode.Point,
                        currentRecord,
                        neighbor,
                        footprint,
                        clearanceApothemScale,
                        actualCostPolicy,
                        statisticsCollector,
                        lineOfSightCache,
                        useObstacleChunkAcceleration,
                        out HexagonalCubePoint parent,
                        out double neighborCost))
                {
                    continue;
                }

                if (records.TryGetValue(neighbor, out HexGridSearchTypes.SparseNodeRecord existingRecord) && neighborCost >= existingRecord.Cost)
                {
                    continue;
                }

                records[neighbor] = new HexGridSearchTypes.SparseNodeRecord(neighborCost, parent, true);
                double priority = neighborCost + HexGridSearchRuntime.GetHeuristicCost(
                    layout.GetCenter(neighbor),
                    goalCenter,
                    actualCostPolicy.MinimumCostPerUnitLength);
                openSet.Enqueue(new OpenNode(neighbor, neighborCost), priority);
            }
        }

        return null;
    }

    /// <summary>
    /// 使用与中心稠密快照匹配的可复用工作区执行格心搜索.
    /// </summary>
    /// <param name="mode">要使用的连接规则.</param>
    /// <param name="snapshot">要读取的中心稠密导航地图快照.</param>
    /// <param name="workspace">复用搜索状态的工作区.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="costPolicy">计算主穿格移动成本的策略.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时、取消与缓存策略.</param>
    /// <param name="maximumDistanceSum">当前内部搜索阶段允许的节点到起终点距离和上限; 为 <see langword="null"/> 时不限制.</param>
    /// <param name="runState">由范围扩张调用共享的累计运行状态; 常规单阶段搜索传入 <see langword="null"/>.</param>
    /// <returns>成功时得到格心航点路径; 失败时返回 <see langword="null"/>.</returns>
    internal static HexGridPath? FindPath(
        HexGridSearchMode mode,
        HexGridCentralNavigationSnapshot snapshot,
        HexGridPathfindingWorkspace workspace,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy? costPolicy,
        HexPathfindingRequestOptions? requestOptions,
        int? maximumDistanceSum = null,
        HexGridSearchRunState? runState = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(layout);

        if (!ReferenceEquals(snapshot.Bake, workspace.Bake))
        {
            throw new ArgumentException("工作区必须使用当前快照的烘焙地图创建.", nameof(workspace));
        }

        IHexTraversalCostPolicy actualCostPolicy = costPolicy ?? HexTraversalMultiplierCostPolicy.Instance;
        HexGridSearchRuntime.ValidateCostPolicy(actualCostPolicy, nameof(costPolicy));
        requestOptions?.Validate();
        requestOptions?.CancellationToken.ThrowIfCancellationRequested();

        if (maximumDistanceSum is null
            && runState is null
            && requestOptions?.SearchScopeStrategy is IHexPathSearchScopeStrategy scopeStrategy)
        {
            return HexGridSearchScopeRunner.FindPath(
                mode,
                snapshot,
                workspace,
                layout,
                start,
                goal,
                footprint,
                clearanceApothemScale,
                costPolicy,
                requestOptions,
                scopeStrategy);
        }

        HexPathfindingStatisticsCollector? statisticsCollector = runState?.StatisticsCollector ?? (requestOptions?.CollectStatistics == true
            ? new HexPathfindingStatisticsCollector()
            : null);
        workspace.BeginSearch(HexGridSearchRuntime.ShouldEnableLineOfSightCache(mode, requestOptions), runState?.HasStarted == true);
        runState?.MarkStarted();
        bool useObstacleChunkAcceleration = requestOptions?.UseObstacleChunkAcceleration ?? true;

        HexGridCentralNavigationBake bake = workspace.Bake;

        if (!bake.TryGetIndex(start, out int startIndex)
            || !bake.TryGetIndex(goal, out int goalIndex)
            || !TryGetTraversableCell(snapshot, start)
            || !TryGetTraversableCell(snapshot, goal))
        {
            return null;
        }

        if (startIndex == goalIndex)
        {
            return new HexGridPath([start], 0, snapshot.Version, statisticsCollector?.CreateStatistics());
        }

        HexagonalWorldPoint goalCenter = layout.GetCenter(goal);
        workspace.SetRecord(startIndex, 0, -1);
        workspace.EnqueueOrDecreasePriority(
            startIndex,
            HexGridSearchRuntime.GetHeuristicCost(layout.GetCenter(start), goalCenter, actualCostPolicy.MinimumCostPerUnitLength));
        long startTimestamp = runState?.StartTimestamp ?? Stopwatch.GetTimestamp();
        int expandedNodeCount = 0;

        while (workspace.TryDequeue(out int currentIndex))
        {
            if (workspace.IsClosed(currentIndex))
            {
                continue;
            }

            workspace.Close(currentIndex);
            workspace.TryGetRecord(currentIndex, out double currentCost, out int currentParentIndex);
            requestOptions?.CancellationToken.ThrowIfCancellationRequested();

            if (HexGridSearchRuntime.IsTimeoutExpired(requestOptions, startTimestamp))
            {
                return null;
            }

            if (currentIndex == goalIndex)
            {
                return HexGridPathReconstructor.ReconstructBakedPath(workspace, goalIndex, currentCost, snapshot.Version, statisticsCollector);
            }

            if (!HexGridSearchRuntime.TryConsumeExpansionBudget(requestOptions, runState, ref expandedNodeCount, statisticsCollector))
            {
                return null;
            }

            for (int direction = 0; direction < 6; direction++)
            {
                int neighborIndex = bake.GetNeighborIndex(currentIndex, direction);
                if (neighborIndex < 0 || workspace.IsClosed(neighborIndex))
                {
                    continue;
                }

                HexagonalCubePoint neighbor = bake.GetPoint(neighborIndex);
                if (!HexGridSearchRuntime.IsWithinSearchScope(start, goal, neighbor, maximumDistanceSum)
                    || !TryGetTraversableCell(snapshot, neighbor)
                    || !HexGridConnectionEvaluator.TryGetBakedBestConnection(
                        mode,
                        snapshot,
                        layout,
                        workspace,
                        currentIndex,
                        currentCost,
                        currentParentIndex,
                        neighborIndex,
                        footprint,
                        clearanceApothemScale,
                        actualCostPolicy,
                        statisticsCollector,
                        useObstacleChunkAcceleration,
                        out int parentIndex,
                        out double neighborCost))
                {
                    continue;
                }

                if (workspace.TryGetRecord(neighborIndex, out double existingCost, out _) && neighborCost >= existingCost)
                {
                    continue;
                }

                workspace.SetRecord(neighborIndex, neighborCost, parentIndex);
                double priority = neighborCost + HexGridSearchRuntime.GetHeuristicCost(
                    layout.GetCenter(neighbor),
                    goalCenter,
                    actualCostPolicy.MinimumCostPerUnitLength);
                workspace.EnqueueOrDecreasePriority(neighborIndex, priority);
            }
        }

        return null;
    }

    private static bool TryGetTraversableCell(IHexNavigationSnapshot snapshot, HexagonalCubePoint point)
    {
        return snapshot.TryGetCell(point, out HexNavigationCell cell) && !cell.HasObstacle;
    }

    private readonly record struct OpenNode(HexagonalCubePoint Point, double Cost);


}
