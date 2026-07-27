using Jarfter.Core.Collections.ObjectModel;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Query;
using Jarfter.NavMesh.Topology;
using System.Runtime.CompilerServices;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.Tiles;

/// <summary>
/// 由一组不可变 tile 导航网格组成的只读快照.
/// 快照仅保存 tile 引用和排序索引, 不会将各 tile 的 polygon 重新合成为单体导航网格.
/// 因此未变更 tile 的网格与内存可在相邻快照之间复用.
/// </summary>
public sealed class TiledNavMeshSnapshot
{
    private const int MaximumHeuristicJumpCount = 64;

    private readonly TiledNavMeshTile[] m_Tiles;
    private readonly int[] m_PolygonOffsets;
    private readonly TiledTileBvhNode[] m_TileBvhNodes;
    private readonly int[] m_TileBvhTiles;
    private readonly TiledCrossTileJumpGraph m_CrossTileJumpGraph;
    private readonly bool m_HasJumpConnections;
    private readonly TiledHeuristicJump[] m_HeuristicJumps;
    private readonly double[] m_HeuristicJumpTransitionDistances;
    private TiledNavMeshPortal[]? m_Portals;
    private TiledPortalGraph? m_PortalGraph;

    internal TiledNavMeshSnapshot(IEnumerable<TiledNavMeshTile> tiles, ReadOnlySpan<TiledNavMeshPortal> portals,
        ReadOnlySpan<NavMeshJumpConnection> crossTileJumpConnections)
    {
        m_Tiles = tiles.ToArray();
        Array.Sort(m_Tiles, static (left, right) => CompareTileIds(left.TileId, right.TileId));
        m_Portals = portals.ToArray();
        m_HasJumpConnections = !crossTileJumpConnections.IsEmpty;
        m_PolygonOffsets = new int[m_Tiles.Length + 1];
        for (int index = 0; index < m_Tiles.Length; index++)
        {
            m_PolygonOffsets[index + 1] = m_PolygonOffsets[index] + m_Tiles[index].NavMesh.PolygonCount;
            m_HasJumpConnections |= m_Tiles[index].NavMesh.JumpConnectionCount != 0;
        }

        m_TileBvhTiles = new int[m_Tiles.Length];
        for (int index = 0; index < m_TileBvhTiles.Length; index++) m_TileBvhTiles[index] = index;
        List<TiledTileBvhNode> tileBvhNodes = new List<TiledTileBvhNode>();
        BuildTileBvh(0, m_TileBvhTiles.Length, tileBvhNodes);
        m_TileBvhNodes = tileBvhNodes.ToArray();
        m_CrossTileJumpGraph = BuildCrossTileJumpGraph(crossTileJumpConnections);
        m_HeuristicJumps = BuildHeuristicJumps();
        m_HeuristicJumpTransitionDistances = BuildHeuristicJumpTransitionDistances(m_HeuristicJumps);
    }

    /// <summary>
    /// 获取快照中包含的 tile 数量.
    /// </summary>
    public int TileCount => m_Tiles.Length;

    /// <summary>
    /// 获取由完全相同的边界线段连接的跨 tile 门户数量.
    /// </summary>
    public int PortalCount => GetPortals().Length;

    /// <summary>
    /// 获取组合快照中的逻辑 polygon 总数.
    /// </summary>
    public int PolygonCount => m_PolygonOffsets[^1];

    /// <summary>
    /// 将当前快照中的 tile 标识复制到调用方提供的缓冲区.
    /// 标识按 Layer、Y、X 的稳定顺序排列.
    /// </summary>
    /// <param name="destination">接收 tile 标识的缓冲区.</param>
    /// <returns>实际写入的 tile 标识数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyTileIds(Span<NavMeshTileId> destination)
    {
        int written = Math.Min(destination.Length, m_Tiles.Length);
        for (int index = 0; index < written; index++) destination[index] = m_Tiles[index].TileId;
        return written;
    }

    /// <summary>
    /// 将跨 tile 门户复制到调用方提供的缓冲区.
    /// 门户中的 polygon 索引分别相对于对应的 tile 导航网格.
    /// </summary>
    /// <param name="destination">接收门户定义的缓冲区.</param>
    /// <returns>实际写入的门户数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyPortals(Span<TiledNavMeshPortal> destination)
    {
        TiledNavMeshPortal[] portals = GetPortals();
        int written = Math.Min(destination.Length, portals.Length);
        portals.AsSpan(0, written).CopyTo(destination);
        return written;
    }

    /// <summary>
    /// 尝试获取指定 tile 的不可变导航网格.
    /// 返回的网格使用全局坐标, 可独立用于 tile 内部查询.
    /// </summary>
    /// <param name="tileId">待查询的 tile 标识.</param>
    /// <param name="navMesh">成功时为该 tile 的不可变导航网格.</param>
    /// <returns>当前快照包含指定 tile 时为 <see langword="true"/>.</returns>
    public bool TryGetTile(NavMeshTileId tileId, out Mesh? navMesh)
    {
        int index = FindTileIndex(tileId);
        if (index < 0)
        {
            navMesh = null;
            return false;
        }

        navMesh = m_Tiles[index].NavMesh;
        return true;
    }

    /// <summary>
    /// 尝试定位位于任意 tile 网格内的点.
    /// 位于多个共享边 tile 的点会按快照的稳定 tile 顺序返回其中一个位置.
    /// </summary>
    /// <param name="point">待定位的有限二维坐标.</param>
    /// <param name="location">成功时包含所属 tile 标识和 tile 内位置.</param>
    /// <returns>点位于任意 tile 的可行走 polygon 内时为 <see langword="true"/>.</returns>
    public bool TryFindLocation(NavMeshPoint point, out TiledNavMeshLocation location)
    {
        return TryFindLocation(point, NavMeshQueryDefaults.Filter, out location);
    }

