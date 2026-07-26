using Jarfter.Core.Collections.ObjectModel;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示用于补足规则 NavMesh 窄通道的地图级局部细化六边形导航图.
/// 节点拓扑仅由不可变地图快照确定, 与单次请求的单位半径无关; 查询时再按单位半径筛选节点和边, 保证较小单位的候选空间包含较大单位的候选空间.
/// </summary>
internal sealed class ContinuousAdaptiveHexNavigationGraph
{
    private const int StartNodeIndex = -1;
    private const int GoalNodeIndex = -2;
    private const int PreferredConnectionRadius = 2;

    private readonly IContinuousNavigationSnapshot m_Snapshot;
    private readonly HexCubePoint m_Origin;
    private readonly double m_FineSpacing;
    private readonly int m_FineRadius;
    private readonly HexCubeGridPoint[] m_GridPositions;
    private readonly HexCubePoint[] m_Positions;
    private readonly Dictionary<HexCubeGridPoint, int> m_NodeIndices;
    private readonly object m_TraversalCacheSync = new object();
    private AdaptiveTraversalCache? m_TraversalCache;

    private ContinuousAdaptiveHexNavigationGraph(
        IContinuousNavigationSnapshot snapshot,
        HexCubePoint origin,
        double fineSpacing,
        int fineRadius,
        HexCubeGridPoint[] gridPositions,
        HexCubePoint[] positions,
        Dictionary<HexCubeGridPoint, int> nodeIndices)
    {
        m_Snapshot = snapshot;
        m_Origin = origin;
        m_FineSpacing = fineSpacing;
        m_FineRadius = fineRadius;
        m_GridPositions = gridPositions;
        m_Positions = positions;
        m_NodeIndices = nodeIndices;
    }

    /// <summary>
    /// 根据指定不可变地图快照构建局部细化导航图.
    /// </summary>
    /// <param name="snapshot">用于构建的不可变导航快照.</param>
    /// <param name="bounds">限制节点范围的有限导航边界.</param>
    /// <param name="cellSpacing">规则 NavMesh 的原始单元间距.</param>
    /// <returns>开放区域使用原始间距、障碍物附近使用半间距的导航图.</returns>
    internal static ContinuousAdaptiveHexNavigationGraph Build(
        IContinuousNavigationSnapshot snapshot,
        ContinuousNavigationBounds bounds,
        double cellSpacing)
    {
        double fineSpacing = cellSpacing / 2;
        int fineRadius = (int)Math.Ceiling(bounds.Shape.RadiusScale / fineSpacing);
        Dictionary<HexCubeGridPoint, int> nodeIndices = [];
        List<HexCubeGridPoint> gridPositions = [];
        List<HexCubePoint> positions = [];

        // 开放区域只保留粗节点, 避免把全图细化为固定高开销网格.
        int coarseRadius = (int)Math.Ceiling(bounds.Shape.RadiusScale / cellSpacing);

        for (int q = -coarseRadius; q <= coarseRadius; q++)
        {
            for (int r = -coarseRadius; r <= coarseRadius; r++)
            {
                AddNode(new HexCubeGridPoint(q * 2, r * 2));
            }
        }

        foreach (HexCubeArea2D obstacle in snapshot.Obstacles)
        {
            // 细化带与本次单位半径无关, 避免不同半径拥有不嵌套的候选拓扑.
            // 两个粗网格间距可覆盖障碍物两侧的细节点与粗节点连接区域.
            double refinementRadius = obstacle.RadiusScale + cellSpacing * 2;
            HexCubePoint relativePosition = obstacle.Position - bounds.Shape.Position;
            int minimumQ = Math.Max(-fineRadius, (int)Math.Floor((relativePosition.Q - refinementRadius) / fineSpacing));
            int maximumQ = Math.Min(fineRadius, (int)Math.Ceiling((relativePosition.Q + refinementRadius) / fineSpacing));
            int minimumR = Math.Max(-fineRadius, (int)Math.Floor((relativePosition.R - refinementRadius) / fineSpacing));
            int maximumR = Math.Min(fineRadius, (int)Math.Ceiling((relativePosition.R + refinementRadius) / fineSpacing));

            for (int q = minimumQ; q <= maximumQ; q++)
            {
                for (int r = minimumR; r <= maximumR; r++)
                {
                    AddNode(new HexCubeGridPoint(q, r));
                }
            }
        }

        return new ContinuousAdaptiveHexNavigationGraph(
            snapshot,
            bounds.Shape.Position,
            fineSpacing,
            fineRadius,
            [.. gridPositions],
            [.. positions],
            nodeIndices);

        void AddNode(HexCubeGridPoint gridPosition)
        {
            if (nodeIndices.ContainsKey(gridPosition)) return;

            HexCubePoint position = bounds.Shape.Position + new HexCubePoint(
                gridPosition.Q * fineSpacing,
                gridPosition.R * fineSpacing);

            if (!bounds.Contains(position)) return;

            nodeIndices.Add(gridPosition, positions.Count);
            gridPositions.Add(gridPosition);
            positions.Add(position);
        }
    }

