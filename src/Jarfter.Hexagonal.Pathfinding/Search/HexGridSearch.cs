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

    private static HexGridPath? FindPath(
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
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(layout);

        IHexTraversalCostPolicy actualCostPolicy = costPolicy ?? HexTraversalMultiplierCostPolicy.Instance;
        ValidateCostPolicy(actualCostPolicy, nameof(costPolicy));
        requestOptions?.Validate();
        requestOptions?.CancellationToken.ThrowIfCancellationRequested();

        if (!TryGetTraversableCell(snapshot, start) || !TryGetTraversableCell(snapshot, goal))
        {
            return null;
        }

        if (start == goal)
        {
            return new HexGridPath([start], 0, snapshot.Version);
        }

        HexagonalWorldPoint goalCenter = layout.GetCenter(goal);
        Dictionary<HexagonalCubePoint, NodeRecord> records = new Dictionary<HexagonalCubePoint, NodeRecord>();
        PriorityQueue<OpenNode, double> openSet = new PriorityQueue<OpenNode, double>();
        HashSet<HexagonalCubePoint> closedSet = new HashSet<HexagonalCubePoint>();
        records.Add(start, new NodeRecord(0, default, false));
        openSet.Enqueue(
            new OpenNode(start, 0),
            GetHeuristicCost(layout.GetCenter(start), goalCenter, actualCostPolicy.MinimumCostPerUnitLength));
        long startTimestamp = Stopwatch.GetTimestamp();
        int expandedNodeCount = 0;

        while (openSet.TryDequeue(out OpenNode openNode, out _))
        {
            if (!records.TryGetValue(openNode.Point, out NodeRecord currentRecord)
                || openNode.Cost != currentRecord.Cost
                || !closedSet.Add(openNode.Point))
            {
                continue;
            }

            requestOptions?.CancellationToken.ThrowIfCancellationRequested();

            if (IsTimeoutExpired(requestOptions, startTimestamp))
            {
                return null;
            }

            if (openNode.Point == goal)
            {
                return ReconstructPath(records, goal, currentRecord.Cost, snapshot.Version);
            }

            if (requestOptions is { MaximumExpandedNodeCount: > 0 } && expandedNodeCount >= requestOptions.MaximumExpandedNodeCount)
            {
                return null;
            }

            expandedNodeCount++;

            for (int direction = 0; direction < 6; direction++)
            {
                HexagonalCubePoint neighbor = openNode.Point.NeighborAtUnchecked(direction);
                if (closedSet.Contains(neighbor) || !TryGetTraversableCell(snapshot, neighbor))
                {
                    continue;
                }

                if (!TryGetBestConnection(
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
                        out HexagonalCubePoint parent,
                        out double neighborCost))
                {
                    continue;
                }

                if (records.TryGetValue(neighbor, out NodeRecord existingRecord) && neighborCost >= existingRecord.Cost)
                {
                    continue;
                }

                records[neighbor] = new NodeRecord(neighborCost, parent, true);
                double priority = neighborCost + GetHeuristicCost(
                    layout.GetCenter(neighbor),
                    goalCenter,
                    actualCostPolicy.MinimumCostPerUnitLength);
                openSet.Enqueue(new OpenNode(neighbor, neighborCost), priority);
            }
        }

        return null;
    }

    private static bool TryGetBestConnection(
        HexGridSearchMode mode,
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        IReadOnlyDictionary<HexagonalCubePoint, NodeRecord> records,
        HexagonalCubePoint current,
        NodeRecord currentRecord,
        HexagonalCubePoint neighbor,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy costPolicy,
        out HexagonalCubePoint parent,
        out double cost)
    {
        if (mode == HexGridSearchMode.ThetaStar && currentRecord.HasParent)
        {
            NodeRecord parentRecord = records[currentRecord.Parent];

            if (HexLineOfSight.TryGetTraversalCost(
                    snapshot,
                    layout,
                    layout.GetCenter(currentRecord.Parent),
                    layout.GetCenter(neighbor),
                    footprint,
                    out double parentConnectionCost,
                    clearanceApothemScale,
                    costPolicy))
            {
                parent = currentRecord.Parent;
                cost = parentRecord.Cost + parentConnectionCost;
                return true;
            }
        }

        if (HexLineOfSight.TryGetTraversalCost(
                snapshot,
                layout,
                layout.GetCenter(current),
                layout.GetCenter(neighbor),
                footprint,
                out double connectionCost,
                clearanceApothemScale,
                costPolicy))
        {
            parent = current;
            cost = currentRecord.Cost + connectionCost;
            return true;
        }

        parent = default;
        cost = 0;
        return false;
    }

    private static bool TryGetTraversableCell(IHexNavigationSnapshot snapshot, HexagonalCubePoint point)
    {
        return snapshot.TryGetCell(point, out HexNavigationCell cell) && !cell.HasObstacle;
    }

    private static double GetHeuristicCost(
        HexagonalWorldPoint point,
        HexagonalWorldPoint goal,
        double minimumCostPerUnitLength)
    {
        return point.DistanceTo(goal) * minimumCostPerUnitLength;
    }

    private static HexGridPath ReconstructPath(
        IReadOnlyDictionary<HexagonalCubePoint, NodeRecord> records,
        HexagonalCubePoint goal,
        double cost,
        long navigationVersion)
    {
        List<HexagonalCubePoint> points = [];
        HexagonalCubePoint current = goal;

        while (true)
        {
            points.Add(current);
            NodeRecord record = records[current];
            if (!record.HasParent)
            {
                break;
            }

            current = record.Parent;
        }

        points.Reverse();
        return new HexGridPath([.. points], cost, navigationVersion);
    }

    private static void ValidateCostPolicy(IHexTraversalCostPolicy costPolicy, string parameterName)
    {
        if (!double.IsFinite(costPolicy.MinimumCostPerUnitLength) || costPolicy.MinimumCostPerUnitLength < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static bool IsTimeoutExpired(HexPathfindingRequestOptions? requestOptions, long startTimestamp)
    {
        return requestOptions is not null
            && requestOptions.Timeout != Timeout.InfiniteTimeSpan
            && Stopwatch.GetElapsedTime(startTimestamp) >= requestOptions.Timeout;
    }

    private readonly record struct OpenNode(HexagonalCubePoint Point, double Cost);

    private readonly record struct NodeRecord(double Cost, HexagonalCubePoint Parent, bool HasParent);
}
