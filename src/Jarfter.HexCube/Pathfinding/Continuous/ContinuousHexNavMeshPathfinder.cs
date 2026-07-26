using Jarfter.Core.Collections.ObjectModel;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 使用规则六边形 NavMesh 与地图级局部细化图进行统一代价连续寻路.
/// 实现会比较两种图上的安全候选路径, 避免单位半径变化时因采样精度切换而出现不合理的路径质量倒退.
/// </summary>
public sealed class ContinuousHexNavMeshPathfinder : IContinuousPathfinder
{
    private const int MaxCachedNavMeshes = 4;

    private readonly object m_Sync = new object();
    private readonly ContinuousNavigationBounds m_Bounds;
    private readonly double m_CellSpacing;
    private readonly List<CachedNavMesh> m_NavMeshCache = [];
    private readonly List<CachedAdaptiveTopology> m_AdaptiveTopologyCache = [];

    /// <summary>
    /// 使用指定的有限边界和单元间距创建 NavMesh 寻路器.
    /// </summary>
    /// <param name="bounds">限制 NavMesh 构建范围的有限导航边界.</param>
    /// <param name="cellSpacing">相邻 NavMesh 单元中心的间距. 数值越小, 路径越贴近可通行空间, 但 NavMesh 的构建时间、缓存占用和单次搜索开销会增加. 必须为有限正数.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="cellSpacing"/> 不是有限正数时抛出.</exception>
    public ContinuousHexNavMeshPathfinder(ContinuousNavigationBounds bounds, double cellSpacing = 1)
    {
        ArgumentNullException.ThrowIfNull(bounds);

        if (!(cellSpacing > 0) || !double.IsFinite(cellSpacing))
        {
            throw new ArgumentOutOfRangeException(nameof(cellSpacing), cellSpacing, "Cell spacing must be a finite positive number.");
        }

        m_Bounds = bounds;
        m_CellSpacing = cellSpacing;
    }

    /// <summary>
    /// 获取当前寻路器的 NavMesh 单元间距.
    /// 数值越小, 路径质量越高, 但会增加 NavMesh 构建、缓存和搜索开销.
    /// </summary>
    public double CellSpacing => m_CellSpacing;

    /// <inheritdoc />
    public ContinuousPathResult FindPath(ContinuousPathRequest request, IContinuousNavigationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ValidateRequest(request);

        if (!m_Bounds.Contains(request.Start) || !m_Bounds.Contains(request.Goal) ||
            !snapshot.IsPositionNavigable(request.Start, request.AgentRadius, request.Clearance) ||
            !snapshot.IsPositionNavigable(request.Goal, request.AgentRadius, request.Clearance))
        {
            return ContinuousPathResult.Empty(request, snapshot.Revision);
        }

        HexCubeLine2D directLine = new HexCubeLine2D(request.Start, request.Goal);

        if (snapshot.UsesUniformTraversalCost && snapshot.HasLineOfSight(directLine, request.AgentRadius, request.Clearance))
        {
            return new ContinuousPathResult([request.Start, request.Goal], directLine.Length, snapshot.Revision, request.AgentRadius, request.Clearance);
        }

        CachedNavMesh cachedNavMesh = GetOrBuildNavMesh(snapshot, request.AgentRadius, request.Clearance);
        ContinuousHexNavMesh navMesh = cachedNavMesh.NavMesh;

        HexCubePoint[] navMeshPath = [];

        if (navMesh.TryGetContainingCellIndex(request.Start, out int startIndex) &&
            navMesh.TryGetContainingCellIndex(request.Goal, out int goalIndex))
        {
            navMeshPath = FindCellPath(navMesh, cachedNavMesh.TraversalCostCache, startIndex, goalIndex, request, snapshot);
        }

        HexCubePoint[] refinedPath = GetOrBuildAdaptiveTopology(snapshot).FindPath(request);
        refinedPath = refinedPath.Length == 0 ? [] : SmoothPath(refinedPath, snapshot, request);
        HexCubePoint[] path = SelectLowerCostPath(navMeshPath, refinedPath, snapshot);

        return path.Length == 0
            ? ContinuousPathResult.Empty(request, snapshot.Revision)
            : new ContinuousPathResult(path, CalculateCost(path, snapshot), snapshot.Revision, request.AgentRadius, request.Clearance);
    }