    /// <summary>
    /// 在当前局部细化导航图中搜索指定请求的连续路径.
    /// </summary>
    /// <param name="request">本次连续寻路请求.</param>
    /// <returns>找到时返回由精确可通行线段组成的路径; 未找到时返回空数组.</returns>
    internal HexCubePoint[] FindPath(ContinuousPathRequest request)
    {
        AdaptiveTraversalCache traversalCache = GetOrBuildTraversalCache(request);
        int[] startVisible = GetVisibleNodeIndices(request.Start, request);
        bool[] goalVisible = new bool[m_Positions.Length];

        foreach (int nodeIndex in GetVisibleNodeIndices(request.Goal, request))
        {
            goalVisible[nodeIndex] = true;
        }
        PriorityQueue<(int nodeIndex, double pathCost), double> open =
            Factory.RentPriorityQueue<(int nodeIndex, double pathCost), double>();
        Dictionary<int, int> cameFrom = Factory.RentDictionary<Dictionary<int, int>>();
        Dictionary<int, double> gScore = Factory.RentDictionary<Dictionary<int, double>>();

        try
        {
            gScore[StartNodeIndex] = 0;
            open.Enqueue((StartNodeIndex, 0), request.HeuristicWeight * request.Start.DistanceTo(request.Goal));

            while (open.TryDequeue(out (int nodeIndex, double pathCost) entry, out _))
            {
                if (!gScore.TryGetValue(entry.nodeIndex, out double currentCost) || currentCost != entry.pathCost) continue;

                if (entry.nodeIndex == GoalNodeIndex)
                {
                    return ReconstructPath(cameFrom, request);
                }

                if (entry.nodeIndex == StartNodeIndex)
                {
                    RelaxStartNeighbors(currentCost);
                    continue;
                }

                RelaxGridNeighbors(entry.nodeIndex, currentCost);

                if (goalVisible[entry.nodeIndex])
                {
                    Relax(
                        entry.nodeIndex,
                        GoalNodeIndex,
                        request.Goal,
                        currentCost,
                        m_Snapshot.GetLineCost(new HexCubeLine2D(m_Positions[entry.nodeIndex], request.Goal)));
                }
            }

            return [];

            void RelaxStartNeighbors(double currentCost)
            {
                HexCubeLine2D directLine = new HexCubeLine2D(request.Start, request.Goal);

                if (m_Snapshot.HasLineOfSight(directLine, request.AgentRadius, request.Clearance))
                {
                    Relax(StartNodeIndex, GoalNodeIndex, request.Goal, currentCost, m_Snapshot.GetLineCost(directLine));
                }

                foreach (int index in startVisible)
                {
                    Relax(
                        StartNodeIndex,
                        index,
                        m_Positions[index],
                        currentCost,
                        m_Snapshot.GetLineCost(new HexCubeLine2D(request.Start, m_Positions[index])));
                }
            }

            void RelaxGridNeighbors(int nodeIndex, double currentCost)
            {
                HexCubeGridPoint gridPosition = m_GridPositions[nodeIndex];
                HexCubePoint position = m_Positions[nodeIndex];

                for (int direction = 0; direction < 6; direction++)
                {
                    RelaxGridNeighbor(nodeIndex, gridPosition, position, direction, 1, currentCost);
                    RelaxGridNeighbor(nodeIndex, gridPosition, position, direction, 2, currentCost);
                }
            }

            void RelaxGridNeighbor(
                int currentNodeIndex,
                HexCubeGridPoint gridPosition,
                HexCubePoint position,
                int direction,
                int distance,
                double currentCost)
            {
                HexCubeGridPoint neighborPosition = gridPosition;

                for (int step = 0; step < distance; step++)
                {
                    neighborPosition = neighborPosition.NeighborAtUnchecked(direction);
                }

                if (!m_NodeIndices.TryGetValue(neighborPosition, out int neighborIndex)) return;

                HexCubePoint neighbor = m_Positions[neighborIndex];
                HexCubeLine2D line = new HexCubeLine2D(position, neighbor);

                if (!traversalCache.TryGetCost(currentNodeIndex, direction, distance, line, neighbor, out double edgeCost)) return;
                Relax(currentNodeIndex, neighborIndex, neighbor, currentCost, edgeCost);
            }

            void Relax(int currentNodeIndex, int neighborIndex, HexCubePoint neighbor, double currentCost, double edgeCost)
            {
                double tentativeCost = currentCost + edgeCost;

                if (gScore.TryGetValue(neighborIndex, out double oldCost) && !(tentativeCost < oldCost)) return;

                cameFrom[neighborIndex] = currentNodeIndex;
                gScore[neighborIndex] = tentativeCost;
                double priority = tentativeCost + request.HeuristicWeight * neighbor.DistanceTo(request.Goal);
                open.Enqueue((neighborIndex, tentativeCost), priority);
            }
        }
        finally
        {
            Factory.ReleasePriorityQueue(open);
            Factory.ReleaseDictionary(cameFrom);
            Factory.ReleaseDictionary(gScore);
        }
    }