    /// <summary>
    /// 尝试定位位于满足过滤器的任意 tile 网格内的点.
    /// 位于多个共享边 tile 的点会按快照的稳定 tile 顺序返回其中一个位置.
    /// </summary>
    /// <param name="point">待定位的有限二维坐标.</param>
    /// <param name="filter">决定 polygon 是否允许参与定位的过滤器.</param>
    /// <param name="location">成功时包含所属 tile 标识和 tile 内位置.</param>
    /// <returns>点位于任意满足过滤器的 tile polygon 内时为 <see langword="true"/>.</returns>
    public bool TryFindLocation(NavMeshPoint point, INavMeshQueryFilter filter, out TiledNavMeshLocation location)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!point.IsFinite) throw new ArgumentException("查询点必须为有限 double 坐标.", nameof(point));
        int selectedTileIndex = -1;
        NavMeshLocation selectedLocation = default;
        FindLocationInTileBvh(0, point, filter, ref selectedTileIndex, ref selectedLocation);
        if (selectedTileIndex < 0)
        {
            location = default;
            return false;
        }

        location = new TiledNavMeshLocation(m_Tiles[selectedTileIndex].TileId, selectedLocation);
        return true;
    }

    /// <summary>
    /// 尝试将点投影到组合快照中最近的可行走位置.
    /// </summary>
    /// <param name="point">待投影的有限二维坐标.</param>
    /// <param name="location">成功时为最近点及其所属 tile polygon.</param>
    /// <returns>快照包含至少一个可投影 polygon 时为 <see langword="true"/>.</returns>
    public bool TryFindNearestLocation(NavMeshPoint point, out TiledNavMeshLocation location)
    {
        return TryFindNearestLocation(point, NavMeshQueryDefaults.Filter, out location);
    }

    /// <summary>
    /// 尝试将点投影到满足过滤器的组合快照最近可行走位置.
    /// 距离相等时按稳定 tile 顺序选择结果.
    /// </summary>
    /// <param name="point">待投影的有限二维坐标.</param>
    /// <param name="filter">决定 polygon 是否允许参与投影的过滤器.</param>
    /// <param name="location">成功时为最近点及其所属 tile polygon.</param>
    /// <returns>快照包含至少一个满足过滤器的 polygon 时为 <see langword="true"/>.</returns>
    public bool TryFindNearestLocation(NavMeshPoint point, INavMeshQueryFilter filter, out TiledNavMeshLocation location)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!point.IsFinite) throw new ArgumentException("查询点必须为有限 double 坐标.", nameof(point));
        int selectedTileIndex = -1;
        NavMeshLocation selectedLocation = default;
        double bestDistanceSquared = double.PositiveInfinity;
        for (int tileIndex = 0; tileIndex < m_Tiles.Length; tileIndex++)
        {
            Mesh tile = m_Tiles[tileIndex].NavMesh;
            if (!tile.TryFindNearestLocation(point, filter, out NavMeshLocation candidate)) continue;
            double offsetX = candidate.Position.X - point.X;
            double offsetY = candidate.Position.Y - point.Y;
            double distanceSquared = offsetX * offsetX + offsetY * offsetY;
            if (distanceSquared >= bestDistanceSquared) continue;
            bestDistanceSquared = distanceSquared;
            selectedTileIndex = tileIndex;
            selectedLocation = candidate;
        }

        if (selectedTileIndex < 0)
        {
            location = default;
            return false;
        }

        location = new TiledNavMeshLocation(m_Tiles[selectedTileIndex].TileId, selectedLocation);
        return true;
    }

    /// <summary>
    /// 在组合 tile polygon 图中查找最小累计区域移动代价 corridor.
    /// 查询直接使用 tile 内邻接、跳跃边和跨 tile 门户, 不会物化单体 <see cref="TiledNavMesh.Snapshot"/>.
    /// </summary>
    /// <param name="start">位于任意 tile 网格内的起点.</param>
    /// <param name="goal">位于任意 tile 网格内的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定 polygon 是否允许穿越的过滤器.</param>
    /// <param name="costPolicy">决定跨 polygon 移动代价的策略.</param>
    /// <param name="destination">接收按行进顺序排列的跨 tile polygon corridor.</param>
    /// <param name="corridorCount">成功时为写入数量; 缓冲区不足时为所需数量; 无路径时为 0.</param>
    /// <param name="searchCost">成功时为 polygon 图上的累计移动代价. 启发式权重为 1 时该值最优.</param>
    /// <param name="options">可选的性能与最优性控制参数. 查询开始时会捕获其当前值.</param>
    /// <returns>找到完整 corridor 且 destination 容量充足时为 <see langword="true"/>.</returns>
    public bool TryFindCorridor(NavMeshPoint start, NavMeshPoint goal, TiledNavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, Span<TiledNavMeshPolygon> destination,
        out int corridorCount, out double searchCost, NavMeshQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        double heuristicWeight = options?.HeuristicWeight ?? 1d;
        if (!TryFindLocation(start, filter, out TiledNavMeshLocation startLocation) ||
            !TryFindLocation(goal, filter, out TiledNavMeshLocation goalLocation))
        {
            corridorCount = 0;
            searchCost = default;
            return false;
        }

        int startTileIndex = FindTileIndex(startLocation.TileId);
        int goalTileIndex = FindTileIndex(goalLocation.TileId);
        int startNode = GetNode(startTileIndex, startLocation.Location.PolygonRef.Index);
        int goalNode = GetNode(goalTileIndex, goalLocation.Location.PolygonRef.Index);
        return TryFindCorridor(startNode, goalNode, workspace, filter, costPolicy, heuristicWeight, destination,
            out corridorCount, out searchCost);
    }

    /// <summary>
    /// 在组合 tile 导航网格中查找并平滑从起点到终点的二维路径.
    /// 查询不会物化单体 <see cref="TiledNavMesh.Snapshot"/>, 且可正确穿越跨 tile 门户与 tile 内跳跃连接.
    /// </summary>
    /// <param name="start">位于任意 tile 网格内的起点.</param>
    /// <param name="goal">位于任意 tile 网格内的终点.</param>
    /// <returns>不可达或任一端点不在网格内时返回 <see langword="null"/>.</returns>
    public TiledNavMeshPath? FindPath(NavMeshPoint start, NavMeshPoint goal)
    {
        return FindPath(start, goal, new TiledNavMeshQueryWorkspace(), NavMeshQueryDefaults.Filter,
            NavMeshQueryDefaults.CostPolicy);
    }

    /// <summary>
    /// 使用已缓存的 tile 位置查找二维路径.
    /// 当位置属于其他快照或其 polygon 已被 tile 更新替换时, 安全返回 <see langword="null"/>.
    /// </summary>
    /// <param name="start">包含当前快照有效 polygon 引用的起点.</param>
    /// <param name="goal">包含当前快照有效 polygon 引用的终点.</param>
    /// <returns>引用无效、端点不可达或端点不满足默认过滤器时返回 <see langword="null"/>.</returns>
    public TiledNavMeshPath? FindPath(TiledNavMeshLocation start, TiledNavMeshLocation goal)
    {
        return FindPath(start, goal, new TiledNavMeshQueryWorkspace(), NavMeshQueryDefaults.Filter,
            NavMeshQueryDefaults.CostPolicy);
    }

    /// <summary>
    /// 使用调用方提供的工作区、过滤器与移动代价策略查找并平滑二维路径.
    /// </summary>
    /// <param name="start">位于任意 tile 网格内的起点.</param>
    /// <param name="goal">位于任意 tile 网格内的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定 polygon 是否允许穿越的过滤器.</param>
    /// <param name="costPolicy">决定跨 polygon 移动代价的策略.</param>
    /// <param name="options">可选的性能与最优性控制参数. 查询开始时会捕获其当前值.</param>
    /// <returns>不可达或任一端点不在网格内时返回 <see langword="null"/>.</returns>
    public TiledNavMeshPath? FindPath(NavMeshPoint start, NavMeshPoint goal, TiledNavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, NavMeshQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        double heuristicWeight = options?.HeuristicWeight ?? 1d;
        if (!TryFindLocation(start, filter, out TiledNavMeshLocation startLocation) ||
            !TryFindLocation(goal, filter, out TiledNavMeshLocation goalLocation))
            return null;

        int startNode = GetNode(FindTileIndex(startLocation.TileId), startLocation.Location.PolygonRef.Index);
        int goalNode = GetNode(FindTileIndex(goalLocation.TileId), goalLocation.Location.PolygonRef.Index);
        if (!TrySearch(startNode, goalNode, workspace, filter, costPolicy, heuristicWeight, out double searchCost))
            return null;

        List<int> nodes = Factory.RentList<int>();
        List<TiledNavMeshTransition> transitions = Factory.RentList<TiledNavMeshTransition>();
        try
        {
            for (int node = goalNode; node >= 0; node = workspace.Parents[node]) nodes.Add(node);
            nodes.Reverse();
            transitions.Add(default);
            for (int index = 1; index < nodes.Count; index++)
                transitions.Add(workspace.ParentTransitions[nodes[index]]);

            TiledNavMeshPolygon[] corridor = new TiledNavMeshPolygon[nodes.Count];
            for (int index = 0; index < corridor.Length; index++) corridor[index] = GetPolygon(nodes[index]);
            NavMeshPoint[] points = BuildStraightPath(start, goal, nodes, corridor, transitions, costPolicy,
                out TiledNavMeshJumpTraversal[] jumps, out double totalCost);
            return new TiledNavMeshPath(points, corridor, jumps, searchCost, totalCost, heuristicWeight);
        }
        finally
        {
            Factory.Release(transitions);
            Factory.Release(nodes);
        }
    }

    /// <summary>
    /// 使用调用方提供的工作区、过滤器和已缓存 tile 位置查找并平滑二维路径.
    /// 该重载跳过坐标定位, 适合连续重规划.
    /// </summary>
    /// <param name="start">包含当前快照有效 polygon 引用的起点.</param>
    /// <param name="goal">包含当前快照有效 polygon 引用的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定 polygon 是否允许穿越的过滤器.</param>
    /// <param name="costPolicy">决定跨 polygon 移动代价的策略.</param>
    /// <param name="options">可选的性能与最优性控制参数. 查询开始时会捕获其当前值.</param>
    /// <returns>引用无效、端点不可达或端点不满足过滤器时返回 <see langword="null"/>.</returns>
    public TiledNavMeshPath? FindPath(TiledNavMeshLocation start, TiledNavMeshLocation goal,
        TiledNavMeshQueryWorkspace workspace, INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy,
        NavMeshQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        if (!TryResolveLocation(start, filter, out int startNode) || !TryResolveLocation(goal, filter, out int goalNode))
            return null;
        double heuristicWeight = options?.HeuristicWeight ?? 1d;
        if (!TrySearch(startNode, goalNode, workspace, filter, costPolicy, heuristicWeight, out double searchCost))
            return null;

        List<int> nodes = Factory.RentList<int>();
        List<TiledNavMeshTransition> transitions = Factory.RentList<TiledNavMeshTransition>();
        try
        {
            for (int node = goalNode; node >= 0; node = workspace.Parents[node]) nodes.Add(node);
            nodes.Reverse();
            transitions.Add(default);
            for (int index = 1; index < nodes.Count; index++)
                transitions.Add(workspace.ParentTransitions[nodes[index]]);
            TiledNavMeshPolygon[] corridor = new TiledNavMeshPolygon[nodes.Count];
            for (int index = 0; index < corridor.Length; index++) corridor[index] = GetPolygon(nodes[index]);
            NavMeshPoint[] points = BuildStraightPath(start.Location.Position, goal.Location.Position, nodes, corridor,
                transitions, costPolicy, out TiledNavMeshJumpTraversal[] jumps, out double totalCost);
            return new TiledNavMeshPath(points, corridor, jumps, searchCost, totalCost, heuristicWeight);
        }
        finally
        {
            Factory.Release(transitions);
            Factory.Release(nodes);
        }
    }

    /// <summary>
    /// 获取仅供同一程序集构建跨 tile 查询索引使用的 tile 只读跨度.
    /// </summary>
    internal ReadOnlySpan<TiledNavMeshTile> TileSpan => m_Tiles;

    private void FindLocationInTileBvh(int nodeIndex, NavMeshPoint point, INavMeshQueryFilter filter,
        ref int selectedTileIndex, ref NavMeshLocation selectedLocation)
    {
        TiledTileBvhNode node = m_TileBvhNodes[nodeIndex];
        if (!node.Contains(point)) return;
        if (node.IsLeaf)
        {
            for (int index = node.Start; index < node.Start + node.Count; index++)
            {
                int tileIndex = m_TileBvhTiles[index];
                if (tileIndex >= selectedTileIndex && selectedTileIndex >= 0) continue;
                TiledNavMeshTile tile = m_Tiles[tileIndex];
                if (!tile.Contains(point) || !tile.NavMesh.TryFindLocation(point, filter, out NavMeshLocation tileLocation))
                    continue;
                selectedTileIndex = tileIndex;
                selectedLocation = tileLocation;
            }

            return;
        }

        FindLocationInTileBvh(node.Left, point, filter, ref selectedTileIndex, ref selectedLocation);
        FindLocationInTileBvh(node.Right, point, filter, ref selectedTileIndex, ref selectedLocation);
    }

    private bool TryResolveLocation(TiledNavMeshLocation location, INavMeshQueryFilter filter, out int node)
    {
        node = default;
        if (!location.Location.Position.IsFinite) return false;
        int tileIndex = FindTileIndex(location.TileId);
        if (tileIndex < 0) return false;
        Mesh tile = m_Tiles[tileIndex].NavMesh;
        NavMeshPolygonRef polygonRef = location.Location.PolygonRef;
        if (!tile.IsValidPolygonRef(polygonRef)) return false;
        int polygonIndex = polygonRef.Index;
        if (!filter.Pass(polygonIndex, tile.GetPolygonAreaId(polygonIndex), tile.GetPolygonFlags(polygonIndex)))
            return false;
        node = GetNode(tileIndex, polygonIndex);
        return true;
    }

    private void BuildTileBvh(int start, int count, List<TiledTileBvhNode> destination)
    {
        TiledTileBounds bounds = GetTileBvhBounds(start, count);
        int nodeIndex = destination.Count;
        destination.Add(default);
        if (count <= 4)
        {
            destination[nodeIndex] = new TiledTileBvhNode(bounds, start, count, -1, -1);
            return;
        }

        bool splitX = bounds.Width >= bounds.Height;
        Array.Sort(m_TileBvhTiles, start, count, new TileCenterComparer(m_Tiles, splitX));
        int leftCount = count / 2;
        int left = destination.Count;
        BuildTileBvh(start, leftCount, destination);
        int right = destination.Count;
        BuildTileBvh(start + leftCount, count - leftCount, destination);
        destination[nodeIndex] = new TiledTileBvhNode(bounds, 0, 0, left, right);
    }

    private TiledTileBounds GetTileBvhBounds(int start, int count)
    {
        TiledTileBounds bounds = m_Tiles[m_TileBvhTiles[start]].Bounds;
        for (int index = start + 1; index < start + count; index++)
            bounds = TiledTileBounds.Union(bounds, m_Tiles[m_TileBvhTiles[index]].Bounds);
        return bounds;
    }

    private int FindTileIndex(NavMeshTileId tileId)
    {
        int low = 0;
        int high = m_Tiles.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = CompareTileIds(m_Tiles[middle].TileId, tileId);
            if (comparison == 0) return middle;
            if (comparison < 0)
                low = middle + 1;
            else
                high = middle - 1;
        }

        return -1;
    }

    private bool TryFindCorridor(int startNode, int goalNode, TiledNavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, double heuristicWeight,
        Span<TiledNavMeshPolygon> destination,
        out int corridorCount, out double searchCost)
    {
        if (!TrySearch(startNode, goalNode, workspace, filter, costPolicy, heuristicWeight, out searchCost))
        {
            corridorCount = 0;
            return false;
        }

        corridorCount = GetCorridorCount(goalNode, workspace);
        if (corridorCount > destination.Length) return false;
        for (int node = goalNode, writeIndex = corridorCount;
             node >= 0;
             node = workspace.Parents[node])
            destination[--writeIndex] = GetPolygon(node);
        return true;
    }

    private bool TrySearch(int startNode, int goalNode, TiledNavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, double heuristicWeight,
        out double searchCost)
    {
        workspace.Reset(PolygonCount);
        workspace.SetOpen(startNode, 0, -1);
        NavMeshPoint goalCenter = GetPolygonCenter(goalNode);
        double minimumMultiplier = costPolicy.MinimumMultiplier;
        int heuristicJumpCount = PrepareJumpHeuristic(goalCenter, minimumMultiplier, workspace);
        workspace.Open.Enqueue(startNode, GetEstimatedRemainingCost(startNode, goalCenter, minimumMultiplier,
            heuristicWeight, heuristicJumpCount, workspace));
        TiledPortalGraph portalGraph = GetPortalGraph();
        while (workspace.Open.TryDequeue(out int currentNode, out _))
        {
            if (!workspace.TryClose(currentNode)) continue;
            if (currentNode == goalNode)
            {
                searchCost = workspace.GetCost(currentNode);
                return true;
            }

            GetTileAndPolygon(currentNode, out int tileIndex, out int polygonIndex);
            Mesh currentTile = m_Tiles[tileIndex].NavMesh;
            int currentAreaId = currentTile.GetPolygonAreaId(polygonIndex);
            double currentCost = workspace.GetCost(currentNode);
            int neighborCount = currentTile.GetPolygonNeighborCount(polygonIndex);
            for (int neighborIndex = 0; neighborIndex < neighborCount; neighborIndex++)
            {
                int targetPolygon = currentTile.GetPolygonNeighborIndex(polygonIndex, neighborIndex);
                NavMeshPortal portal = currentTile.GetDirectedPolygonPortal(polygonIndex, targetPolygon);
                OpenNeighbor(GetNode(tileIndex, targetPolygon), currentNode, currentCost, goalCenter, minimumMultiplier,
                    heuristicWeight, heuristicJumpCount,
                    currentTile.GetPolygonNeighborCenterDistance(polygonIndex, neighborIndex), currentAreaId, workspace,
                    filter, costPolicy, new TiledNavMeshTransition(TiledNavMeshTransitionKind.Portal, portal.Left,
                        portal.Right, default, default, default));
            }

            for (int linkIndex = portalGraph.Offsets[currentNode]; linkIndex < portalGraph.Offsets[currentNode + 1];
                 linkIndex++)
                OpenNeighbor(portalGraph.Links[linkIndex].TargetNode, currentNode, currentCost, goalCenter,
                    minimumMultiplier, heuristicWeight, heuristicJumpCount,
                    portalGraph.Links[linkIndex].CenterDistance, currentAreaId, workspace, filter, costPolicy,
                    portalGraph.Links[linkIndex].Transition);

            int jumpCount = currentTile.GetPolygonJumpCount(polygonIndex);
            for (int jumpIndex = 0; jumpIndex < jumpCount; jumpIndex++)
            {
                int targetPolygon = currentTile.GetPolygonJumpTarget(polygonIndex, jumpIndex);
                int targetNode = GetNode(tileIndex, targetPolygon);
                GetTileAndPolygon(targetNode, out int targetTileIndex, out int targetPolygonIndex);
                Mesh targetTile = m_Tiles[targetTileIndex].NavMesh;
                if (!filter.Pass(targetPolygonIndex, targetTile.GetPolygonAreaId(targetPolygonIndex),
                        targetTile.GetPolygonFlags(targetPolygonIndex)))
                    continue;
                double leaveMultiplier = GetTraversalMultiplier(currentAreaId, currentAreaId, costPolicy);
                int targetAreaId = targetTile.GetPolygonAreaId(targetPolygonIndex);
                double enterMultiplier = GetTraversalMultiplier(targetAreaId, targetAreaId, costPolicy);
                double jumpCost = NavMeshPoint.Distance(currentTile.GetPolygonCenter(polygonIndex),
                                      currentTile.GetPolygonJumpStart(polygonIndex, jumpIndex)) * leaveMultiplier +
                                  currentTile.GetPolygonJumpFixedCost(polygonIndex, jumpIndex) +
                                  NavMeshPoint.Distance(currentTile.GetPolygonJumpEnd(polygonIndex, jumpIndex),
                                      targetTile.GetPolygonCenter(targetPolygonIndex)) * enterMultiplier;
                double candidate = currentCost + jumpCost;
                if (candidate >= workspace.GetCost(targetNode)) continue;
                workspace.SetOpen(targetNode, candidate, currentNode,
                    new TiledNavMeshTransition(TiledNavMeshTransitionKind.Jump, default, default,
                        currentTile.GetPolygonJumpStart(polygonIndex, jumpIndex),
                        currentTile.GetPolygonJumpEnd(polygonIndex, jumpIndex),
                        currentTile.GetPolygonJumpFixedCost(polygonIndex, jumpIndex)));
                workspace.Open.Enqueue(targetNode, candidate + GetEstimatedRemainingCost(targetNode, goalCenter,
                    minimumMultiplier, heuristicWeight, heuristicJumpCount, workspace));
            }

            for (int jumpIndex = m_CrossTileJumpGraph.Offsets[currentNode];
                 jumpIndex < m_CrossTileJumpGraph.Offsets[currentNode + 1];
                 jumpIndex++)
            {
                TiledCrossTileJump jump = m_CrossTileJumpGraph.Jumps[jumpIndex];
                OpenCrossTileJump(jump, currentNode, currentCost, currentAreaId, goalCenter, minimumMultiplier,
                    heuristicWeight, heuristicJumpCount, workspace, filter, costPolicy);
            }
        }

        searchCost = default;
        return false;
    }

    private void OpenNeighbor(int targetNode, int currentNode, double currentCost, NavMeshPoint goalCenter,
        double minimumMultiplier, double heuristicWeight, int heuristicJumpCount, double centerDistance, int currentAreaId,
        TiledNavMeshQueryWorkspace workspace, INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy,
        TiledNavMeshTransition transition)
    {
        GetTileAndPolygon(targetNode, out int targetTileIndex, out int targetPolygonIndex);
        Mesh targetTile = m_Tiles[targetTileIndex].NavMesh;
        int targetAreaId = targetTile.GetPolygonAreaId(targetPolygonIndex);
        if (!filter.Pass(targetPolygonIndex, targetAreaId, targetTile.GetPolygonFlags(targetPolygonIndex))) return;
        double candidate = currentCost + centerDistance *
            GetTraversalMultiplier(currentAreaId, targetAreaId, costPolicy);
        if (candidate >= workspace.GetCost(targetNode)) return;
        workspace.SetOpen(targetNode, candidate, currentNode, transition);
        workspace.Open.Enqueue(targetNode, candidate + GetEstimatedRemainingCost(targetNode, goalCenter,
            minimumMultiplier, heuristicWeight, heuristicJumpCount, workspace));
    }

    private void OpenCrossTileJump(TiledCrossTileJump jump, int currentNode, double currentCost, int currentAreaId,
        NavMeshPoint goalCenter, double minimumMultiplier, double heuristicWeight, int heuristicJumpCount,
        TiledNavMeshQueryWorkspace workspace, INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy)
    {
        GetTileAndPolygon(jump.TargetNode, out int targetTileIndex, out int targetPolygonIndex);
        Mesh targetTile = m_Tiles[targetTileIndex].NavMesh;
        int targetAreaId = targetTile.GetPolygonAreaId(targetPolygonIndex);
        if (!filter.Pass(targetPolygonIndex, targetAreaId, targetTile.GetPolygonFlags(targetPolygonIndex))) return;
        double leaveMultiplier = GetTraversalMultiplier(currentAreaId, currentAreaId, costPolicy);
        double enterMultiplier = GetTraversalMultiplier(targetAreaId, targetAreaId, costPolicy);
        double candidate = currentCost + NavMeshPoint.Distance(GetPolygonCenter(currentNode), jump.Start) * leaveMultiplier +
                           jump.FixedCost + NavMeshPoint.Distance(jump.End, GetPolygonCenter(jump.TargetNode)) *
                           enterMultiplier;
        if (candidate >= workspace.GetCost(jump.TargetNode)) return;
        workspace.SetOpen(jump.TargetNode, candidate, currentNode,
            new TiledNavMeshTransition(TiledNavMeshTransitionKind.Jump, default, default, jump.Start, jump.End,
                jump.FixedCost));
        workspace.Open.Enqueue(jump.TargetNode, candidate + GetEstimatedRemainingCost(jump.TargetNode, goalCenter,
            minimumMultiplier, heuristicWeight, heuristicJumpCount, workspace));
    }

    private int GetNode(int tileIndex, int polygonIndex)
    {
        return m_PolygonOffsets[tileIndex] + polygonIndex;
    }

    private void GetTileAndPolygon(int node, out int tileIndex, out int polygonIndex)
    {
        int low = 0;
        int high = m_Tiles.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (node < m_PolygonOffsets[middle])
            {
                high = middle - 1;
                continue;
            }

            if (node >= m_PolygonOffsets[middle + 1])
            {
                low = middle + 1;
                continue;
            }

            tileIndex = middle;
            polygonIndex = node - m_PolygonOffsets[middle];
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(node));
    }

    private TiledNavMeshPolygon GetPolygon(int node)
    {
        GetTileAndPolygon(node, out int tileIndex, out int polygonIndex);
        return new TiledNavMeshPolygon(m_Tiles[tileIndex].TileId, polygonIndex);
    }

    private NavMeshPoint GetPolygonCenter(int node)
    {
        GetTileAndPolygon(node, out int tileIndex, out int polygonIndex);
        return m_Tiles[tileIndex].NavMesh.GetPolygonCenter(polygonIndex);
    }

    private double GetEstimatedRemainingCost(int node, NavMeshPoint goalCenter, double minimumMultiplier,
        double heuristicWeight, int heuristicJumpCount, TiledNavMeshQueryWorkspace workspace)
    {
        NavMeshPoint position = GetPolygonCenter(node);
        double lowerBound = NavMeshPoint.Distance(position, goalCenter) * minimumMultiplier;
        if (heuristicJumpCount > 0)
        {
            for (int index = 0; index < heuristicJumpCount; index++)
            {
                TiledHeuristicJump jump = m_HeuristicJumps[index];
                double candidate = NavMeshPoint.Distance(position, jump.Start) * minimumMultiplier + jump.FixedCost +
                                   workspace.JumpHeuristicCosts[index];
                if (candidate < lowerBound) lowerBound = candidate;
            }
        }
        else if (heuristicJumpCount < 0)
        {
            lowerBound = 0d;
        }

        return lowerBound * heuristicWeight;
    }

    private TiledHeuristicJump[] BuildHeuristicJumps()
    {
        if (!m_HasJumpConnections) return [];
        List<TiledHeuristicJump> jumps = Factory.RentList<TiledHeuristicJump>();
        try
        {
            foreach (TiledNavMeshTile tile in m_Tiles)
            {
                Mesh navMesh = tile.NavMesh;
                for (int polygonIndex = 0; polygonIndex < navMesh.PolygonCount; polygonIndex++)
                {
                    int jumpCount = navMesh.GetPolygonJumpCount(polygonIndex);
                    for (int jumpIndex = 0; jumpIndex < jumpCount; jumpIndex++)
                    {
                        jumps.Add(new TiledHeuristicJump(navMesh.GetPolygonJumpStart(polygonIndex, jumpIndex),
                            navMesh.GetPolygonJumpEnd(polygonIndex, jumpIndex),
                            navMesh.GetPolygonJumpFixedCost(polygonIndex, jumpIndex)));
                        if (jumps.Count > MaximumHeuristicJumpCount) return jumps.ToArray();
                    }
                }
            }

            return jumps.ToArray();
        }
        finally
        {
            Factory.Release(jumps);
        }
    }

    private static double[] BuildHeuristicJumpTransitionDistances(ReadOnlySpan<TiledHeuristicJump> jumps)
    {
        if (jumps.Length > MaximumHeuristicJumpCount) return [];
        double[] result = new double[jumps.Length * jumps.Length];
        for (int previousIndex = 0; previousIndex < jumps.Length; previousIndex++)
        {
            for (int nextIndex = 0; nextIndex < jumps.Length; nextIndex++)
            {
                result[previousIndex * jumps.Length + nextIndex] = NavMeshPoint.Distance(jumps[previousIndex].End,
                    jumps[nextIndex].Start);
            }
        }

        return result;
    }

    private int PrepareJumpHeuristic(NavMeshPoint goalCenter, double minimumMultiplier,
        TiledNavMeshQueryWorkspace workspace)
    {
        if (!m_HasJumpConnections) return 0;
        int count = m_HeuristicJumps.Length;
        if (count == 0 || count > MaximumHeuristicJumpCount) return -1;
        workspace.ResetJumpHeuristic(count);
        for (int index = 0; index < count; index++)
            workspace.JumpHeuristicCosts[index] = NavMeshPoint.Distance(m_HeuristicJumps[index].End, goalCenter) *
                                                  minimumMultiplier;

        // 跳跃之间的可行地面路径未知, 因而使用直线下界执行反向 Dijkstra, 保持估计值可采纳.
        for (int iteration = 0; iteration < count; iteration++)
        {
            int current = -1;
            double currentCost = double.PositiveInfinity;
            for (int index = 0; index < count; index++)
            {
                if (workspace.JumpHeuristicClosed[index] || workspace.JumpHeuristicCosts[index] >= currentCost)
                    continue;
                current = index;
                currentCost = workspace.JumpHeuristicCosts[index];
            }

            if (current < 0) break;
            workspace.JumpHeuristicClosed[current] = true;
            TiledHeuristicJump nextJump = m_HeuristicJumps[current];
            for (int index = 0; index < count; index++)
            {
                if (workspace.JumpHeuristicClosed[index]) continue;
                double transitionDistance = m_HeuristicJumpTransitionDistances[index * count + current];
                double candidate = transitionDistance * minimumMultiplier + nextJump.FixedCost + currentCost;
                if (candidate < workspace.JumpHeuristicCosts[index])
                    workspace.JumpHeuristicCosts[index] = candidate;
            }
        }

        return count;
    }

    private static int GetCorridorCount(int lastNode, TiledNavMeshQueryWorkspace workspace)
    {
        int count = 0;
        for (int node = lastNode; node >= 0; node = workspace.Parents[node]) count++;
        return count;
    }

    private NavMeshPoint[] BuildStraightPath(NavMeshPoint start, NavMeshPoint goal, List<int> nodes,
        ReadOnlySpan<TiledNavMeshPolygon> corridor, List<TiledNavMeshTransition> transitions,
        INavMeshTraversalCostPolicy costPolicy, out TiledNavMeshJumpTraversal[] jumps, out double totalCost)
    {
        List<NavMeshPoint> points = Factory.RentList<NavMeshPoint>();
        List<TiledNavMeshJumpTraversal> traversals = Factory.RentList<TiledNavMeshJumpTraversal>();
        try
        {
            totalCost = 0d;
            int firstPortalTransition = 1;
            NavMeshPoint segmentStart = start;
            for (int index = 1; index < transitions.Count; index++)
            {
                TiledNavMeshTransition transition = transitions[index];
                if (transition.Kind != TiledNavMeshTransitionKind.Jump) continue;
                int firstPointIndex = points.Count == 0 ? 0 : points.Count - 1;
                AppendGroundPath(points, segmentStart, transition.JumpStart, transitions, firstPortalTransition, index);
                totalCost += CalculateGroundPathCost(points, firstPointIndex, nodes, transitions, firstPortalTransition,
                    index, costPolicy);
                if (points[^1] != transition.JumpEnd) points.Add(transition.JumpEnd);
                totalCost += transition.JumpFixedCost;
                traversals.Add(new TiledNavMeshJumpTraversal(corridor[index - 1], corridor[index], transition.JumpStart,
                    transition.JumpEnd, transition.JumpFixedCost));
                firstPortalTransition = index + 1;
                segmentStart = transition.JumpEnd;
            }

            int finalFirstPointIndex = points.Count == 0 ? 0 : points.Count - 1;
            AppendGroundPath(points, segmentStart, goal, transitions, firstPortalTransition, transitions.Count);
            totalCost += CalculateGroundPathCost(points, finalFirstPointIndex, nodes, transitions,
                firstPortalTransition, transitions.Count, costPolicy);
            jumps = traversals.ToArray();
            return points.ToArray();
        }
        finally
        {
            Factory.Release(traversals);
            Factory.Release(points);
        }
    }

    private static void AppendGroundPath(List<NavMeshPoint> destination, NavMeshPoint start, NavMeshPoint goal,
        List<TiledNavMeshTransition> transitions, int firstTransition, int endExclusive)
    {
        List<NavMeshPortal> portals = Factory.RentList<NavMeshPortal>();
        try
        {
            for (int index = firstTransition; index < endExclusive; index++)
            {
                TiledNavMeshTransition transition = transitions[index];
                if (transition.Kind != TiledNavMeshTransitionKind.Portal)
                    throw new InvalidOperationException("地面路径段只能由 portal 转换组成.");
                portals.Add(new NavMeshPortal(transition.Left, transition.Right));
            }

            portals.Add(new NavMeshPortal(goal, goal));
            AppendFunnelPath(destination, start, goal, portals);
        }
        finally
        {
            Factory.Release(portals);
        }
    }

    private double CalculateGroundPathCost(List<NavMeshPoint> points, int firstPointIndex, List<int> nodes,
        List<TiledNavMeshTransition> transitions, int firstTransition, int endExclusive,
        INavMeshTraversalCostPolicy costPolicy)
    {
        if (ReferenceEquals(costPolicy, NavMeshQueryDefaults.CostPolicy))
        {
            double length = 0d;
            for (int pointIndex = firstPointIndex + 1; pointIndex < points.Count; pointIndex++)
                length += NavMeshPoint.Distance(points[pointIndex - 1], points[pointIndex]);
            return length;
        }

        double totalCost = 0d;
        int currentPolygonIndex = firstTransition - 1;
        int nextTransitionIndex = firstTransition;
        for (int pointIndex = firstPointIndex + 1; pointIndex < points.Count; pointIndex++)
        {
            NavMeshPoint start = points[pointIndex - 1];
            NavMeshPoint end = points[pointIndex];
            double segmentLength = NavMeshPoint.Distance(start, end);
            if (segmentLength == 0d) continue;

            double previousT = 0d;
            // funnel 航点可落在多个 portal 的公共端点上. 允许相同 t 的零长度区间, 才能保持 corridor 顺序.
            while (nextTransitionIndex < endExclusive)
            {
                TiledNavMeshTransition transition = transitions[nextTransitionIndex];
                if (!TryIntersectSegment(start, end, transition.Left, transition.Right, out double portalT) ||
                    portalT < previousT - 1e-12)
                    break;
                portalT = Math.Clamp(portalT, previousT, 1d);
                totalCost += segmentLength * (portalT - previousT) *
                    GetPolygonMovementMultiplier(nodes[currentPolygonIndex], costPolicy);
                previousT = portalT;
                currentPolygonIndex++;
                nextTransitionIndex++;
            }

            totalCost += segmentLength * (1d - previousT) *
                GetPolygonMovementMultiplier(nodes[currentPolygonIndex], costPolicy);
        }

        if (nextTransitionIndex != endExclusive)
            throw new InvalidOperationException("funnel 路径未按 corridor 顺序穿越全部 portal.");
        return totalCost;
    }

    private double GetPolygonMovementMultiplier(int node, INavMeshTraversalCostPolicy costPolicy)
    {
        GetTileAndPolygon(node, out int tileIndex, out int polygonIndex);
        int areaId = m_Tiles[tileIndex].NavMesh.GetPolygonAreaId(polygonIndex);
        return GetTraversalMultiplier(areaId, areaId, costPolicy);
    }

    private static bool TryIntersectSegment(NavMeshPoint rayStart, NavMeshPoint rayEnd, NavMeshPoint edgeStart,
        NavMeshPoint edgeEnd, out double t)
    {
        double rayX = rayEnd.X - rayStart.X;
        double rayY = rayEnd.Y - rayStart.Y;
        double edgeX = edgeEnd.X - edgeStart.X;
        double edgeY = edgeEnd.Y - edgeStart.Y;
        double cross = rayX * edgeY - rayY * edgeX;
        if (Math.Abs(cross) < 1e-12)
        {
            t = default;
            return false;
        }

        double offsetX = edgeStart.X - rayStart.X;
        double offsetY = edgeStart.Y - rayStart.Y;
        t = (offsetX * edgeY - offsetY * edgeX) / cross;
        double u = (offsetX * rayY - offsetY * rayX) / cross;
        return t >= 0d && t <= 1d && u >= 0d && u <= 1d;
    }

    private static void AppendFunnelPath(List<NavMeshPoint> destination, NavMeshPoint start, NavMeshPoint goal,
        List<NavMeshPortal> portals)
    {
        int skip = destination.Count == 0 ? 0 : 1;
        List<NavMeshPoint> result = Factory.RentList<NavMeshPoint>();
        try
        {
            result.Add(start);
            NavMeshPoint apex = start;
            NavMeshPoint left = portals[0].Left;
            NavMeshPoint right = portals[0].Right;
            int leftIndex = 0;
            int rightIndex = 0;
            for (int index = 1; index < portals.Count; index++)
            {
                NavMeshPortal portal = portals[index];
                if (NavMeshPoint.Cross(apex, right, portal.Right) <= 0d)
                {
                    if (apex == right || NavMeshPoint.Cross(apex, left, portal.Right) > 0d)
                    {
                        right = portal.Right;
                        rightIndex = index;
                    }
                    else
                    {
                        result.Add(left);
                        apex = left;
                        left = apex;
                        right = apex;
                        index = leftIndex;
                        rightIndex = leftIndex;
                        continue;
                    }
                }

                if (NavMeshPoint.Cross(apex, left, portal.Left) >= 0d)
                {
                    if (apex == left || NavMeshPoint.Cross(apex, right, portal.Left) < 0d)
                    {
                        left = portal.Left;
                        leftIndex = index;
                    }
                    else
                    {
                        result.Add(right);
                        apex = right;
                        left = apex;
                        right = apex;
                        index = rightIndex;
                        leftIndex = rightIndex;
                    }
                }
            }

            if (result[^1] != goal) result.Add(goal);
            for (int index = skip; index < result.Count; index++) destination.Add(result[index]);
        }
        finally
        {
            Factory.Release(result);
        }
    }

    private TiledPortalGraph GetPortalGraph()
    {
        return m_PortalGraph ??= BuildPortalGraph();
    }

    private TiledCrossTileJumpGraph BuildCrossTileJumpGraph(ReadOnlySpan<NavMeshJumpConnection> connections)
    {
        if (connections.IsEmpty) return new TiledCrossTileJumpGraph(new int[PolygonCount + 1], []);
        List<TiledCrossTileJump> jumps = Factory.RentList<TiledCrossTileJump>();
        try
        {
            for (int index = 0; index < connections.Length; index++)
            {
                NavMeshJumpConnection connection = connections[index];
                if (!connection.Start.IsFinite || !connection.End.IsFinite || !double.IsFinite(connection.FixedCost) ||
                    connection.FixedCost < 0d)
                    throw new ArgumentException($"跨 tile 跳跃连接 {index} 的端点或固定开销无效.", nameof(connections));
                if (!TryFindLocation(connection.Start, out TiledNavMeshLocation start) ||
                    !TryFindLocation(connection.End, out TiledNavMeshLocation end))
                    throw new ArgumentException($"跨 tile 跳跃连接 {index} 的端点必须位于当前导航网格内.", nameof(connections));
                int startNode = GetNode(FindTileIndex(start.TileId), start.Location.PolygonRef.Index);
                int endNode = GetNode(FindTileIndex(end.TileId), end.Location.PolygonRef.Index);
                jumps.Add(new TiledCrossTileJump(startNode, endNode, connection.Start, connection.End,
                    connection.FixedCost));
                if (connection.IsBidirectional)
                    jumps.Add(new TiledCrossTileJump(endNode, startNode, connection.End, connection.Start,
                        connection.FixedCost));
            }

            int[] counts = new int[PolygonCount];
            foreach (TiledCrossTileJump jump in jumps) counts[jump.SourceNode]++;
            int[] offsets = new int[PolygonCount + 1];
            for (int index = 0; index < counts.Length; index++) offsets[index + 1] = offsets[index] + counts[index];
            TiledCrossTileJump[] edges = new TiledCrossTileJump[offsets[^1]];
            int[] positions = offsets[..^1].ToArray();
            foreach (TiledCrossTileJump jump in jumps) edges[positions[jump.SourceNode]++] = jump;
            return new TiledCrossTileJumpGraph(offsets, edges);
        }
        finally
        {
            Factory.Release(jumps);
        }
    }

    private TiledPortalGraph BuildPortalGraph()
    {
        TiledNavMeshPortal[] portals = GetPortals();
        int[] counts = new int[PolygonCount];
        foreach (TiledNavMeshPortal portal in portals)
        {
            int first = GetNode(FindTileIndex(portal.FirstTileId), portal.FirstPolygonIndex);
            int second = GetNode(FindTileIndex(portal.SecondTileId), portal.SecondPolygonIndex);
            counts[first]++;
            counts[second]++;
        }

        int[] offsets = new int[PolygonCount + 1];
        for (int index = 0; index < counts.Length; index++) offsets[index + 1] = offsets[index] + counts[index];
        TiledPortalLink[] links = new TiledPortalLink[offsets[^1]];
        int[] writePositions = offsets[..^1].ToArray();
        foreach (TiledNavMeshPortal portal in portals)
        {
            int firstTileIndex = FindTileIndex(portal.FirstTileId);
            int secondTileIndex = FindTileIndex(portal.SecondTileId);
            int first = GetNode(firstTileIndex, portal.FirstPolygonIndex);
            int second = GetNode(secondTileIndex, portal.SecondPolygonIndex);
            NavMeshPoint firstCenter = m_Tiles[firstTileIndex].NavMesh.GetPolygonCenter(portal.FirstPolygonIndex);
            NavMeshPoint secondCenter = m_Tiles[secondTileIndex].NavMesh.GetPolygonCenter(portal.SecondPolygonIndex);
            double centerDistance = NavMeshPoint.Distance(firstCenter, secondCenter);
            NavMeshPortal firstToSecond = CreateDirectedPortal(firstCenter, secondCenter, portal.Segment);
            NavMeshPortal secondToFirst = CreateDirectedPortal(secondCenter, firstCenter, portal.Segment);
            links[writePositions[first]++] = new TiledPortalLink(
                second,
                centerDistance,
                new TiledNavMeshTransition(
                    TiledNavMeshTransitionKind.Portal,
                    firstToSecond.Left,
                    firstToSecond.Right,
                    default,
                    default,
                    default));
            links[writePositions[second]++] = new TiledPortalLink(
                first,
                centerDistance,
                new TiledNavMeshTransition(
                    TiledNavMeshTransitionKind.Portal,
                    secondToFirst.Left,
                    secondToFirst.Right,
                    default,
                    default,
                    default));
        }

        return new TiledPortalGraph(offsets, links);
    }

    private static NavMeshPortal CreateDirectedPortal(
        NavMeshPoint sourceCenter,
        NavMeshPoint targetCenter,
        in NavMeshSegment segment)
    {
        if (NavMeshPoint.Cross(sourceCenter, targetCenter, segment.Start) >= 0d)
        {
            return new NavMeshPortal(segment.End, segment.Start);
        }

        return new NavMeshPortal(segment.Start, segment.End);
    }

    private static double GetTraversalMultiplier(int fromAreaId, int toAreaId,
        INavMeshTraversalCostPolicy costPolicy)
    {
        double multiplier = costPolicy.GetMultiplier(fromAreaId, toAreaId);
        if (!double.IsFinite(multiplier) || multiplier < costPolicy.MinimumMultiplier)
            throw new InvalidOperationException("移动代价策略返回了小于 MinimumMultiplier 的无效倍率.");
        return multiplier;
    }

    private TiledNavMeshPortal[] GetPortals()
    {
        return m_Portals ??= BuildPortals();
    }

    private TiledNavMeshPortal[] BuildPortals()
    {
        List<TiledNavMeshPortal> portals = Factory.RentList<TiledNavMeshPortal>();
        try
        {
            for (int firstTileIndex = 0; firstTileIndex < m_Tiles.Length; firstTileIndex++)
            {
                TiledNavMeshTile firstTile = m_Tiles[firstTileIndex];
                for (int secondTileIndex = firstTileIndex + 1; secondTileIndex < m_Tiles.Length; secondTileIndex++)
                {
                    TiledNavMeshTile secondTile = m_Tiles[secondTileIndex];
                    if (!firstTile.Bounds.Overlaps(secondTile.Bounds)) continue;
                    foreach (TiledNavMeshBoundaryEdge firstEdge in firstTile.BoundaryEdges)
                    {
                        foreach (TiledNavMeshBoundaryEdge secondEdge in secondTile.BoundaryEdges)
                        {
                            if (!TryGetOverlappingSegment(firstEdge.Segment, secondEdge.Segment,
                                    out NavMeshSegment segment))
                                continue;
                            portals.Add(new TiledNavMeshPortal(firstTile.TileId, firstEdge.PolygonIndex,
                                secondTile.TileId, secondEdge.PolygonIndex, segment));
                        }
                    }
                }
            }

            return portals.ToArray();
        }
        finally
        {
            Factory.Release(portals);
        }
    }

    private static bool TryGetOverlappingSegment(in NavMeshSegment first, in NavMeshSegment second,
        out NavMeshSegment overlap)
    {
        double directionX = first.End.X - first.Start.X;
        double directionY = first.End.Y - first.Start.Y;
        double lengthSquared = directionX * directionX + directionY * directionY;
        if (lengthSquared <= 0d || Math.Abs(directionX * (second.Start.Y - first.Start.Y) -
                                 directionY * (second.Start.X - first.Start.X)) > 1e-12 ||
            Math.Abs(directionX * (second.End.Y - first.Start.Y) -
                     directionY * (second.End.X - first.Start.X)) > 1e-12)
        {
            overlap = default;
            return false;
        }

        double firstT = ((second.Start.X - first.Start.X) * directionX +
                         (second.Start.Y - first.Start.Y) * directionY) / lengthSquared;
        double secondT = ((second.End.X - first.Start.X) * directionX +
                          (second.End.Y - first.Start.Y) * directionY) / lengthSquared;
        double startT = Math.Max(0d, Math.Min(firstT, secondT));
        double endT = Math.Min(1d, Math.Max(firstT, secondT));
        if (endT - startT <= 1e-12)
        {
            overlap = default;
            return false;
        }

        overlap = new NavMeshSegment(new NavMeshPoint(first.Start.X + directionX * startT,
            first.Start.Y + directionY * startT), new NavMeshPoint(first.Start.X + directionX * endT,
            first.Start.Y + directionY * endT));
        return true;
    }

    private static int CompareTileIds(NavMeshTileId left, NavMeshTileId right)
    {
        int layer = left.Layer.CompareTo(right.Layer);
        if (layer != 0) return layer;
        int y = left.Y.CompareTo(right.Y);
        return y != 0 ? y : left.X.CompareTo(right.X);
    }

    private sealed class TiledPortalGraph
    {
        public TiledPortalGraph(int[] offsets, TiledPortalLink[] links)
        {
            Offsets = offsets;
            Links = links;
        }

        public int[] Offsets { get; }

        public TiledPortalLink[] Links { get; }
    }

    private sealed class TiledCrossTileJumpGraph
    {
        public TiledCrossTileJumpGraph(int[] offsets, TiledCrossTileJump[] jumps)
        {
            Offsets = offsets;
            Jumps = jumps;
        }

        public int[] Offsets { get; }

        public TiledCrossTileJump[] Jumps { get; }
    }

    private readonly record struct TiledPortalLink(
        int TargetNode,
        double CenterDistance,
        TiledNavMeshTransition Transition);

    private readonly record struct TiledHeuristicJump(NavMeshPoint Start, NavMeshPoint End, double FixedCost);

    private readonly record struct TiledCrossTileJump(int SourceNode, int TargetNode, NavMeshPoint Start,
        NavMeshPoint End, double FixedCost);

    private readonly record struct TiledTileBvhNode(TiledTileBounds Bounds, int Start, int Count, int Left, int Right)
    {
        public bool IsLeaf => Left < 0;

        public bool Contains(NavMeshPoint point) => Bounds.Contains(point);
    }

    private sealed class TileCenterComparer : IComparer<int>
    {
        private readonly TiledNavMeshTile[] m_Tiles;
        private readonly bool m_SplitX;

        public TileCenterComparer(TiledNavMeshTile[] tiles, bool splitX)
        {
            m_Tiles = tiles;
            m_SplitX = splitX;
        }

        public int Compare(int left, int right)
        {
            TiledTileBounds leftBounds = m_Tiles[left].Bounds;
            TiledTileBounds rightBounds = m_Tiles[right].Bounds;
            double leftCenter = m_SplitX ? leftBounds.CenterX : leftBounds.CenterY;
            double rightCenter = m_SplitX ? rightBounds.CenterX : rightBounds.CenterY;
            int comparison = leftCenter.CompareTo(rightCenter);
            return comparison != 0 ? comparison : left.CompareTo(right);
        }
    }
}