    private static HexCubePoint[] FindCellPath(
        ContinuousHexNavMesh navMesh,
        NavMeshTraversalCostCache traversalCostCache,
        int startIndex,
        int goalIndex,
        ContinuousPathRequest request,
        IContinuousNavigationSnapshot snapshot)
    {
        PriorityQueue<(int cellIndex, double pathCost), double> open =
            Factory.RentPriorityQueue<(int cellIndex, double pathCost), double>();
        Dictionary<int, int> cameFrom = Factory.RentDictionary<Dictionary<int, int>>();
        Dictionary<int, double> gScore = Factory.RentDictionary<Dictionary<int, double>>();

        try
        {
            HexCubePoint goalCenter = navMesh.GetCell(goalIndex).Position;
            bool usesUniformTraversalCost = snapshot.UsesUniformTraversalCost;
            gScore[startIndex] = 0;
            open.Enqueue((startIndex, 0), request.HeuristicWeight * navMesh.GetCell(startIndex).Position.DistanceTo(goalCenter));

            while (open.TryDequeue(out (int cellIndex, double pathCost) entry, out _))
            {
                if (!gScore.TryGetValue(entry.cellIndex, out double currentCost) || currentCost != entry.pathCost) continue;

                if (entry.cellIndex == goalIndex)
                {
                    int[] cellPath = ReconstructCellPath(cameFrom, startIndex, goalIndex);
                    HexCubePoint[] funnelPath = BuildFunnelPath(navMesh, cellPath, request.Start, request.Goal);
                    return SmoothPath(funnelPath, snapshot, request);
                }

                HexCubePoint currentCenter = navMesh.GetCell(entry.cellIndex).Position;

                for (int direction = 0; direction < 6; direction++)
                {
                    if (!navMesh.TryGetNeighborIndex(entry.cellIndex, direction, out int neighborIndex)) continue;

                    HexCubePoint neighborCenter = navMesh.GetCell(neighborIndex).Position;
                    double edgeCost = usesUniformTraversalCost
                        ? snapshot.GetLineCost(new HexCubeLine2D(currentCenter, neighborCenter))
                        : traversalCostCache.GetCost(entry.cellIndex, direction, currentCenter, neighborCenter);
                    double tentativeCost = currentCost + edgeCost;

                    if (gScore.TryGetValue(neighborIndex, out double oldCost) && !(tentativeCost < oldCost)) continue;

                    cameFrom[neighborIndex] = entry.cellIndex;
                    gScore[neighborIndex] = tentativeCost;
                    double priority = tentativeCost + request.HeuristicWeight * neighborCenter.DistanceTo(goalCenter);
                    open.Enqueue((neighborIndex, tentativeCost), priority);
                }
            }

            return [];
        }
        finally
        {
            Factory.ReleasePriorityQueue(open);
            Factory.ReleaseDictionary(cameFrom);
            Factory.ReleaseDictionary(gScore);
        }
    }

    private CachedNavMesh GetOrBuildNavMesh(
        IContinuousNavigationSnapshot snapshot,
        double agentRadius,
        double clearance)
    {
        ContinuousNavigationBounds bounds = m_Bounds;

        lock (m_Sync)
        {
            foreach (CachedNavMesh cachedNavMesh in m_NavMeshCache)
            {
                if (ReferenceEquals(cachedNavMesh.Snapshot, snapshot) &&
                    cachedNavMesh.AgentRadius == agentRadius && cachedNavMesh.Clearance == clearance)
                {
                    return cachedNavMesh;
                }
            }

            ContinuousHexNavMesh navMesh = ContinuousHexNavMesh.Build(
                snapshot,
                bounds,
                agentRadius,
                clearance,
                m_CellSpacing);

            if (m_NavMeshCache.Count == MaxCachedNavMeshes)
            {
                m_NavMeshCache.RemoveAt(0);
            }

            CachedNavMesh newCachedNavMesh = new CachedNavMesh(
                snapshot,
                agentRadius,
                clearance,
                navMesh,
                new NavMeshTraversalCostCache(snapshot, navMesh.Cells.Count));
            m_NavMeshCache.Add(newCachedNavMesh);
            return newCachedNavMesh;
        }
    }