    private int[] GetVisibleNodeIndices(HexCubePoint point, ContinuousPathRequest request)
    {
        HexCubePoint normalizedPoint = (point - m_Origin) / m_FineSpacing;
        HexCubeGridPoint nearestGridPosition = normalizedPoint.AsRound();
        List<int> visibleNodes = new List<int>(12);

        for (int radius = 0; radius <= m_FineRadius; radius++)
        {
            AddVisibleNodesInRing(radius);

            if (radius >= PreferredConnectionRadius && visibleNodes.Count != 0)
            {
                return [.. visibleNodes];
            }
        }

        return [.. visibleNodes];

        void AddVisibleNodesInRing(int radius)
        {
            for (int q = -radius; q <= radius; q++)
            {
                int minimumR = Math.Max(-radius, -q - radius);
                int maximumR = Math.Min(radius, -q + radius);

                for (int r = minimumR; r <= maximumR; r++)
                {
                    if (Math.Max(Math.Abs(q), Math.Max(Math.Abs(r), Math.Abs(q + r))) != radius) continue;

                    HexCubeGridPoint candidatePosition = new HexCubeGridPoint(
                        nearestGridPosition.Q + q,
                        nearestGridPosition.R + r);

                    if (!m_NodeIndices.TryGetValue(candidatePosition, out int nodeIndex)) continue;

                    HexCubePoint node = m_Positions[nodeIndex];

                    if (!m_Snapshot.IsPositionNavigable(node, request.AgentRadius, request.Clearance) ||
                        !m_Snapshot.HasLineOfSight(
                            new HexCubeLine2D(point, node),
                            request.AgentRadius,
                            request.Clearance)) continue;

                    visibleNodes.Add(nodeIndex);
                }
            }
        }
    }