/// <summary>
/// 表示 tile 网格顶点的不可变二维轴对齐包围盒.
/// 仅用于组合快照内部的 tile 定位加速.
/// </summary>
internal readonly record struct TiledTileBounds(double MinX, double MinY, double MaxX, double MaxY)
{
    /// <summary>
    /// 获取包围盒在 X 轴上的宽度.
    /// </summary>
    public double Width => MaxX - MinX;

    /// <summary>
    /// 获取包围盒在 Y 轴上的高度.
    /// </summary>
    public double Height => MaxY - MinY;

    /// <summary>
    /// 获取包围盒在 X 轴上的中心坐标.
    /// </summary>
    public double CenterX => (MinX + MaxX) * 0.5d;

    /// <summary>
    /// 获取包围盒在 Y 轴上的中心坐标.
    /// </summary>
    public double CenterY => (MinY + MaxY) * 0.5d;

    /// <summary>
    /// 判断有限二维点是否落在包围盒中或其边界上.
    /// </summary>
    /// <param name="point">待判断的二维点.</param>
    /// <returns>点位于包围盒中时为 <see langword="true"/>.</returns>
    public bool Contains(NavMeshPoint point)
    {
        return point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;
    }

    /// <summary>
    /// 判断两个包围盒是否相交或接触.
    /// </summary>
    /// <param name="other">待判断的另一个包围盒.</param>
    /// <returns>两个包围盒存在公共区域或边界时为 <see langword="true"/>.</returns>
    public bool Overlaps(TiledTileBounds other)
    {
        return MinX <= other.MaxX && MaxX >= other.MinX && MinY <= other.MaxY && MaxY >= other.MinY;
    }

    /// <summary>
    /// 计算两个包围盒的并集.
    /// </summary>
    /// <param name="left">第一个包围盒.</param>
    /// <param name="right">第二个包围盒.</param>
    /// <returns>同时包含两个包围盒的最小轴对齐包围盒.</returns>
    public static TiledTileBounds Union(TiledTileBounds left, TiledTileBounds right)
    {
        return new TiledTileBounds(Math.Min(left.MinX, right.MinX), Math.Min(left.MinY, right.MinY),
            Math.Max(left.MaxX, right.MaxX), Math.Max(left.MaxY, right.MaxY));
    }
}