    private ContinuousAdaptiveHexNavigationGraph GetOrBuildAdaptiveTopology(IContinuousNavigationSnapshot snapshot)
    {
        ContinuousNavigationBounds bounds = m_Bounds;

        lock (m_Sync)
        {
            foreach (CachedAdaptiveTopology cachedTopology in m_AdaptiveTopologyCache)
            {
                if (ReferenceEquals(cachedTopology.Snapshot, snapshot)) return cachedTopology.Graph;
            }

            if (m_AdaptiveTopologyCache.Count == MaxCachedNavMeshes)
            {
                m_AdaptiveTopologyCache.RemoveAt(0);
            }

            ContinuousAdaptiveHexNavigationGraph graph = ContinuousAdaptiveHexNavigationGraph.Build(
                snapshot,
                bounds,
                m_CellSpacing);
            m_AdaptiveTopologyCache.Add(new CachedAdaptiveTopology(snapshot, graph));
            return graph;
        }
    }

    private static HexCubePoint[] SelectLowerCostPath(
        HexCubePoint[] navMeshPath,
        HexCubePoint[] refinedPath,
        IContinuousNavigationSnapshot snapshot)
    {
        if (navMeshPath.Length == 0) return refinedPath;
        if (refinedPath.Length == 0) return navMeshPath;

        return CalculateCost(refinedPath, snapshot) < CalculateCost(navMeshPath, snapshot)
            ? refinedPath
            : navMeshPath;
    }

    private static int[] ReconstructCellPath(
        Dictionary<int, int> cameFrom,
        int startIndex,
        int goalIndex)
    {
        int cellCount = 1;
        int currentIndex = goalIndex;

        while (currentIndex != startIndex)
        {
            currentIndex = cameFrom[currentIndex];
            cellCount++;
        }

        int[] path = new int[cellCount];
        currentIndex = goalIndex;

        for (int index = cellCount - 1; index >= 0; index--)
        {
            path[index] = currentIndex;
            if (index > 0) currentIndex = cameFrom[currentIndex];
        }

        return path;
    }

    private static HexCubePoint[] BuildFunnelPath(
        ContinuousHexNavMesh navMesh,
        IReadOnlyList<int> cellPath,
        HexCubePoint start,
        HexCubePoint goal)
    {
        List<(HexCubePoint left, HexCubePoint right)> portals = new List<(HexCubePoint left, HexCubePoint right)>(cellPath.Count + 1)
        {
            (start, start)
        };

        for (int index = 1; index < cellPath.Count; index++)
        {
            int previousIndex = cellPath[index - 1];
            int currentIndex = cellPath[index];
            int direction = GetNeighborDirection(navMesh, previousIndex, currentIndex);
            HexCubeLine2D portal = navMesh.GetPortal(previousIndex, direction);
            HexCubePoint center = navMesh.GetCell(previousIndex).Position;
            HexCubePoint movement = navMesh.GetCell(currentIndex).Position - center;
            double firstSide = Cross(movement, portal.Start - center);
            portals.Add(firstSide >= 0 ? (portal.Start, portal.End) : (portal.End, portal.Start));
        }

        portals.Add((goal, goal));
        List<HexCubePoint> result = [start];
        HexCubePoint apex = start;
        HexCubePoint left = start;
        HexCubePoint right = start;
        int apexIndex;
        int leftIndex = 0;
        int rightIndex = 0;

        for (int portalIndex = 1; portalIndex < portals.Count; portalIndex++)
        {
            (HexCubePoint nextLeft, HexCubePoint nextRight) = portals[portalIndex];

            if (Cross(right - apex, nextRight - apex) <= 0)
            {
                if (apex == right || Cross(left - apex, nextRight - apex) > 0)
                {
                    right = nextRight;
                    rightIndex = portalIndex;
                }
                else
                {
                    result.Add(left);
                    apex = left;
                    apexIndex = leftIndex;
                    left = apex;
                    right = apex;
                    leftIndex = apexIndex;
                    rightIndex = apexIndex;
                    portalIndex = apexIndex;
                    continue;
                }
            }

            if (Cross(left - apex, nextLeft - apex) >= 0)
            {
                if (apex == left || Cross(right - apex, nextLeft - apex) < 0)
                {
                    left = nextLeft;
                    leftIndex = portalIndex;
                }
                else
                {
                    result.Add(right);
                    apex = right;
                    apexIndex = rightIndex;
                    left = apex;
                    right = apex;
                    leftIndex = apexIndex;
                    rightIndex = apexIndex;
                    portalIndex = apexIndex;
                }
            }
        }

        result.Add(goal);
        return [.. result];
    }