    private AdaptiveTraversalCache GetOrBuildTraversalCache(ContinuousPathRequest request)
    {
        lock (m_TraversalCacheSync)
        {
            if (m_TraversalCache is { } traversalCache && traversalCache.Matches(request)) return traversalCache;

            m_TraversalCache = new AdaptiveTraversalCache(m_Snapshot, request, m_Positions.Length);
            return m_TraversalCache;
        }
    }

    private HexCubePoint[] ReconstructPath(Dictionary<int, int> cameFrom, ContinuousPathRequest request)
    {
        int pathLength = 1;
        int currentIndex = GoalNodeIndex;

        while (cameFrom.TryGetValue(currentIndex, out int parentIndex))
        {
            currentIndex = parentIndex;
            pathLength++;
        }

        HexCubePoint[] path = new HexCubePoint[pathLength];
        currentIndex = GoalNodeIndex;

        for (int index = pathLength - 1; index >= 0; index--)
        {
            path[index] = currentIndex switch
            {
                StartNodeIndex => request.Start,
                GoalNodeIndex => request.Goal,
                _ => m_Positions[currentIndex]
            };

            if (index > 0) currentIndex = cameFrom[currentIndex];
        }

        return path;
    }

    /// <summary>
    /// 缓存同一单位半径和安全距离下的有向局部边通行结果.
    /// 每个槽位只对应一个固定线段, 因此复用不会改变浮点计算顺序或路径选择结果.
    /// </summary>
    private sealed class AdaptiveTraversalCache
    {
        private const long UncomputedCostBits = long.MinValue;
        private const long BlockedCostBits = long.MaxValue;

        private readonly IContinuousNavigationSnapshot m_Snapshot;
        private readonly double m_AgentRadius;
        private readonly double m_Clearance;
        private readonly long[] m_CostBits;

        public AdaptiveTraversalCache(
            IContinuousNavigationSnapshot snapshot,
            ContinuousPathRequest request,
            int nodeCount)
        {
            m_Snapshot = snapshot;
            m_AgentRadius = request.AgentRadius;
            m_Clearance = request.Clearance;
            m_CostBits = new long[checked(nodeCount * 12)];
            Array.Fill(m_CostBits, UncomputedCostBits);
        }

        public bool Matches(ContinuousPathRequest request)
        {
            return m_AgentRadius == request.AgentRadius && m_Clearance == request.Clearance;
        }

        public bool TryGetCost(
            int nodeIndex,
            int direction,
            int distance,
            HexCubeLine2D line,
            HexCubePoint neighbor,
            out double cost)
        {
            int edgeIndex = nodeIndex * 12 + direction * 2 + distance - 1;
            long cachedBits = Volatile.Read(ref m_CostBits[edgeIndex]);

            if (cachedBits == BlockedCostBits)
            {
                cost = 0;
                return false;
            }

            if (cachedBits != UncomputedCostBits)
            {
                cost = BitConverter.Int64BitsToDouble(cachedBits);
                return true;
            }

            if (!m_Snapshot.IsPositionNavigable(neighbor, m_AgentRadius, m_Clearance) ||
                !m_Snapshot.HasLineOfSight(line, m_AgentRadius, m_Clearance))
            {
                Interlocked.CompareExchange(ref m_CostBits[edgeIndex], BlockedCostBits, UncomputedCostBits);
                cost = 0;
                return false;
            }

            cost = m_Snapshot.GetLineCost(line);
            Interlocked.CompareExchange(
                ref m_CostBits[edgeIndex],
                BitConverter.DoubleToInt64Bits(cost),
                UncomputedCostBits);
            return true;
        }
    }
}