/// <summary>
/// 表示组合快照内部复用的不可变 tile 网格引用.
/// </summary>
internal sealed class TiledNavMeshTile
{
    private static readonly ConditionalWeakTable<Mesh, TiledNavMeshBoundaryEdge[]> s_BoundaryEdges =
        new ConditionalWeakTable<Mesh, TiledNavMeshBoundaryEdge[]>();

    /// <summary>
    /// 使用 tile 标识和不可变网格创建内部快照条目.
    /// </summary>
    /// <param name="tileId">tile 的全局标识.</param>
    /// <param name="navMesh">使用全局坐标构建的不可变导航网格.</param>
    internal TiledNavMeshTile(NavMeshTileId tileId, Mesh navMesh)
    {
        TileId = tileId;
        NavMesh = navMesh;
        BoundaryEdges = s_BoundaryEdges.GetValue(navMesh, static mesh => BuildBoundaryEdges(mesh));
        Bounds = CreateBounds(navMesh.VertexSpan);
    }

    /// <summary>
    /// 获取 tile 的全局标识.
    /// </summary>
    internal NavMeshTileId TileId { get; }

    /// <summary>
    /// 获取 tile 的不可变导航网格.
    /// </summary>
    internal Mesh NavMesh { get; }

    /// <summary>
    /// 获取 tile 内部 polygon 配对后保留的边界边模板.
    /// </summary>
    internal TiledNavMeshBoundaryEdge[] BoundaryEdges { get; }