    private static int GetNeighborDirection(ContinuousHexNavMesh navMesh, int cellIndex, int neighborIndex)
    {
        for (int direction = 0; direction < 6; direction++)
        {
            if (navMesh.TryGetNeighborIndex(cellIndex, direction, out int candidateIndex) && candidateIndex == neighborIndex) return direction;
        }

        throw new InvalidOperationException("Cell path contains non-adjacent NavMesh cells.");
    }

    private static double Cross(HexCubePoint left, HexCubePoint right) => left.Q * right.R - left.R * right.Q;

    private static HexCubePoint[] SmoothPath(
        HexCubePoint[] path,
        IContinuousNavigationSnapshot snapshot,
        ContinuousPathRequest request)
    {
        double[] segmentCosts = new double[path.Length - 1];

        for (int index = 0; index < segmentCosts.Length; index++)
        {
            segmentCosts[index] = snapshot.GetLineCost(new HexCubeLine2D(path[index], path[index + 1]));
        }

        List<HexCubePoint> result = new List<HexCubePoint>(path.Length) { path[0] };
        int currentIndex = 0;

        while (currentIndex < path.Length - 1)
        {
            double replacedCost = 0;
            int nextIndex = path.Length - 1;

            while (nextIndex > currentIndex + 1)
            {
                for (int index = currentIndex; index < nextIndex; index++)
                {
                    replacedCost += segmentCosts[index];
                }

                HexCubeLine2D shortcut = new HexCubeLine2D(path[currentIndex], path[nextIndex]);

                // 高代价地图中仅接受不增加精确总代价的捷径, 防止 Funnel 的几何捷径穿越昂贵区域.
                if (snapshot.HasLineOfSight(shortcut, request.AgentRadius, request.Clearance) &&
                    snapshot.GetLineCost(shortcut) <= replacedCost + 1e-12)
                {
                    break;
                }

                replacedCost = 0;
                nextIndex--;
            }

            result.Add(path[nextIndex]);
            currentIndex = nextIndex;
        }

        return [.. result];
    }

    private static double CalculateCost(IReadOnlyList<HexCubePoint> path, IContinuousNavigationSnapshot snapshot)
    {
        double cost = 0;

        for (int index = 1; index < path.Count; index++)
        {
            cost += snapshot.GetLineCost(new HexCubeLine2D(path[index - 1], path[index]));
        }

        return cost;
    }

    private static void ValidateRequest(ContinuousPathRequest request)
    {
        if (!double.IsFinite(request.Start.Q) || !double.IsFinite(request.Start.R) ||
            !double.IsFinite(request.Goal.Q) || !double.IsFinite(request.Goal.R))
        {
            throw new ArgumentOutOfRangeException(nameof(request), request, "Start and goal coordinates must be finite.");
        }

        if (!(request.AgentRadius >= 0) || !double.IsFinite(request.AgentRadius))
        {
            throw new ArgumentOutOfRangeException(nameof(request), request, "Agent radius must be a finite non-negative number.");
        }

        if (!(request.Clearance >= 0) || !double.IsFinite(request.Clearance))
        {
            throw new ArgumentOutOfRangeException(nameof(request), request, "Clearance must be a finite non-negative number.");
        }

        _ = HeuristicWeight.Validate(request.HeuristicWeight, nameof(request.HeuristicWeight));
    }

    private readonly record struct CachedNavMesh(
        IContinuousNavigationSnapshot Snapshot,
        double AgentRadius,
        double Clearance,
        ContinuousHexNavMesh NavMesh,
        NavMeshTraversalCostCache TraversalCostCache);

    private readonly record struct CachedAdaptiveTopology(
        IContinuousNavigationSnapshot Snapshot,
        ContinuousAdaptiveHexNavigationGraph Graph);
}