    /// <summary>
    /// 获取 tile 全部顶点形成的轴对齐包围盒.
    /// </summary>
    internal TiledTileBounds Bounds { get; }

    /// <summary>
    /// 判断点是否位于 tile 的顶点包围盒内.
    /// </summary>
    /// <param name="point">待判断的有限二维点.</param>
    /// <returns>点在 tile 包围盒内时为 <see langword="true"/>.</returns>
    internal bool Contains(NavMeshPoint point) => Bounds.Contains(point);

    private static TiledTileBounds CreateBounds(ReadOnlySpan<NavMeshPoint> vertices)
    {
        if (vertices.IsEmpty) throw new ArgumentException("导航网格必须至少包含一个顶点.", nameof(vertices));
        NavMeshPoint first = vertices[0];
        double minX = first.X;
        double minY = first.Y;
        double maxX = first.X;
        double maxY = first.Y;
        for (int index = 1; index < vertices.Length; index++)
        {
            NavMeshPoint vertex = vertices[index];
            minX = Math.Min(minX, vertex.X);
            minY = Math.Min(minY, vertex.Y);
            maxX = Math.Max(maxX, vertex.X);
            maxY = Math.Max(maxY, vertex.Y);
        }

        return new TiledTileBounds(minX, minY, maxX, maxY);
    }

    private static TiledNavMeshBoundaryEdge[] BuildBoundaryEdges(Mesh navMesh)
    {
        Dictionary<long, TiledNavMeshBoundaryEdge> boundaries =
            new Dictionary<long, TiledNavMeshBoundaryEdge>();
        ReadOnlySpan<NavMeshPoint> vertices = navMesh.VertexSpan;
        for (int polygonIndex = 0; polygonIndex < navMesh.PolygonCount; polygonIndex++)
        {
            ReadOnlySpan<int> polygonVertices = navMesh.GetPolygonVertexIndices(polygonIndex);
            for (int vertexOffset = 0; vertexOffset < polygonVertices.Length; vertexOffset++)
            {
                int firstVertex = polygonVertices[vertexOffset];
                int secondVertex = polygonVertices[(vertexOffset + 1) % polygonVertices.Length];
                long edge = CreateVertexEdgeKey(firstVertex, secondVertex);
                if (boundaries.Remove(edge)) continue;
                boundaries.Add(edge, new TiledNavMeshBoundaryEdge(polygonIndex,
                    new NavMeshSegment(vertices[firstVertex], vertices[secondVertex])));
            }
        }

        return boundaries.Values.ToArray();
    }

    private static long CreateVertexEdgeKey(int first, int second)
    {
        uint minimum = (uint)Math.Min(first, second);
        uint maximum = (uint)Math.Max(first, second);
        return ((long)minimum << 32) | maximum;
    }
}

/// <summary>
/// 表示两个 tile 通过共线重叠的边界线段建立的双向门户.
/// polygon 索引仅能与各自 tile 的导航网格实例组合使用.
/// </summary>
public readonly record struct TiledNavMeshPortal(
    NavMeshTileId FirstTileId,
    int FirstPolygonIndex,
    NavMeshTileId SecondTileId,
    int SecondPolygonIndex,
    NavMeshSegment Segment);

/// <summary>
/// 表示组合 tile 快照中已定位到的可行走位置.
/// 本地 <see cref="NavMeshLocation"/> 仅能与对应的 <see cref="TileId"/> 所指 tile 网格组合使用.
/// </summary>
public readonly record struct TiledNavMeshLocation(NavMeshTileId TileId, NavMeshLocation Location);

/// <summary>
/// 标识组合 tile 快照中的一个逻辑 polygon.
/// PolygonIndex 仅相对于 TileId 对应的 tile 导航网格有效.
/// </summary>
public readonly record struct TiledNavMeshPolygon(NavMeshTileId TileId, int PolygonIndex);

/// <summary>
/// 表示用于构造跨 tile 门户的内部边界端点.
/// </summary>
internal readonly record struct TiledNavMeshPortalEndpoint(
    NavMeshTileId TileId,
    int PolygonIndex,
    NavMeshSegment Segment);

/// <summary>
/// 表示可由不同 tile 实例复用的本地边界边模板.
/// </summary>
internal readonly record struct TiledNavMeshBoundaryEdge(int PolygonIndex, NavMeshSegment Segment);

/// <summary>
/// 表示忽略方向后的全局边界线段键.
/// </summary>
internal readonly record struct TiledNavMeshEdgeKey
{
    /// <summary>
    /// 使用线段端点创建无方向边界键.
    /// </summary>
    /// <param name="segment">需要转换的边界线段.</param>
    internal TiledNavMeshEdgeKey(NavMeshSegment segment)
    {
        if (ComparePoints(segment.Start, segment.End) <= 0)
        {
            First = segment.Start;
            Second = segment.End;
        }
        else
        {
            First = segment.End;
            Second = segment.Start;
        }
    }

    /// <summary>
    /// 获取按稳定顺序排列的第一个端点.
    /// </summary>
    internal readonly NavMeshPoint First;

    /// <summary>
    /// 获取按稳定顺序排列的第二个端点.
    /// </summary>
    internal readonly NavMeshPoint Second;

    private static int ComparePoints(NavMeshPoint left, NavMeshPoint right)
    {
        int x = left.X.CompareTo(right.X);
        return x != 0 ? x : left.Y.CompareTo(right.Y);
    }
}
