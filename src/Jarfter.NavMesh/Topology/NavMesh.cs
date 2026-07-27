using Jarfter.Core.Collections.ObjectModel;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Query;
using System.Runtime.InteropServices;

namespace Jarfter.NavMesh.Topology;

/// <summary>
/// 不可变的二维三角导航网格, 支持同步路径查询.
/// </summary>
public sealed class NavMesh
{
    private const int MaximumHeuristicJumpCount = 64;

    private static long s_NextId;

    private readonly long m_Id;
    private readonly NavMeshPoint[] m_Vertices;
    private readonly NavMeshTriangle[] m_Triangles;
    private readonly NavMeshPoint[] m_Centers;
    private readonly int[] m_Neighbors;
    private readonly PolygonInfo[] m_Polygons;
    private readonly int[] m_TrianglePolygons;
    private readonly PolygonNeighbor[][] m_PolygonNeighbors;
    private readonly BvhNode[] m_PolygonBvhNodes;
    private readonly int[] m_BvhPolygons;
    private readonly JumpEdge[][] m_JumpEdges;
    private readonly NavMeshJumpConnection[] m_JumpConnections;
    private readonly HeuristicJump[] m_HeuristicJumps;
    private readonly double[] m_HeuristicJumpTransitionDistances;
    private readonly BvhNode[] m_BvhNodes;
    private readonly int[] m_BvhTriangles;

    private NavMesh(NavMeshPoint[] vertices, NavMeshTriangle[] triangles, NavMeshPoint[] centers, int[] neighbors,
        PolygonInfo[] polygons, int[] trianglePolygons, PolygonNeighbor[][] polygonNeighbors, JumpEdge[][] jumpEdges,
        NavMeshJumpConnection[] jumpConnections, HeuristicJump[] heuristicJumps,
        double[] heuristicJumpTransitionDistances, BvhNode[] bvhNodes, int[] bvhTriangles,
        BvhNode[] polygonBvhNodes, int[] bvhPolygons)
    {
        m_Id = Interlocked.Increment(ref s_NextId);
        m_Vertices = vertices;
        m_Triangles = triangles;
        m_Centers = centers;
        m_Neighbors = neighbors;
        m_Polygons = polygons;
        m_TrianglePolygons = trianglePolygons;
        m_PolygonNeighbors = polygonNeighbors;
        m_PolygonBvhNodes = polygonBvhNodes;
        m_BvhPolygons = bvhPolygons;
        m_JumpEdges = jumpEdges;
        m_JumpConnections = jumpConnections;
        m_HeuristicJumps = heuristicJumps;
        m_HeuristicJumpTransitionDistances = heuristicJumpTransitionDistances;
        m_BvhNodes = bvhNodes;
        m_BvhTriangles = bvhTriangles;
    }

    /// <summary>
    /// 获取仅供同一程序集运行时组合网格使用的顶点只读跨度.
    /// </summary>
    internal ReadOnlySpan<NavMeshPoint> VertexSpan => m_Vertices;

    /// <summary>
    /// 获取仅供同一程序集运行时组合网格使用的三角形只读跨度.
    /// </summary>
    internal ReadOnlySpan<NavMeshTriangle> TriangleSpan => m_Triangles;

    /// <summary>
    /// 获取仅供同一程序集组合快照使用的跳跃连接只读跨度.
    /// </summary>
    internal ReadOnlySpan<NavMeshJumpConnection> JumpConnectionSpan => m_JumpConnections;

    /// <summary>
    /// 获取网格中的三角形数量.
    /// </summary>
    public int TriangleCount => m_Triangles.Length;

    /// <summary>
    /// 获取网格中的顶点数量.
    /// </summary>
    public int VertexCount => m_Vertices.Length;

    /// <summary>
    /// 获取逻辑凸多边形数量.
    /// 由三角形创建的网格中, 每个三角形都是一个多边形; 由凸多边形创建的网格中, 此值保留原始多边形数量.
    /// </summary>
    public int PolygonCount => m_Polygons.Length;

    /// <summary>
    /// 获取跳跃连接数量.
    /// </summary>
    public int JumpConnectionCount => m_JumpConnections.Length;

    /// <summary>
    /// 将跳跃连接复制到调用方提供的缓冲区.
    /// </summary>
    /// <param name="destination">接收跳跃连接的缓冲区.</param>
    /// <returns>实际写入的连接数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyJumpConnections(Span<NavMeshJumpConnection> destination)
    {
        int written = Math.Min(destination.Length, m_JumpConnections.Length);
        m_JumpConnections.AsSpan(0, written).CopyTo(destination);
        return written;
    }

    /// <summary>
    /// 将顶点坐标复制到调用方提供的缓冲区.
    /// </summary>
    /// <param name="destination">接收顶点的缓冲区.</param>
    /// <returns>实际写入的顶点数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyVertices(Span<NavMeshPoint> destination)
    {
        int written = Math.Min(destination.Length, m_Vertices.Length);
        m_Vertices.AsSpan(0, written).CopyTo(destination);
        return written;
    }

    /// <summary>
    /// 将三角形拓扑复制到调用方提供的缓冲区.
    /// </summary>
    /// <param name="destination">接收三角形的缓冲区.</param>
    /// <returns>实际写入的三角形数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyTriangles(Span<NavMeshTriangle> destination)
    {
        int written = Math.Min(destination.Length, m_Triangles.Length);
        m_Triangles.AsSpan(0, written).CopyTo(destination);
        return written;
    }

    /// <summary>
    /// 将逻辑凸多边形定义复制到调用方提供的缓冲区.
    /// 返回的多边形拥有独立的顶点索引数组, 可安全保存或传递给其他网格.
    /// </summary>
    /// <param name="destination">接收凸多边形的缓冲区.</param>
    /// <returns>实际写入的多边形数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyPolygons(Span<NavMeshConvexPolygon> destination)
    {
        int written = Math.Min(destination.Length, m_Polygons.Length);
        for (int index = 0; index < written; index++)
        {
            PolygonInfo polygon = m_Polygons[index];
            destination[index] = new NavMeshConvexPolygon(polygon.Vertices, polygon.AreaId, polygon.Flags);
        }

        return written;
    }

    /// <summary>
    /// 获取指定三角形的区域标识.
    /// </summary>
    /// <param name="triangleIndex">三角形索引.</param>
    /// <returns>构建拓扑时提供的区域标识.</returns>
    public int GetAreaId(int triangleIndex)
    {
        return m_Triangles[triangleIndex].AreaId;
    }

    /// <summary>
    /// 获取指定三角形的通行 flags 位掩码.
    /// </summary>
    /// <param name="triangleIndex">三角形索引.</param>
    /// <returns>构建拓扑时提供的通行 flags.</returns>
    public uint GetFlags(int triangleIndex)
    {
        return m_Triangles[triangleIndex].Flags;
    }

    /// <summary>
    /// 判断 polygon 引用是否属于当前不可变导航网格.
    /// </summary>
    /// <param name="polygonRef">待验证的 polygon 引用.</param>
    /// <returns>引用可用于当前网格时为 <see langword="true"/>.</returns>
    public bool IsValidPolygonRef(NavMeshPolygonRef polygonRef)
    {
        return polygonRef.NavMeshId == m_Id && (uint)polygonRef.Index < m_Polygons.Length;
    }

    /// <summary>
    /// 尝试定位位于网格内的点并返回其所属 polygon 引用.
    /// </summary>
    /// <param name="point">待定位的有限二维坐标.</param>
    /// <param name="location">成功时包含 polygon 引用和原始坐标的位置.</param>
    /// <returns>点位于网格内时为 <see langword="true"/>.</returns>
    public bool TryFindLocation(NavMeshPoint point, out NavMeshLocation location)
    {
        return TryFindLocation(point, NavMeshQueryDefaults.Filter, out location);
    }

    /// <summary>
    /// 尝试定位位于允许 polygon 内的点并返回其所属引用.
    /// </summary>
    /// <param name="point">待定位的有限二维坐标.</param>
    /// <param name="filter">决定 polygon 是否允许参与定位的过滤器.</param>
    /// <param name="location">成功时包含 polygon 引用和原始坐标的位置.</param>
    /// <returns>点位于满足过滤器的 polygon 内时为 <see langword="true"/>.</returns>
    public bool TryFindLocation(NavMeshPoint point, INavMeshQueryFilter filter, out NavMeshLocation location)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!point.IsFinite) throw new ArgumentException("查询点必须为有限 double 坐标.", nameof(point));
        int polygonIndex = FindContainingPolygon(point);
        if (polygonIndex < 0 || !filter.Pass(polygonIndex, GetPolygonAreaId(polygonIndex), GetPolygonFlags(polygonIndex)))
        {
            location = default;
            return false;
        }

        location = new NavMeshLocation(new NavMeshPolygonRef(m_Id, polygonIndex), point);
        return true;
    }

    /// <summary>
    /// 尝试将坐标投影到指定的已知 polygon, 并返回可缓存的位置.
    /// 该方法不执行全局空间索引查询, 适合调用方已保存当前 polygon 引用的连续位置修正.
    /// </summary>
    /// <param name="polygonRef">当前网格返回的 polygon 引用.</param>
    /// <param name="point">待投影的有限二维坐标.</param>
    /// <param name="location">成功时包含 polygon 内或边界上的最近位置.</param>
    /// <returns>引用属于当前网格时为 <see langword="true"/>.</returns>
    public bool TryProjectToPolygon(NavMeshPolygonRef polygonRef, NavMeshPoint point, out NavMeshLocation location)
    {
        if (!point.IsFinite) throw new ArgumentException("查询点必须为有限 double 坐标.", nameof(point));
        if (!IsValidPolygonRef(polygonRef))
        {
            location = default;
            return false;
        }

        NavMeshPoint projected = ClosestPointOnPolygon(polygonRef.Index, point);
        location = new NavMeshLocation(polygonRef, projected);
        return true;
    }

    /// <summary>
    /// 尝试将坐标投影到指定已知 polygon 的边界上, 并返回可缓存的位置.
    /// 即使输入点位于 polygon 内部, 也会返回最近边界点.
    /// </summary>
    /// <param name="polygonRef">当前网格返回的 polygon 引用.</param>
    /// <param name="point">待投影的有限二维坐标.</param>
    /// <param name="location">成功时包含 polygon 边界上的最近位置.</param>
    /// <returns>引用属于当前网格时为 <see langword="true"/>.</returns>
    public bool TryProjectToPolygonBoundary(NavMeshPolygonRef polygonRef, NavMeshPoint point,
        out NavMeshLocation location)
    {
        if (!point.IsFinite) throw new ArgumentException("查询点必须为有限 double 坐标.", nameof(point));
        if (!IsValidPolygonRef(polygonRef))
        {
            location = default;
            return false;
        }

        location = new NavMeshLocation(polygonRef, ClosestPointOnPolygonBoundary(polygonRef.Index, point));
        return true;
    }

    /// <summary>
    /// 创建可在单一调用线程中反复使用的查询工作区.
    /// </summary>
    /// <returns>供路径查询复用的工作区.</returns>
    public NavMeshQueryWorkspace CreateQueryWorkspace() => new NavMeshQueryWorkspace();

    /// <summary>
    /// 将包围盒与其自身包围盒相交的三角形索引复制到调用方缓冲区.
    /// </summary>
    /// <param name="bounds">查询使用的轴对齐包围盒.</param>
    /// <param name="destination">接收三角形索引的缓冲区.</param>
    /// <returns>实际写入的索引数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyTrianglesOverlappingBounds(NavMeshBounds bounds, Span<int> destination)
    {
        if (!double.IsFinite(bounds.MinX) || !double.IsFinite(bounds.MinY) || !double.IsFinite(bounds.MaxX) ||
            !double.IsFinite(bounds.MaxY) || bounds.MinX > bounds.MaxX || bounds.MinY > bounds.MaxY)
            throw new ArgumentOutOfRangeException(nameof(bounds));

        Span<int> pendingNodes = stackalloc int[64];
        int pendingCount = 1;
        int written = 0;
        pendingNodes[0] = 0;
        while (pendingCount > 0)
        {
            BvhNode node = m_BvhNodes[pendingNodes[--pendingCount]];
            if (!Overlaps(bounds, node.MinX, node.MinY, node.MaxX, node.MaxY)) continue;
            if (node.Count == 0)
            {
                pendingNodes[pendingCount++] = node.Left;
                pendingNodes[pendingCount++] = node.Right;
                continue;
            }

            for (int offset = 0; offset < node.Count; offset++)
            {
                int triangleIndex = m_BvhTriangles[node.Start + offset];
                if (!TriangleOverlapsBounds(triangleIndex, bounds)) continue;
                if (written >= destination.Length) return written;
                destination[written++] = triangleIndex;
            }
        }

        return written;
    }

    /// <summary>
    /// 将包围盒与其自身包围盒相交的逻辑 polygon 引用复制到调用方缓冲区.
    /// </summary>
    /// <param name="bounds">查询使用的轴对齐包围盒.</param>
    /// <param name="destination">接收 polygon 引用的缓冲区.</param>
    /// <returns>实际写入的引用数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyPolygonsOverlappingBounds(NavMeshBounds bounds, Span<NavMeshPolygonRef> destination)
    {
        return CopyPolygonsOverlappingBounds(bounds, NavMeshQueryDefaults.Filter, destination);
    }

    /// <summary>
    /// 将包围盒与其自身包围盒相交且满足过滤器的逻辑 polygon 引用复制到调用方缓冲区.
    /// </summary>
    /// <param name="bounds">查询使用的轴对齐包围盒.</param>
    /// <param name="filter">决定 polygon 是否可参与查询的过滤器.</param>
    /// <param name="destination">接收 polygon 引用的缓冲区.</param>
    /// <returns>实际写入的引用数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyPolygonsOverlappingBounds(NavMeshBounds bounds, INavMeshQueryFilter filter,
        Span<NavMeshPolygonRef> destination)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!double.IsFinite(bounds.MinX) || !double.IsFinite(bounds.MinY) || !double.IsFinite(bounds.MaxX) ||
            !double.IsFinite(bounds.MaxY) || bounds.MinX > bounds.MaxX || bounds.MinY > bounds.MaxY)
            throw new ArgumentOutOfRangeException(nameof(bounds));

        Span<int> pendingNodes = stackalloc int[64];
        int pendingCount = 1;
        int written = 0;
        pendingNodes[0] = 0;
        while (pendingCount > 0)
        {
            BvhNode node = m_PolygonBvhNodes[pendingNodes[--pendingCount]];
            if (!Overlaps(bounds, node.MinX, node.MinY, node.MaxX, node.MaxY)) continue;
            if (node.Count == 0)
            {
                pendingNodes[pendingCount++] = node.Left;
                pendingNodes[pendingCount++] = node.Right;
                continue;
            }

            for (int offset = 0; offset < node.Count; offset++)
            {
                int polygonIndex = m_BvhPolygons[node.Start + offset];
                if (!PolygonOverlapsBounds(polygonIndex, bounds) ||
                    !filter.Pass(polygonIndex, GetPolygonAreaId(polygonIndex), GetPolygonFlags(polygonIndex)))
                    continue;
                if (written >= destination.Length) return written;
                destination[written++] = new NavMeshPolygonRef(m_Id, polygonIndex);
            }
        }

        return written;
    }

    /// <summary>
    /// 尝试查找距离指定点最近的可行走三角形位置.
    /// </summary>
    /// <param name="point">待投影的二维点.</param>
    /// <param name="nearestTriangleIndex">最近三角形索引.</param>
    /// <param name="nearestPoint">位于最近三角形上的投影点.</param>
    /// <returns>网格非空且输入有效时为 <see langword="true"/>.</returns>
    public bool TryFindNearestPoint(NavMeshPoint point, out int nearestTriangleIndex, out NavMeshPoint nearestPoint)
    {
        return TryFindNearestPoint(point, NavMeshQueryDefaults.Filter, out nearestTriangleIndex, out nearestPoint);
    }

    /// <summary>
    /// 尝试查找距离指定点最近且满足过滤器的可行走三角形位置.
    /// </summary>
    /// <param name="point">待投影的二维点.</param>
    /// <param name="filter">决定三角形是否可参与查询的过滤器.</param>
    /// <param name="nearestTriangleIndex">最近三角形索引.</param>
    /// <param name="nearestPoint">位于最近三角形上的投影点.</param>
    /// <returns>存在满足过滤器的三角形时为 <see langword="true"/>.</returns>
    public bool TryFindNearestPoint(NavMeshPoint point, INavMeshQueryFilter filter, out int nearestTriangleIndex,
        out NavMeshPoint nearestPoint)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!point.IsFinite) throw new ArgumentException("查询点必须为有限 double 坐标.", nameof(point));
        nearestTriangleIndex = -1;
        nearestPoint = default;
        double nearestDistanceSquared = double.PositiveInfinity;
        // 平衡 BVH 的最大深度远小于 64 (int 数量上限下最多 31 层), 因而固定栈空间足够且无分配.
        Span<int> pendingNodes = stackalloc int[64];
        int pendingCount = 1;
        pendingNodes[0] = 0;
        while (pendingCount > 0)
        {
            BvhNode node = m_BvhNodes[pendingNodes[--pendingCount]];
            if (DistanceSquaredToBounds(point, node) >= nearestDistanceSquared) continue;
            if (node.Left < 0)
            {
                for (int offset = 0; offset < node.Count; offset++)
                {
                    int triangleIndex = m_BvhTriangles[node.Start + offset];
                    int polygonIndex = m_TrianglePolygons[triangleIndex];
                    if (!filter.Pass(polygonIndex, GetPolygonAreaId(polygonIndex), GetPolygonFlags(polygonIndex)))
                        continue;
                    NavMeshPoint candidate = ClosestPointOnTriangle(point, m_Triangles[triangleIndex]);
                    double x = candidate.X - point.X;
                    double y = candidate.Y - point.Y;
                    double distanceSquared = x * x + y * y;
                    if (distanceSquared >= nearestDistanceSquared) continue;
                    nearestDistanceSquared = distanceSquared;
                    nearestTriangleIndex = triangleIndex;
                    nearestPoint = candidate;
                }

                continue;
            }

            BvhNode left = m_BvhNodes[node.Left];
            BvhNode right = m_BvhNodes[node.Right];
            if (DistanceSquaredToBounds(point, left) < DistanceSquaredToBounds(point, right))
            {
                pendingNodes[pendingCount++] = node.Right;
                pendingNodes[pendingCount++] = node.Left;
            }
            else
            {
                pendingNodes[pendingCount++] = node.Left;
                pendingNodes[pendingCount++] = node.Right;
            }
        }

        return nearestTriangleIndex >= 0;
    }

    /// <summary>
    /// 尝试将指定点投影到最近可行走位置, 并返回可供后续查询复用的 location.
    /// </summary>
    /// <param name="point">待投影的二维点.</param>
    /// <param name="location">成功时包含最近投影点及其所属 polygon 引用.</param>
    /// <returns>网格非空且输入有效时为 <see langword="true"/>.</returns>
    public bool TryFindNearestLocation(NavMeshPoint point, out NavMeshLocation location)
    {
        return TryFindNearestLocation(point, NavMeshQueryDefaults.Filter, out location);
    }

    /// <summary>
    /// 尝试将指定点投影到最近且满足过滤器的可行走位置, 并返回可缓存的 location.
    /// </summary>
    /// <param name="point">待投影的二维点.</param>
    /// <param name="filter">决定 polygon 是否可参与投影的过滤器.</param>
    /// <param name="location">成功时包含最近投影点及其所属 polygon 引用.</param>
    /// <returns>存在满足过滤器的可行走位置时为 <see langword="true"/>.</returns>
    public bool TryFindNearestLocation(NavMeshPoint point, INavMeshQueryFilter filter, out NavMeshLocation location)
    {
        if (!TryFindNearestPoint(point, filter, out int triangleIndex, out NavMeshPoint nearestPoint))
        {
            location = default;
            return false;
        }

        int polygonIndex = m_TrianglePolygons[triangleIndex];
        location = new NavMeshLocation(new NavMeshPolygonRef(m_Id, polygonIndex), nearestPoint);
        return true;
    }

    /// <summary>
    /// 尝试在以查询点为中心、指定半尺寸为范围的包围盒内查找最近可行走位置.
    /// 只有自身包围盒与搜索范围相交的 polygon 会参与查询.
    /// </summary>
    /// <param name="point">待投影的有限二维点.</param>
    /// <param name="halfExtents">搜索范围在 X 和 Y 方向的非负半尺寸.</param>
    /// <param name="filter">决定 polygon 是否可参与投影的过滤器.</param>
    /// <param name="location">成功时包含最近投影点及其所属 polygon 引用.</param>
    /// <returns>存在满足过滤器且位于搜索范围内的 polygon 时为 <see langword="true"/>.</returns>
    public bool TryFindNearestLocation(NavMeshPoint point, NavMeshPoint halfExtents, INavMeshQueryFilter filter,
        out NavMeshLocation location)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!point.IsFinite) throw new ArgumentException("查询点必须为有限 double 坐标.", nameof(point));
        if (!halfExtents.IsFinite || halfExtents.X < 0 || halfExtents.Y < 0)
            throw new ArgumentOutOfRangeException(nameof(halfExtents));
        NavMeshBounds bounds = new NavMeshBounds(point.X - halfExtents.X, point.Y - halfExtents.Y,
            point.X + halfExtents.X, point.Y + halfExtents.Y);
        int nearestPolygon = -1;
        NavMeshPoint nearestPoint = default;
        double nearestDistanceSquared = double.PositiveInfinity;
        Span<int> pendingNodes = stackalloc int[64];
        int pendingCount = 1;
        pendingNodes[0] = 0;
        while (pendingCount > 0)
        {
            BvhNode node = m_PolygonBvhNodes[pendingNodes[--pendingCount]];
            if (!Overlaps(bounds, node.MinX, node.MinY, node.MaxX, node.MaxY)) continue;
            if (node.Count == 0)
            {
                pendingNodes[pendingCount++] = node.Left;
                pendingNodes[pendingCount++] = node.Right;
                continue;
            }

            for (int offset = 0; offset < node.Count; offset++)
            {
                int polygonIndex = m_BvhPolygons[node.Start + offset];
                if (!PolygonOverlapsBounds(polygonIndex, bounds) ||
                    !filter.Pass(polygonIndex, GetPolygonAreaId(polygonIndex), GetPolygonFlags(polygonIndex)))
                    continue;
                NavMeshPoint candidate = ClosestPointOnPolygon(polygonIndex, point);
                double distanceSquared = DistanceSquared(point, candidate);
                if (distanceSquared >= nearestDistanceSquared) continue;
                nearestPolygon = polygonIndex;
                nearestPoint = candidate;
                nearestDistanceSquared = distanceSquared;
            }
        }

        if (nearestPolygon < 0)
        {
            location = default;
            return false;
        }

        location = new NavMeshLocation(new NavMeshPolygonRef(m_Id, nearestPolygon), nearestPoint);
        return true;
    }

    /// <summary>
    /// 尝试在指定已知凸 polygon 内按面积均匀随机选取一个位置.
    /// 该方法不执行全局查询, 返回的位置可直接用于后续路径查询.
    /// </summary>
    /// <param name="polygonRef">当前网格返回的 polygon 引用.</param>
    /// <param name="random">提供随机 double 的随机源.</param>
    /// <param name="location">成功时包含 polygon 内的随机位置.</param>
    /// <returns>引用属于当前网格时为 <see langword="true"/>.</returns>
    public bool TryFindRandomPoint(NavMeshPolygonRef polygonRef, Random random, out NavMeshLocation location)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (!IsValidPolygonRef(polygonRef))
        {
            location = default;
            return false;
        }

        ReadOnlySpan<int> vertices = m_Polygons[polygonRef.Index].Vertices;
        NavMeshPoint first = m_Vertices[vertices[0]];
        double totalArea = 0;
        for (int index = 1; index < vertices.Length - 1; index++)
            totalArea += NavMeshPoint.Cross(first, m_Vertices[vertices[index]], m_Vertices[vertices[index + 1]]) * 0.5;

        double targetArea = random.NextDouble() * totalArea;
        double accumulatedArea = 0;
        int triangleIndex = vertices.Length - 2;
        for (int index = 1; index < vertices.Length - 1; index++)
        {
            double area = NavMeshPoint.Cross(first, m_Vertices[vertices[index]], m_Vertices[vertices[index + 1]]) * 0.5;
            accumulatedArea += area;
            if (targetArea > accumulatedArea) continue;
            triangleIndex = index;
            break;
        }

        NavMeshPoint second = m_Vertices[vertices[triangleIndex]];
        NavMeshPoint third = m_Vertices[vertices[triangleIndex + 1]];
        double root = Math.Sqrt(random.NextDouble());
        double thirdWeight = random.NextDouble() * root;
        double secondWeight = root - thirdWeight;
        NavMeshPoint point = new NavMeshPoint(first.X + (second.X - first.X) * secondWeight +
                                               (third.X - first.X) * thirdWeight,
            first.Y + (second.Y - first.Y) * secondWeight + (third.Y - first.Y) * thirdWeight);
        location = new NavMeshLocation(polygonRef, point);
        return true;
    }

    /// <summary>
    /// 尝试按可行走三角形面积权重随机选取一个点.
    /// </summary>
    /// <param name="random">提供随机 double 的随机源.</param>
    /// <param name="triangleIndex">随机选中的三角形索引.</param>
    /// <param name="point">随机点.</param>
    /// <returns>网格非空时为 <see langword="true"/>.</returns>
    public bool TryFindRandomPoint(Random random, out int triangleIndex, out NavMeshPoint point)
    {
        return TryFindRandomPoint(random, NavMeshQueryDefaults.Filter, out triangleIndex, out point);
    }

    /// <summary>
    /// 尝试按满足过滤器的可行走三角形面积权重随机选取一个点.
    /// </summary>
    /// <param name="random">提供随机 double 的随机源.</param>
    /// <param name="filter">决定三角形是否可参与查询的过滤器.</param>
    /// <param name="triangleIndex">随机选中的三角形索引.</param>
    /// <param name="point">随机点.</param>
    /// <returns>存在满足过滤器的三角形时为 <see langword="true"/>.</returns>
    public bool TryFindRandomPoint(Random random, INavMeshQueryFilter filter, out int triangleIndex,
        out NavMeshPoint point)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(filter);
        double totalArea = 0;
        for (int index = 0; index < m_Triangles.Length; index++)
        {
            if (!filter.Pass(index, GetAreaId(index), GetFlags(index))) continue;
            totalArea += TriangleArea(index);
        }

        if (totalArea == 0)
        {
            triangleIndex = -1;
            point = default;
            return false;
        }

        double target = random.NextDouble() * totalArea;
        double accumulatedArea = 0;
        triangleIndex = m_Triangles.Length - 1;
        for (int index = 0; index < m_Triangles.Length; index++)
        {
            if (!filter.Pass(index, GetAreaId(index), GetFlags(index))) continue;
            accumulatedArea += TriangleArea(index);
            if (target > accumulatedArea) continue;
            triangleIndex = index;
            break;
        }

        NavMeshTriangle triangle = m_Triangles[triangleIndex];
        NavMeshPoint first = m_Vertices[triangle.First];
        NavMeshPoint second = m_Vertices[triangle.Second];
        NavMeshPoint third = m_Vertices[triangle.Third];
        double root = Math.Sqrt(random.NextDouble());
        double secondWeight = root * (1 - random.NextDouble());
        double thirdWeight = root - secondWeight;
        point = new NavMeshPoint(first.X * (1 - root) + second.X * secondWeight + third.X * thirdWeight,
            first.Y * (1 - root) + second.Y * secondWeight + third.Y * thirdWeight);
        return true;
    }

    /// <summary>
    /// 将网格外边界线段复制到调用方提供的缓冲区.
    /// </summary>
    /// <param name="destination">接收边界线段的缓冲区.</param>
    /// <returns>实际写入的线段数量.</returns>
    public int CopyBoundarySegments(Span<NavMeshSegment> destination)
    {
        int written = 0;
        for (int triangleIndex = 0; triangleIndex < m_Triangles.Length; triangleIndex++)
        {
            NavMeshTriangle triangle = m_Triangles[triangleIndex];
            for (int edge = 0; edge < 3; edge++)
            {
                if (m_Neighbors[triangleIndex * 3 + edge] >= 0) continue;
                if (written >= destination.Length) return written;
                int start = edge switch { 0 => triangle.First, 1 => triangle.Second, _ => triangle.Third };
                int end = edge switch { 0 => triangle.Second, 1 => triangle.Third, _ => triangle.First };
                destination[written++] = new NavMeshSegment(m_Vertices[start], m_Vertices[end]);
            }
        }

        return written;
    }

    /// <summary>
    /// 将指定 polygon 的可阻挡墙段复制到调用方提供的缓冲区.
    /// 外边界以及通向不可通行相邻 polygon 的共享边均会作为墙段返回.
    /// </summary>
    /// <param name="polygonRef">当前网格返回的 polygon 引用.</param>
    /// <param name="destination">接收墙段的缓冲区.</param>
    /// <returns>实际写入的墙段数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyPolygonWallSegments(NavMeshPolygonRef polygonRef, Span<NavMeshSegment> destination)
    {
        return CopyPolygonWallSegments(polygonRef, NavMeshQueryDefaults.Filter, destination);
    }

    /// <summary>
    /// 将指定 polygon 在给定过滤器下可阻挡的墙段复制到调用方提供的缓冲区.
    /// </summary>
    /// <param name="polygonRef">当前网格返回的 polygon 引用.</param>
    /// <param name="filter">决定相邻 polygon 是否可穿越的过滤器.</param>
    /// <param name="destination">接收墙段的缓冲区.</param>
    /// <returns>实际写入的墙段数量. 缓冲区不足时返回 destination 的长度.</returns>
    /// <exception cref="ArgumentException">polygon 引用不属于当前网格.</exception>
    public int CopyPolygonWallSegments(NavMeshPolygonRef polygonRef, INavMeshQueryFilter filter,
        Span<NavMeshSegment> destination)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!IsValidPolygonRef(polygonRef))
            throw new ArgumentException("polygon 引用不属于当前网格.", nameof(polygonRef));
        int polygonIndex = polygonRef.Index;
        if (!filter.Pass(polygonIndex, GetPolygonAreaId(polygonIndex), GetPolygonFlags(polygonIndex))) return 0;
        ReadOnlySpan<int> vertices = m_Polygons[polygonIndex].Vertices;
        int written = 0;
        for (int index = 0; index < vertices.Length; index++)
        {
            int first = vertices[index];
            int second = vertices[(index + 1) % vertices.Length];
            if (HasTraversablePolygonNeighbor(polygonIndex, first, second, filter)) continue;
            if (written >= destination.Length) return written;
            destination[written++] = new NavMeshSegment(m_Vertices[first], m_Vertices[second]);
        }

        return written;
    }

    /// <summary>
    /// 查找指定点在给定半径内的最近边界墙.
    /// </summary>
    /// <param name="point">位于网格内的查询点.</param>
    /// <param name="maxDistance">允许返回的最大距离.</param>
    /// <param name="hit">成功时的最近墙信息.</param>
    /// <returns>存在不超过最大距离的边界墙时为 <see langword="true"/>.</returns>
    public bool TryFindDistanceToWall(NavMeshPoint point, double maxDistance, out NavMeshWallHit hit)
    {
        if (!point.IsFinite || !double.IsFinite(maxDistance) || maxDistance < 0)
            throw new ArgumentOutOfRangeException(nameof(maxDistance));
        double bestSquared = maxDistance * maxDistance;
        NavMeshPoint bestPoint = default;
        NavMeshPoint bestNormal = default;
        bool found = false;
        for (int triangleIndex = 0; triangleIndex < m_Triangles.Length; triangleIndex++)
        {
            NavMeshTriangle triangle = m_Triangles[triangleIndex];
            for (int edge = 0; edge < 3; edge++)
            {
                if (m_Neighbors[triangleIndex * 3 + edge] >= 0) continue;
                int startIndex = edge switch { 0 => triangle.First, 1 => triangle.Second, _ => triangle.Third };
                int endIndex = edge switch { 0 => triangle.Second, 1 => triangle.Third, _ => triangle.First };
                NavMeshPoint start = m_Vertices[startIndex];
                NavMeshPoint end = m_Vertices[endIndex];
                NavMeshPoint candidate = ClosestPointOnSegment(point, start, end);
                double squared = DistanceSquared(point, candidate);
                if (squared > bestSquared) continue;
                double x = end.X - start.X;
                double y = end.Y - start.Y;
                double length = Math.Sqrt(x * x + y * y);
                bestSquared = squared;
                bestPoint = candidate;
                bestNormal = new NavMeshPoint(y / length, -x / length);
                found = true;
            }
        }

        hit = new NavMeshWallHit(Math.Sqrt(bestSquared), bestPoint, bestNormal);
        return found;
    }

    /// <summary>
    /// 查找线段从起点到终点首次穿出导航网格的位置.
    /// </summary>
    /// <param name="start">线段的起点.</param>
    /// <param name="end">线段的终点.</param>
    /// <param name="hit">成功时的首次边界命中信息.</param>
    /// <returns>线段与网格边界相交时为 <see langword="true"/>.</returns>
    public bool TryRaycastBoundary(NavMeshPoint start, NavMeshPoint end, out NavMeshRaycastHit hit)
    {
        if (!start.IsFinite) throw new ArgumentException("起点必须为有限 double 坐标.", nameof(start));
        if (!end.IsFinite) throw new ArgumentException("终点必须为有限 double 坐标.", nameof(end));
        double bestT = double.PositiveInfinity;
        NavMeshPoint position = default;
        NavMeshPoint normal = default;
        double rayX = end.X - start.X;
        double rayY = end.Y - start.Y;
        for (int triangleIndex = 0; triangleIndex < m_Triangles.Length; triangleIndex++)
        {
            NavMeshTriangle triangle = m_Triangles[triangleIndex];
            for (int edge = 0; edge < 3; edge++)
            {
                if (m_Neighbors[triangleIndex * 3 + edge] >= 0) continue;
                int firstIndex = edge switch { 0 => triangle.First, 1 => triangle.Second, _ => triangle.Third };
                int secondIndex = edge switch { 0 => triangle.Second, 1 => triangle.Third, _ => triangle.First };
                NavMeshPoint first = m_Vertices[firstIndex];
                NavMeshPoint second = m_Vertices[secondIndex];
                double edgeX = second.X - first.X;
                double edgeY = second.Y - first.Y;
                double cross = rayX * edgeY - rayY * edgeX;
                if (Math.Abs(cross) < 1e-12) continue;
                double offsetX = first.X - start.X;
                double offsetY = first.Y - start.Y;
                double t = (offsetX * edgeY - offsetY * edgeX) / cross;
                double u = (offsetX * rayY - offsetY * rayX) / cross;
                if (t <= 0 || t > 1 || u < 0 || u > 1 || t >= bestT) continue;
                double edgeLength = Math.Sqrt(edgeX * edgeX + edgeY * edgeY);
                bestT = t;
                position = new NavMeshPoint(start.X + rayX * t, start.Y + rayY * t);
                normal = new NavMeshPoint(edgeY / edgeLength, -edgeX / edgeLength);
            }
        }

        hit = new NavMeshRaycastHit(bestT, position, normal);
        return bestT < double.PositiveInfinity;
    }

    /// <summary>
    /// 沿三角 corridor 执行二维射线查询.
    /// </summary>
    /// <param name="start">位于可通行三角形内的起点.</param>
    /// <param name="end">射线线段的终点.</param>
    /// <returns>包含是否抵达终点、命中信息和经过三角形的结果.</returns>
    public NavMeshRaycastResult Raycast(NavMeshPoint start, NavMeshPoint end)
    {
        return Raycast(start, end, NavMeshQueryDefaults.Filter);
    }

    /// <summary>
    /// 沿三角 corridor 执行受过滤器约束的二维射线查询.
    /// </summary>
    /// <param name="start">位于可通行三角形内的起点.</param>
    /// <param name="end">射线线段的终点.</param>
    /// <param name="filter">决定相邻三角形是否可穿越的过滤器.</param>
    /// <returns>包含是否抵达终点、命中信息和经过三角形的结果.</returns>
    public NavMeshRaycastResult Raycast(NavMeshPoint start, NavMeshPoint end, INavMeshQueryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!start.IsFinite) throw new ArgumentException("起点必须为有限 double 坐标.", nameof(start));
        if (!end.IsFinite) throw new ArgumentException("终点必须为有限 double 坐标.", nameof(end));
        int current = FindContainingTriangle(start);
        if (current < 0 || !filter.Pass(current, GetAreaId(current), GetFlags(current)))
            throw new ArgumentException("起点必须位于允许通行的三角形内.", nameof(start));

        List<int> corridor = Factory.RentList<int>();
        try
        {
            corridor.Add(current);
            double currentT = 0;
            while (currentT < 1)
            {
                if (!TryFindExitEdge(current, start, end, currentT, out int edge, out double t))
                    return new NavMeshRaycastResult(true, null, corridor.ToArray());
                if (t >= 1 - 1e-12) return new NavMeshRaycastResult(true, null, corridor.ToArray());

                int neighbor = m_Neighbors[current * 3 + edge];
                if (neighbor < 0 || !filter.Pass(neighbor, GetAreaId(neighbor), GetFlags(neighbor)))
                    return new NavMeshRaycastResult(false, CreateRaycastHit(current, edge, start, end, t),
                        corridor.ToArray());

                current = neighbor;
                corridor.Add(current);
                currentT = t;
            }

            return new NavMeshRaycastResult(true, null, corridor.ToArray());
        }
        finally
        {
            Factory.Release(corridor);
        }
    }

    /// <summary>
    /// 沿指定线段尝试在导航网格表面移动.
    /// </summary>
    /// <param name="start">位于网格内的移动起点.</param>
    /// <param name="end">期望抵达的终点.</param>
    /// <param name="position">成功时为终点, 失败时为首次边界命中点.</param>
    /// <returns>线段完全位于网格内时为 <see langword="true"/>.</returns>
    public bool TryMoveAlongSurface(NavMeshPoint start, NavMeshPoint end, out NavMeshPoint position)
    {
        if (!start.IsFinite) throw new ArgumentException("起点必须为有限 double 坐标.", nameof(start));
        if (!end.IsFinite) throw new ArgumentException("终点必须为有限 double 坐标.", nameof(end));
        if (FindContainingTriangle(start) < 0)
        {
            position = default;
            return false;
        }

        if (TryRaycastBoundary(start, end, out NavMeshRaycastHit hit))
        {
            position = hit.Position;
            return false;
        }

        position = end;
        return true;
    }

    /// <summary>
    /// 查找从指定位置可达且与查询圆相交的局部 polygon.
    /// 结果按累计移动成本从低到高排列. 该几何局部查询只沿共享地面 portal 扩展, 不穿越跳跃连接.
    /// </summary>
    /// <param name="start">当前网格返回的有效起始位置.</param>
    /// <param name="radius">查询圆的非负半径.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定 polygon 是否可通行的过滤器.</param>
    /// <param name="costPolicy">决定穿越相邻 polygon 时累计成本的策略.</param>
    /// <param name="destination">接收按累计成本排列的局部 polygon 结果.</param>
    /// <param name="resultCount">找到的完整结果数量. 该值大于 destination 长度时, destination 仅包含前部结果.</param>
    /// <returns>起点有效且 destination 足以容纳全部结果时为 <see langword="true"/>.</returns>
    public bool TryFindPolygonsAroundCircle(NavMeshLocation start, double radius, NavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, Span<NavMeshLocalPolygon> destination,
        out int resultCount)
    {
        return TraversePolygonsAroundCircle(start, radius, workspace, filter, costPolicy, destination, null,
            out resultCount, out _) && resultCount <= destination.Length;
    }

    /// <summary>
    /// 在起始位置可达且与查询圆相交的局部 polygon 集合中按面积均匀随机选取一个点.
    /// 该方法使用与 <see cref="TryFindPolygonsAroundCircle"/> 相同的 portal-circle 遍历规则,
    /// 但随机点本身不强制位于圆内.
    /// </summary>
    /// <param name="start">当前网格返回的有效起始位置.</param>
    /// <param name="radius">查询圆的非负半径.</param>
    /// <param name="random">提供随机 double 的随机源.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定 polygon 是否可通行的过滤器.</param>
    /// <param name="costPolicy">决定穿越相邻 polygon 时累计成本的策略.</param>
    /// <param name="location">成功时包含随机选中的可行走位置.</param>
    /// <returns>找到可达 polygon 时为 <see langword="true"/>.</returns>
    public bool TryFindRandomPointAroundCircle(NavMeshLocation start, double radius, Random random,
        NavMeshQueryWorkspace workspace, INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy,
        out NavMeshLocation location)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (!TraversePolygonsAroundCircle(start, radius, workspace, filter, costPolicy, default, random, out _,
                out int polygonIndex))
        {
            location = default;
            return false;
        }

        return TryFindRandomPoint(new NavMeshPolygonRef(m_Id, polygonIndex), random, out location);
    }

    private bool TraversePolygonsAroundCircle(NavMeshLocation start, double radius, NavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, Span<NavMeshLocalPolygon> destination,
        Random? random, out int resultCount, out int selectedPolygon)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        resultCount = 0;
        selectedPolygon = -1;
        if (!start.Position.IsFinite || !IsValidPolygonRef(start.PolygonRef) ||
            !ContainsPolygon(start.PolygonRef.Index, start.Position))
            return false;
        int startPolygon = start.PolygonRef.Index;
        if (!filter.Pass(startPolygon, GetPolygonAreaId(startPolygon), GetPolygonFlags(startPolygon))) return false;

        workspace.Reset(m_Polygons.Length);
        workspace.SetOpen(startPolygon, 0, -1);
        workspace.Positions[startPolygon] = start.Position;
        workspace.Open.Enqueue(startPolygon, 0);
        double radiusSquared = radius * radius;
        double accumulatedArea = 0;
        while (workspace.Open.TryDequeue(out int current, out _))
        {
            if (!workspace.TryClose(current)) continue;
            int parent = workspace.Parents[current];
            if (resultCount < destination.Length)
            {
                destination[resultCount] = new NavMeshLocalPolygon(new NavMeshPolygonRef(m_Id, current),
                    parent < 0 ? NavMeshPolygonRef.Invalid : new NavMeshPolygonRef(m_Id, parent),
                    workspace.GetCost(current));
            }

            resultCount++;
            if (random is not null)
            {
                double area = PolygonArea(current);
                accumulatedArea += area;
                if (random.NextDouble() * accumulatedArea <= area) selectedPolygon = current;
            }

            foreach (PolygonNeighbor neighbor in m_PolygonNeighbors[current])
            {
                int target = neighbor.TargetPolygon;
                if (workspace.IsClosed(target) || !filter.Pass(target, GetPolygonAreaId(target), GetPolygonFlags(target)))
                    continue;
                NavMeshPoint portalStart = m_Vertices[neighbor.FirstVertex];
                NavMeshPoint portalEnd = m_Vertices[neighbor.SecondVertex];
                if (DistanceSquared(start.Position, ClosestPointOnSegment(start.Position, portalStart, portalEnd)) >
                    radiusSquared)
                    continue;
                NavMeshPoint targetPosition = new NavMeshPoint((portalStart.X + portalEnd.X) * 0.5,
                    (portalStart.Y + portalEnd.Y) * 0.5);
                double multiplier = GetTraversalMultiplier(GetPolygonAreaId(current), GetPolygonAreaId(target),
                    costPolicy);
                double candidateCost = workspace.GetCost(current) +
                                       NavMeshPoint.Distance(workspace.Positions[current], targetPosition) * multiplier;
                if (candidateCost >= workspace.GetCost(target)) continue;
                workspace.SetOpen(target, candidateCost, current);
                workspace.Positions[target] = targetPosition;
                workspace.Open.Enqueue(target, candidateCost);
            }
        }

        return true;
    }

    /// <summary>
    /// 从逆时针三角形拓扑创建不可变导航网格.
    /// </summary>
    /// <param name="vertices">供三角形索引引用的双精度顶点.</param>
    /// <param name="triangles">逆时针且面积大于零的三角形.</param>
    /// <returns>已建立相邻关系的导航网格.</returns>
    public static NavMesh Create(ReadOnlySpan<NavMeshPoint> vertices, ReadOnlySpan<NavMeshTriangle> triangles)
    {
        return CreateCore(vertices, triangles, [], default, default);
    }

    /// <summary>
    /// 从逆时针凸多边形定义创建不可变导航网格.
    /// 每个凸多边形会同时保留为一个 A* 查询节点, 并在几何细节查询中扇形拆分为三角形.
    /// </summary>
    /// <param name="vertices">供多边形顶点索引引用的双精度顶点.</param>
    /// <param name="polygons">逆时针、凸且面积大于零的多边形.</param>
    /// <param name="jumpConnections">可选的地面外跳跃连接.</param>
    /// <returns>已建立相邻关系和跳跃连接的导航网格.</returns>
    public static NavMesh Create(ReadOnlySpan<NavMeshPoint> vertices, ReadOnlySpan<NavMeshConvexPolygon> polygons,
        ReadOnlySpan<NavMeshJumpConnection> jumpConnections = default)
    {
        if (polygons.IsEmpty) throw new ArgumentException("至少需要一个凸多边形.", nameof(polygons));
        List<NavMeshTriangle> triangles = Factory.RentList<NavMeshTriangle>();
        List<int> trianglePolygons = Factory.RentList<int>();
        try
        {
            for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++)
            {
                NavMeshConvexPolygon polygon = polygons[polygonIndex] ??
                    throw new ArgumentException($"多边形 {polygonIndex} 不能为 null.", nameof(polygons));
                ReadOnlySpan<int> indices = polygon.AsSpan();
                ValidateConvexPolygon(indices, vertices, polygonIndex);
                for (int index = 1; index < indices.Length - 1; index++)
                {
                    triangles.Add(new NavMeshTriangle(indices[0], indices[index], indices[index + 1], polygon.AreaId,
                        polygon.Flags));
                    trianglePolygons.Add(polygonIndex);
                }
            }

            return CreateCore(vertices, CollectionsMarshal.AsSpan(triangles), jumpConnections,
                polygons, CollectionsMarshal.AsSpan(trianglePolygons));
        }
        finally
        {
            Factory.Release(trianglePolygons);
            Factory.Release(triangles);
        }
    }

    private static NavMesh CreateCore(ReadOnlySpan<NavMeshPoint> vertices, ReadOnlySpan<NavMeshTriangle> triangles,
        ReadOnlySpan<NavMeshJumpConnection> jumpConnections, ReadOnlySpan<NavMeshConvexPolygon> sourcePolygons,
        ReadOnlySpan<int> sourceTrianglePolygons)
    {
        if (vertices.IsEmpty) throw new ArgumentException("至少需要一个顶点.", nameof(vertices));
        if (triangles.IsEmpty) throw new ArgumentException("至少需要一个三角形.", nameof(triangles));

        NavMeshPoint[] copiedVertices = vertices.ToArray();
        NavMeshTriangle[] copiedTriangles = triangles.ToArray();
        Dictionary<Edge, int> edgeOwners = new Dictionary<Edge, int>(copiedTriangles.Length * 3);
        HashSet<Edge> pairedEdges = new HashSet<Edge>();
        int[] neighbors = new int[copiedTriangles.Length * 3];
        Array.Fill(neighbors, -1);

        for (int vertexIndex = 0; vertexIndex < copiedVertices.Length; vertexIndex++)
        {
            if (!copiedVertices[vertexIndex].IsFinite)
                throw new ArgumentException("顶点必须为有限 double 坐标.", nameof(vertices));
        }

        for (int triangleIndex = 0; triangleIndex < copiedTriangles.Length; triangleIndex++)
        {
            NavMeshTriangle triangle = copiedTriangles[triangleIndex];
            ValidateTriangle(triangle, copiedVertices, triangleIndex);
            AddEdge(triangle.First, triangle.Second, triangleIndex * 3, edgeOwners, pairedEdges, neighbors);
            AddEdge(triangle.Second, triangle.Third, triangleIndex * 3 + 1, edgeOwners, pairedEdges, neighbors);
            AddEdge(triangle.Third, triangle.First, triangleIndex * 3 + 2, edgeOwners, pairedEdges, neighbors);
        }

        NavMeshPoint[] centers = new NavMeshPoint[copiedTriangles.Length];
        for (int index = 0; index < centers.Length; index++)
            centers[index] = GetTriangleCenter(copiedTriangles[index], copiedVertices);
        int[] bvhTriangles = new int[copiedTriangles.Length];
        for (int index = 0; index < bvhTriangles.Length; index++) bvhTriangles[index] = index;
        List<BvhNode> bvhNodes = new List<BvhNode>();
        BuildBvh(bvhTriangles, 0, bvhTriangles.Length, copiedVertices, copiedTriangles, bvhNodes);
        PolygonInfo[] polygons = BuildPolygons(copiedTriangles, copiedVertices, sourcePolygons);
        int[] trianglePolygons = sourceTrianglePolygons.IsEmpty
            ? CreateTrianglePolygons(copiedTriangles.Length)
            : sourceTrianglePolygons.ToArray();
        PolygonNeighbor[][] polygonNeighbors = BuildPolygonNeighbors(polygons);
        int[] bvhPolygons = new int[polygons.Length];
        for (int index = 0; index < bvhPolygons.Length; index++) bvhPolygons[index] = index;
        List<BvhNode> polygonBvhNodes = new List<BvhNode>();
        BuildPolygonBvh(bvhPolygons, 0, bvhPolygons.Length, polygons, polygonBvhNodes);
        NavMeshJumpConnection[] copiedJumpConnections = jumpConnections.ToArray();
        HeuristicJump[] heuristicJumps = BuildHeuristicJumps(copiedJumpConnections);
        double[] heuristicJumpTransitionDistances = BuildHeuristicJumpTransitionDistances(heuristicJumps);
        JumpEdge[][] jumpEdges = BuildJumpEdges(copiedJumpConnections, copiedTriangles, copiedVertices, trianglePolygons,
            polygons.Length);
        return new NavMesh(copiedVertices, copiedTriangles, centers, neighbors, polygons, trianglePolygons,
            polygonNeighbors, jumpEdges, copiedJumpConnections, heuristicJumps, heuristicJumpTransitionDistances,
            bvhNodes.ToArray(), bvhTriangles, polygonBvhNodes.ToArray(), bvhPolygons);
    }

    /// <summary>
    /// 在网格内查找从起点到终点的最短二维路径.
    /// </summary>
    /// <param name="start">位于网格内的起点.</param>
    /// <param name="goal">位于网格内的终点.</param>
    /// <returns>不可达或任一端点不在网格内时返回 <see langword="null"/>.</returns>
    public NavMeshPath? FindPath(NavMeshPoint start, NavMeshPoint goal)
        => FindPath(start, goal, new NavMeshQueryWorkspace(), NavMeshQueryDefaults.Filter,
            NavMeshQueryDefaults.CostPolicy);

    /// <summary>
    /// 使用调用方提供的工作区在网格内查找二维路径.
    /// </summary>
    /// <param name="start">位于网格内的起点.</param>
    /// <param name="goal">位于网格内的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <returns>不可达或任一端点不在网格内时返回 <see langword="null"/>.</returns>
    public NavMeshPath? FindPath(NavMeshPoint start, NavMeshPoint goal, NavMeshQueryWorkspace workspace)
        => FindPath(start, goal, workspace, NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy);

    /// <summary>
    /// 使用区域过滤器和移动倍率策略查找二维路径.
    /// </summary>
    /// <param name="start">位于网格内的起点.</param>
    /// <param name="goal">位于网格内的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定三角形是否可通行的过滤器.</param>
    /// <param name="costPolicy">决定跨越区域移动成本的策略.</param>
    /// <param name="options">可选的性能与最优性控制参数. 查询开始时会捕获其当前值.</param>
    /// <returns>不可达或任一端点不在网格内时返回 <see langword="null"/>.</returns>
    public NavMeshPath? FindPath(NavMeshPoint start, NavMeshPoint goal, NavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, NavMeshQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        double heuristicWeight = options?.HeuristicWeight ?? 1;
        if (!start.IsFinite || !goal.IsFinite) throw new ArgumentException("起点和终点必须为有限 double 坐标.");
        int startPolygon = FindContainingPolygon(start);
        int goalPolygon = FindContainingPolygon(goal);
        if (startPolygon < 0 || goalPolygon < 0) return null;
        return FindPathCore(start, startPolygon, goal, goalPolygon, workspace, filter, costPolicy, heuristicWeight);
    }

    /// <summary>
    /// 使用已缓存的起点 polygon 引用查找二维路径.
    /// 该重载跳过起点坐标定位, 适合移动对象的连续重规划.
    /// </summary>
    /// <param name="start">包含当前网格返回的有效 polygon 引用的起点位置.</param>
    /// <param name="goal">位于网格内的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定凸多边形是否可通行的过滤器.</param>
    /// <param name="costPolicy">决定跨越区域移动成本的策略.</param>
    /// <param name="options">可选的性能与最优性控制参数. 查询开始时会捕获其当前值.</param>
    /// <returns>引用无效、端点不可达或任一端点不在允许区域时返回 <see langword="null"/>.</returns>
    public NavMeshPath? FindPath(NavMeshLocation start, NavMeshPoint goal, NavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, NavMeshQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        double heuristicWeight = options?.HeuristicWeight ?? 1;
        if (!start.Position.IsFinite || !goal.IsFinite) throw new ArgumentException("起点和终点必须为有限 double 坐标.");
        int startPolygon = start.PolygonRef.Index;
        if (!IsValidPolygonRef(start.PolygonRef) || !ContainsPolygon(startPolygon, start.Position)) return null;
        int goalPolygon = FindContainingPolygon(goal);
        return goalPolygon < 0
            ? null
            : FindPathCore(start.Position, startPolygon, goal, goalPolygon, workspace, filter, costPolicy,
                heuristicWeight);
    }

    /// <summary>
    /// 使用已缓存的起点 polygon 引用和默认查询规则查找二维路径.
    /// </summary>
    /// <param name="start">包含当前网格返回的有效 polygon 引用的起点位置.</param>
    /// <param name="goal">位于网格内的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <returns>引用无效、端点不可达或任一端点不在网格内时返回 <see langword="null"/>.</returns>
    public NavMeshPath? FindPath(NavMeshLocation start, NavMeshPoint goal, NavMeshQueryWorkspace workspace)
    {
        return FindPath(start, goal, workspace, NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy);
    }

    /// <summary>
    /// 使用已缓存的起点和终点位置查找二维路径.
    /// 该重载不执行坐标定位, 适合持续跟踪多个移动对象时复用上一帧的定位结果.
    /// </summary>
    /// <param name="start">包含当前网格返回的有效 polygon 引用的起点位置.</param>
    /// <param name="goal">包含当前网格返回的有效 polygon 引用的终点位置.</param>
    /// <returns>引用无效或端点不可达时返回 <see langword="null"/>.</returns>
    public NavMeshPath? FindPath(NavMeshLocation start, NavMeshLocation goal)
    {
        return FindPath(start, goal, new NavMeshQueryWorkspace(), NavMeshQueryDefaults.Filter,
            NavMeshQueryDefaults.CostPolicy);
    }

    /// <summary>
    /// 使用已缓存的起点和终点位置及调用方工作区查找二维路径.
    /// 该重载跳过两端的坐标定位, 并在引用属于其他网格实例时安全返回 <see langword="null"/>.
    /// </summary>
    /// <param name="start">包含当前网格返回的有效 polygon 引用的起点位置.</param>
    /// <param name="goal">包含当前网格返回的有效 polygon 引用的终点位置.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定凸多边形是否可通行的过滤器.</param>
    /// <param name="costPolicy">决定跨越区域移动成本的策略.</param>
    /// <param name="options">可选的性能与最优性控制参数. 查询开始时会捕获其当前值.</param>
    /// <returns>引用无效、端点不可达或任一端点不在允许区域时返回 <see langword="null"/>.</returns>
    public NavMeshPath? FindPath(NavMeshLocation start, NavMeshLocation goal, NavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, NavMeshQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        double heuristicWeight = options?.HeuristicWeight ?? 1;
        if (!start.Position.IsFinite || !goal.Position.IsFinite)
            throw new ArgumentException("起点和终点必须为有限 double 坐标.");
        int startPolygon = start.PolygonRef.Index;
        int goalPolygon = goal.PolygonRef.Index;
        if (!IsValidPolygonRef(start.PolygonRef) || !IsValidPolygonRef(goal.PolygonRef) ||
            !ContainsPolygon(startPolygon, start.Position) || !ContainsPolygon(goalPolygon, goal.Position))
            return null;
        return FindPathCore(start.Position, startPolygon, goal.Position, goalPolygon, workspace, filter, costPolicy,
            heuristicWeight);
    }

    private NavMeshPath? FindPathCore(NavMeshPoint start, int startPolygon, NavMeshPoint goal, int goalPolygon,
        NavMeshQueryWorkspace workspace, INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy,
        double heuristicWeight)
    {
        if (!filter.Pass(startPolygon, GetPolygonAreaId(startPolygon), GetPolygonFlags(startPolygon)) ||
            !filter.Pass(goalPolygon, GetPolygonAreaId(goalPolygon), GetPolygonFlags(goalPolygon)))
            return null;

        List<int> corridor = Factory.RentList<int>();
        try
        {
            if (!TryFindCorridor(startPolygon, goalPolygon, goal, corridor, workspace, filter, costPolicy,
                    heuristicWeight, out _, out double searchCost))
                return null;
            int[] corridorJumps = new int[corridor.Count];
            for (int index = 1; index < corridor.Count; index++)
                corridorJumps[index] = workspace.ParentJumps[corridor[index]];
            NavMeshJumpTraversal[] jumps;
            NavMeshPoint[] points = BuildStraightPath(start, goal, corridor, corridorJumps, out jumps);
            return new NavMeshPath(points, corridor.ToArray(), jumps, searchCost,
                CalculatePathCost(points, jumps, costPolicy), heuristicWeight);
        }
        finally
        {
            Factory.Release(corridor);
        }
    }

    /// <summary>
    /// 在不构造 funnel 航点和托管结果数组的情况下查找凸多边形 corridor.
    /// 当 <paramref name="destination"/> 容量不足时返回 <see langword="false"/>, 并通过
    /// <paramref name="corridorCount"/> 返回所需的完整长度; 不存在路径时该长度为 0.
    /// </summary>
    /// <param name="start">位于网格内的起点.</param>
    /// <param name="goal">位于网格内的终点.</param>
    /// <param name="workspace">不得被并发使用的可复用查询工作区.</param>
    /// <param name="filter">决定凸多边形是否可通行的过滤器.</param>
    /// <param name="costPolicy">决定跨越区域移动成本的策略.</param>
    /// <param name="destination">接收按行进顺序排列的凸多边形索引.</param>
    /// <param name="corridorCount">成功时为写入数量; 缓冲区不足时为所需数量; 无路径时为 0.</param>
    /// <param name="totalCost">成功时为 A* 在凸多边形中心图上的累计区域加权移动代价.</param>
    /// <param name="options">可选的性能与最优性控制参数. 查询开始时会捕获其当前值.</param>
    /// <returns>找到完整 corridor 且 destination 容量充足时为 <see langword="true"/>.</returns>
    public bool TryFindCorridor(NavMeshPoint start, NavMeshPoint goal, NavMeshQueryWorkspace workspace,
        INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy, Span<int> destination,
        out int corridorCount, out double totalCost, NavMeshQueryOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(costPolicy);
        if (!double.IsFinite(costPolicy.MinimumMultiplier) || costPolicy.MinimumMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(costPolicy));
        double heuristicWeight = options?.HeuristicWeight ?? 1;
        if (!start.IsFinite || !goal.IsFinite) throw new ArgumentException("起点和终点必须为有限 double 坐标.");
        int startPolygon = FindContainingPolygon(start);
        int goalPolygon = FindContainingPolygon(goal);
        if (startPolygon < 0 || goalPolygon < 0)
        {
            corridorCount = 0;
            totalCost = default;
            return false;
        }
        if (!filter.Pass(startPolygon, GetPolygonAreaId(startPolygon), GetPolygonFlags(startPolygon)) ||
            !filter.Pass(goalPolygon, GetPolygonAreaId(goalPolygon), GetPolygonFlags(goalPolygon)))
        {
            corridorCount = 0;
            totalCost = default;
            return false;
        }

        if (!TryFindCorridor(startPolygon, goalPolygon, goal, null, workspace, filter, costPolicy, heuristicWeight,
                out int lastTriangle, out totalCost))
        {
            corridorCount = 0;
            return false;
        }

        corridorCount = GetCorridorCount(lastTriangle, workspace);
        if (corridorCount > destination.Length) return false;
        for (int triangleIndex = lastTriangle, writeIndex = corridorCount;
             triangleIndex >= 0;
             triangleIndex = workspace.Parents[triangleIndex])
            destination[--writeIndex] = triangleIndex;
        return true;
    }

    private bool TryFindCorridor(int startTriangle, int goalTriangle, NavMeshPoint goal, List<int>? corridor,
        NavMeshQueryWorkspace workspace, INavMeshQueryFilter filter, INavMeshTraversalCostPolicy costPolicy,
        double heuristicWeight, out int lastTriangle, out double totalCost)
    {
        int count = m_Polygons.Length;
        workspace.Reset(count);
        bool usesDefaultFilter = ReferenceEquals(filter, NavMeshQueryDefaults.Filter);
        bool usesDefaultCostPolicy = ReferenceEquals(costPolicy, NavMeshQueryDefaults.CostPolicy);
        double minimumMultiplier = costPolicy.MinimumMultiplier;
        int heuristicJumpCount = PrepareJumpHeuristic(goal, minimumMultiplier, workspace);
        workspace.SetOpen(startTriangle, 0, -1);
        double startPriority = heuristicJumpCount == 0
            ? NavMeshPoint.Distance(PolygonCenter(startTriangle), goal) * minimumMultiplier * heuristicWeight
            : CalculateHeuristic(startTriangle, goal, minimumMultiplier, heuristicWeight, heuristicJumpCount,
                workspace);
        workspace.Open.Enqueue(startTriangle, startPriority);
        while (workspace.Open.TryDequeue(out int current, out _))
        {
            if (!workspace.TryClose(current)) continue;
            if (current == goalTriangle)
            {
                if (corridor is not null)
                {
                    for (int node = current; node >= 0; node = workspace.Parents[node]) corridor.Add(node);
                    corridor.Reverse();
                }

                lastTriangle = current;
                totalCost = workspace.GetCost(current);
                return true;
            }

            PolygonInfo currentPolygon = m_Polygons[current];
            int currentAreaId = currentPolygon.AreaId;
            double currentCost = workspace.GetCost(current);
            foreach (PolygonNeighbor polygonNeighbor in m_PolygonNeighbors[current])
            {
                int neighbor = polygonNeighbor.TargetPolygon;
                PolygonInfo neighborPolygon = m_Polygons[neighbor];
                if (!usesDefaultFilter && !filter.Pass(neighbor, neighborPolygon.AreaId, neighborPolygon.Flags))
                    continue;
                double multiplier = usesDefaultCostPolicy
                    ? 1
                    : GetTraversalMultiplier(currentAreaId, neighborPolygon.AreaId, costPolicy);
                double candidate = currentCost + polygonNeighbor.CenterDistance * multiplier;
                if (candidate >= workspace.GetCost(neighbor)) continue;
                workspace.SetOpen(neighbor, candidate, current);
                double estimatedRemainingCost = heuristicJumpCount == 0
                    ? NavMeshPoint.Distance(PolygonCenter(neighbor), goal) * minimumMultiplier * heuristicWeight
                    : CalculateHeuristic(neighbor, goal, minimumMultiplier, heuristicWeight, heuristicJumpCount,
                        workspace);
                workspace.Open.Enqueue(neighbor, candidate + estimatedRemainingCost);
            }

            foreach (JumpEdge jump in m_JumpEdges[current])
            {
                int neighbor = jump.TargetTriangle;
                PolygonInfo neighborPolygon = m_Polygons[neighbor];
                if (!usesDefaultFilter && !filter.Pass(neighbor, neighborPolygon.AreaId, neighborPolygon.Flags))
                    continue;
                double leaveMultiplier = usesDefaultCostPolicy
                    ? 1
                    : GetTraversalMultiplier(currentAreaId, currentAreaId, costPolicy);
                double enterMultiplier = usesDefaultCostPolicy
                    ? 1
                    : GetTraversalMultiplier(neighborPolygon.AreaId, neighborPolygon.AreaId, costPolicy);
                double candidate = currentCost +
                                   NavMeshPoint.Distance(PolygonCenter(current), jump.Start) * leaveMultiplier +
                                   jump.FixedCost + NavMeshPoint.Distance(jump.End, PolygonCenter(neighbor)) * enterMultiplier;
                if (candidate >= workspace.GetCost(neighbor)) continue;
                workspace.SetOpen(neighbor, candidate, current, jump.ParentMarker);
                double estimatedRemainingCost = heuristicJumpCount == 0
                    ? NavMeshPoint.Distance(PolygonCenter(neighbor), goal) * minimumMultiplier * heuristicWeight
                    : CalculateHeuristic(neighbor, goal, minimumMultiplier, heuristicWeight, heuristicJumpCount,
                        workspace);
                workspace.Open.Enqueue(neighbor, candidate + estimatedRemainingCost);
            }
        }

        lastTriangle = -1;
        totalCost = default;
        return false;
    }

    private static int GetCorridorCount(int lastTriangle, NavMeshQueryWorkspace workspace)
    {
        int count = 0;
        for (int triangleIndex = lastTriangle; triangleIndex >= 0; triangleIndex = workspace.Parents[triangleIndex])
            count++;
        return count;
    }

    private NavMeshPoint[] BuildStraightPath(NavMeshPoint start, NavMeshPoint goal, List<int> corridor,
        ReadOnlySpan<int> corridorJumps, out NavMeshJumpTraversal[] jumps)
    {
        if (m_JumpConnections.Length == 0) return BuildStraightPath(start, goal, corridor, out jumps);
        List<NavMeshPoint> points = Factory.RentList<NavMeshPoint>();
        List<NavMeshJumpTraversal> traversals = Factory.RentList<NavMeshJumpTraversal>();
        try
        {
            int segmentStart = 0;
            NavMeshPoint segmentStartPoint = start;
            for (int index = 1; index < corridor.Count; index++)
            {
                int marker = corridorJumps[index];
                if (marker == 0) continue;
                NavMeshJumpConnection connection = m_JumpConnections[Math.Abs(marker) - 1];
                bool forward = marker > 0;
                NavMeshPoint jumpStart = forward ? connection.Start : connection.End;
                NavMeshPoint jumpEnd = forward ? connection.End : connection.Start;
                AppendGroundPath(points, segmentStartPoint, jumpStart, corridor, segmentStart, index - 1);
                if (points.Count == 0 || points[^1] != jumpEnd) points.Add(jumpEnd);
                traversals.Add(new NavMeshJumpTraversal(Math.Abs(marker) - 1, jumpStart, jumpEnd, connection.FixedCost));
                segmentStart = index;
                segmentStartPoint = jumpEnd;
            }

            AppendGroundPath(points, segmentStartPoint, goal, corridor, segmentStart, corridor.Count - 1);
            jumps = traversals.ToArray();
            return points.ToArray();
        }
        finally
        {
            Factory.Release(traversals);
            Factory.Release(points);
        }
    }

    private void AppendGroundPath(List<NavMeshPoint> destination, NavMeshPoint start, NavMeshPoint goal,
        List<int> corridor, int first, int last)
    {
        List<int> section = Factory.RentList<int>();
        try
        {
            for (int index = first; index <= last; index++) section.Add(corridor[index]);
            NavMeshPoint[] sectionPoints = BuildStraightPath(start, goal, section, out _);
            int skip = destination.Count == 0 ? 0 : 1;
            for (int index = skip; index < sectionPoints.Length; index++) destination.Add(sectionPoints[index]);
        }
        finally
        {
            Factory.Release(section);
        }
    }

    private NavMeshPoint[] BuildStraightPath(NavMeshPoint start, NavMeshPoint goal, List<int> corridor,
        out NavMeshJumpTraversal[] jumps)
    {
        jumps = [];
        List<Portal> portals = Factory.RentList<Portal>();
        try
        {
            for (int index = 0; index < corridor.Count - 1; index++)
                portals.Add(GetPolygonPortal(corridor[index], corridor[index + 1]));
            portals.Add(new Portal(goal, goal));
            List<NavMeshPoint> result = Factory.RentList<NavMeshPoint>();
            try
            {
                result.Add(start);
                NavMeshPoint apex = start;
                NavMeshPoint left = portals[0].Left;
                NavMeshPoint right = portals[0].Right;
                int apexIndex;
                int leftIndex = 0;
                int rightIndex = 0;
                for (int index = 1; index < portals.Count; index++)
                {
                    Portal portal = portals[index];
                    if (NavMeshPoint.Cross(apex, right, portal.Right) <= 0)
                    {
                        if (apex == right || NavMeshPoint.Cross(apex, left, portal.Right) > 0)
                        {
                            right = portal.Right;
                            rightIndex = index;
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
                            index = apexIndex;
                            continue;
                        }
                    }

                    if (NavMeshPoint.Cross(apex, left, portal.Left) >= 0)
                    {
                        if (apex == left || NavMeshPoint.Cross(apex, right, portal.Left) < 0)
                        {
                            left = portal.Left;
                            leftIndex = index;
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
                            index = apexIndex;
                        }
                    }
                }

                if (result[^1] != goal) result.Add(goal);
                return result.ToArray();
            }
            finally
            {
                Factory.Release(result);
            }
        }
        finally
        {
            Factory.Release(portals);
        }
    }

    private double CalculatePathCost(ReadOnlySpan<NavMeshPoint> points, ReadOnlySpan<NavMeshJumpTraversal> jumps,
        INavMeshTraversalCostPolicy costPolicy)
    {
        double totalCost = 0;
        int jumpIndex = 0;
        for (int index = 1; index < points.Length; index++)
        {
            NavMeshPoint start = points[index - 1];
            NavMeshPoint end = points[index];
            if (jumpIndex < jumps.Length && start == jumps[jumpIndex].Start && end == jumps[jumpIndex].End)
            {
                totalCost += jumps[jumpIndex++].FixedCost;
                continue;
            }
            double segmentLength = NavMeshPoint.Distance(start, end);
            if (segmentLength == 0) continue;

            // 航点可能恰好落在 portal 顶点上, 以微小的前向采样确定该段实际进入的三角形.
            double probeT = Math.Min(0.5, 1e-10 / segmentLength);
            NavMeshPoint probe = new NavMeshPoint(start.X + (end.X - start.X) * probeT,
                start.Y + (end.Y - start.Y) * probeT);
            int triangleIndex = FindContainingTriangle(probe);
            if (triangleIndex < 0) throw new InvalidOperationException("funnel 路径离开了导航网格.");
            totalCost += CalculateSegmentCost(start, end, segmentLength, triangleIndex, costPolicy);
        }

        return totalCost;
    }

    private double CalculateSegmentCost(NavMeshPoint start, NavMeshPoint end, double segmentLength, int triangleIndex,
        INavMeshTraversalCostPolicy costPolicy)
    {
        double totalCost = 0;
        double currentT = 0;
        while (true)
        {
            if (ContainsPoint(triangleIndex, end))
            {
                totalCost += segmentLength * (1 - currentT) *
                             GetTraversalMultiplier(GetAreaId(triangleIndex), GetAreaId(triangleIndex), costPolicy);
                return totalCost;
            }

            if (!TryFindExitEdge(triangleIndex, start, end, currentT, out _, out double exitT))
                throw new InvalidOperationException("funnel 路径无法穿越当前三角形.");
            totalCost += segmentLength * (exitT - currentT) *
                         GetTraversalMultiplier(GetAreaId(triangleIndex), GetAreaId(triangleIndex), costPolicy);
            double nextProbeT = Math.Min(1, exitT + 1e-10 / segmentLength);
            NavMeshPoint nextProbe = new NavMeshPoint(start.X + (end.X - start.X) * nextProbeT,
                start.Y + (end.Y - start.Y) * nextProbeT);
            triangleIndex = FindContainingTriangle(nextProbe);
            if (triangleIndex < 0) throw new InvalidOperationException("funnel 路径穿过了导航网格边界.");
            currentT = exitT;
        }
    }

    private bool ContainsPoint(int triangleIndex, NavMeshPoint point)
    {
        NavMeshTriangle triangle = m_Triangles[triangleIndex];
        return NavMeshPoint.Cross(m_Vertices[triangle.First], m_Vertices[triangle.Second], point) >= -1e-12 &&
               NavMeshPoint.Cross(m_Vertices[triangle.Second], m_Vertices[triangle.Third], point) >= -1e-12 &&
               NavMeshPoint.Cross(m_Vertices[triangle.Third], m_Vertices[triangle.First], point) >= -1e-12;
    }

    private static double GetTraversalMultiplier(int fromAreaId, int toAreaId, INavMeshTraversalCostPolicy costPolicy)
    {
        double multiplier = costPolicy.GetMultiplier(fromAreaId, toAreaId);
        if (!double.IsFinite(multiplier) || multiplier < costPolicy.MinimumMultiplier)
            throw new InvalidOperationException("移动倍率必须为不小于 MinimumMultiplier 的有限正数.");
        return multiplier;
    }

    private bool TryFindExitEdge(int triangleIndex, NavMeshPoint start, NavMeshPoint end, double currentT,
        out int exitEdge, out double exitT)
    {
        exitEdge = -1;
        exitT = double.PositiveInfinity;
        NavMeshTriangle triangle = m_Triangles[triangleIndex];
        for (int edge = 0; edge < 3; edge++)
        {
            int firstIndex = edge switch { 0 => triangle.First, 1 => triangle.Second, _ => triangle.Third };
            int secondIndex = edge switch { 0 => triangle.Second, 1 => triangle.Third, _ => triangle.First };
            if (!TryIntersectSegment(start, end, m_Vertices[firstIndex], m_Vertices[secondIndex], out double t))
                continue;
            if (t <= currentT + 1e-12 || t >= exitT) continue;
            exitEdge = edge;
            exitT = t;
        }

        return exitEdge >= 0;
    }

    private NavMeshRaycastHit CreateRaycastHit(int triangleIndex, int edge, NavMeshPoint start, NavMeshPoint end,
        double t)
    {
        NavMeshTriangle triangle = m_Triangles[triangleIndex];
        int firstIndex = edge switch { 0 => triangle.First, 1 => triangle.Second, _ => triangle.Third };
        int secondIndex = edge switch { 0 => triangle.Second, 1 => triangle.Third, _ => triangle.First };
        NavMeshPoint first = m_Vertices[firstIndex];
        NavMeshPoint second = m_Vertices[secondIndex];
        double edgeX = second.X - first.X;
        double edgeY = second.Y - first.Y;
        double inverseLength = 1 / Math.Sqrt(edgeX * edgeX + edgeY * edgeY);
        return new NavMeshRaycastHit(t, new NavMeshPoint(start.X + (end.X - start.X) * t,
            start.Y + (end.Y - start.Y) * t), new NavMeshPoint(edgeY * inverseLength, -edgeX * inverseLength));
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
        return t >= 0 && t <= 1 && u >= 0 && u <= 1;
    }

    private Portal GetPolygonPortal(int current, int next)
    {
        foreach (PolygonNeighbor neighbor in m_PolygonNeighbors[current])
        {
            if (neighbor.TargetPolygon != next) continue;
            NavMeshPoint first = m_Vertices[neighbor.FirstVertex];
            NavMeshPoint second = m_Vertices[neighbor.SecondVertex];
            return NavMeshPoint.Cross(PolygonCenter(current), PolygonCenter(next), first) >= 0
                ? new Portal(second, first)
                : new Portal(first, second);
        }

        throw new InvalidOperationException("相邻多边形未共享一条边.");
    }

    private int FindContainingTriangle(NavMeshPoint point)
    {
        Span<int> pendingNodes = stackalloc int[64];
        int pendingCount = 1;
        pendingNodes[0] = 0;
        while (pendingCount > 0)
        {
            BvhNode node = m_BvhNodes[pendingNodes[--pendingCount]];
            if (point.X < node.MinX || point.X > node.MaxX || point.Y < node.MinY || point.Y > node.MaxY) continue;
            if (node.Count > 0)
            {
                for (int offset = 0; offset < node.Count; offset++)
                {
                    int triangleIndex = m_BvhTriangles[node.Start + offset];
                    if (ContainsPoint(triangleIndex, point)) return triangleIndex;
                }

                continue;
            }

            pendingNodes[pendingCount++] = node.Left;
            pendingNodes[pendingCount++] = node.Right;
        }

        return -1;
    }

    private int FindContainingPolygon(NavMeshPoint point)
    {
        Span<int> pendingNodes = stackalloc int[64];
        int pendingCount = 1;
        pendingNodes[0] = 0;
        while (pendingCount > 0)
        {
            BvhNode node = m_PolygonBvhNodes[pendingNodes[--pendingCount]];
            if (point.X < node.MinX || point.X > node.MaxX || point.Y < node.MinY || point.Y > node.MaxY) continue;
            if (node.Count > 0)
            {
                for (int offset = 0; offset < node.Count; offset++)
                {
                    int polygonIndex = m_BvhPolygons[node.Start + offset];
                    if (ContainsPolygon(polygonIndex, point)) return polygonIndex;
                }

                continue;
            }

            pendingNodes[pendingCount++] = node.Left;
            pendingNodes[pendingCount++] = node.Right;
        }

        return -1;
    }

    private bool ContainsPolygon(int polygonIndex, NavMeshPoint point)
    {
        ReadOnlySpan<int> vertices = m_Polygons[polygonIndex].Vertices;
        for (int index = 0; index < vertices.Length; index++)
        {
            NavMeshPoint first = m_Vertices[vertices[index]];
            NavMeshPoint second = m_Vertices[vertices[(index + 1) % vertices.Length]];
            if (NavMeshPoint.Cross(first, second, point) < -1e-12) return false;
        }

        return true;
    }

    private bool TriangleOverlapsBounds(int triangleIndex, NavMeshBounds bounds)
    {
        NavMeshTriangle triangle = m_Triangles[triangleIndex];
        NavMeshPoint first = m_Vertices[triangle.First];
        NavMeshPoint second = m_Vertices[triangle.Second];
        NavMeshPoint third = m_Vertices[triangle.Third];
        double minX = Math.Min(first.X, Math.Min(second.X, third.X));
        double minY = Math.Min(first.Y, Math.Min(second.Y, third.Y));
        double maxX = Math.Max(first.X, Math.Max(second.X, third.X));
        double maxY = Math.Max(first.Y, Math.Max(second.Y, third.Y));
        return Overlaps(bounds, minX, minY, maxX, maxY);
    }

    private bool PolygonOverlapsBounds(int polygonIndex, NavMeshBounds bounds)
    {
        NavMeshBounds polygonBounds = m_Polygons[polygonIndex].Bounds;
        return Overlaps(bounds, polygonBounds.MinX, polygonBounds.MinY, polygonBounds.MaxX, polygonBounds.MaxY);
    }

    private static bool Overlaps(NavMeshBounds bounds, double minX, double minY, double maxX, double maxY)
    {
        return bounds.MinX <= maxX && bounds.MaxX >= minX && bounds.MinY <= maxY && bounds.MaxY >= minY;
    }

    private static int BuildBvh(int[] triangleIndices, int start, int count, NavMeshPoint[] vertices,
        NavMeshTriangle[] triangles, List<BvhNode> nodes)
    {
        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;
        for (int offset = 0; offset < count; offset++)
        {
            NavMeshTriangle triangle = triangles[triangleIndices[start + offset]];
            Include(vertices[triangle.First], ref minX, ref minY, ref maxX, ref maxY);
            Include(vertices[triangle.Second], ref minX, ref minY, ref maxX, ref maxY);
            Include(vertices[triangle.Third], ref minX, ref minY, ref maxX, ref maxY);
        }

        int nodeIndex = nodes.Count;
        nodes.Add(default);
        if (count <= 8)
        {
            nodes[nodeIndex] = new BvhNode(minX, minY, maxX, maxY, start, count, -1, -1);
            return nodeIndex;
        }

        bool splitX = maxX - minX >= maxY - minY;
        Array.Sort(triangleIndices, start, count,
            Comparer<int>.Create((left, right) => GetCenter(triangles[left], vertices, splitX)
                .CompareTo(GetCenter(triangles[right], vertices, splitX))));
        int leftCount = count / 2;
        int left = BuildBvh(triangleIndices, start, leftCount, vertices, triangles, nodes);
        int right = BuildBvh(triangleIndices, start + leftCount, count - leftCount, vertices, triangles, nodes);
        nodes[nodeIndex] = new BvhNode(minX, minY, maxX, maxY, 0, 0, left, right);
        return nodeIndex;
    }

    private static int BuildPolygonBvh(int[] polygonIndices, int start, int count, PolygonInfo[] polygons,
        List<BvhNode> nodes)
    {
        double minX = double.PositiveInfinity;
        double minY = double.PositiveInfinity;
        double maxX = double.NegativeInfinity;
        double maxY = double.NegativeInfinity;
        for (int offset = 0; offset < count; offset++)
        {
            NavMeshBounds bounds = polygons[polygonIndices[start + offset]].Bounds;
            minX = Math.Min(minX, bounds.MinX);
            minY = Math.Min(minY, bounds.MinY);
            maxX = Math.Max(maxX, bounds.MaxX);
            maxY = Math.Max(maxY, bounds.MaxY);
        }

        int nodeIndex = nodes.Count;
        nodes.Add(default);
        if (count <= 4)
        {
            nodes[nodeIndex] = new BvhNode(minX, minY, maxX, maxY, start, count, -1, -1);
            return nodeIndex;
        }

        bool splitX = maxX - minX >= maxY - minY;
        Array.Sort(polygonIndices, start, count,
            Comparer<int>.Create((left, right) => (splitX ? polygons[left].Center.X : polygons[left].Center.Y)
                .CompareTo(splitX ? polygons[right].Center.X : polygons[right].Center.Y)));
        int leftCount = count / 2;
        int left = BuildPolygonBvh(polygonIndices, start, leftCount, polygons, nodes);
        int right = BuildPolygonBvh(polygonIndices, start + leftCount, count - leftCount, polygons, nodes);
        nodes[nodeIndex] = new BvhNode(minX, minY, maxX, maxY, 0, 0, left, right);
        return nodeIndex;
    }

    private static void Include(NavMeshPoint point, ref double minX, ref double minY, ref double maxX, ref double maxY)
    {
        minX = Math.Min(minX, point.X);
        minY = Math.Min(minY, point.Y);
        maxX = Math.Max(maxX, point.X);
        maxY = Math.Max(maxY, point.Y);
    }

    private static double GetCenter(NavMeshTriangle triangle, NavMeshPoint[] vertices, bool x)
    {
        double first = x ? vertices[triangle.First].X : vertices[triangle.First].Y;
        double second = x ? vertices[triangle.Second].X : vertices[triangle.Second].Y;
        double third = x ? vertices[triangle.Third].X : vertices[triangle.Third].Y;
        return (first + second + third) / 3;
    }

    private NavMeshPoint Center(int triangleIndex)
    {
        return m_Centers[triangleIndex];
    }

    private NavMeshPoint PolygonCenter(int polygonIndex)
    {
        return m_Polygons[polygonIndex].Center;
    }

    /// <summary>
    /// 获取仅供同一程序集查询和组合网格使用的 polygon 区域标识.
    /// </summary>
    internal int GetPolygonAreaId(int polygonIndex)
    {
        return m_Polygons[polygonIndex].AreaId;
    }

    /// <summary>
    /// 获取仅供同一程序集查询和组合网格使用的 polygon 通行 flags.
    /// </summary>
    internal uint GetPolygonFlags(int polygonIndex)
    {
        return m_Polygons[polygonIndex].Flags;
    }

    /// <summary>
    /// 获取仅供同一程序集组合快照使用的 polygon 顶点索引只读跨度.
    /// </summary>
    internal ReadOnlySpan<int> GetPolygonVertexIndices(int polygonIndex)
    {
        return m_Polygons[polygonIndex].Vertices;
    }

    private static NavMeshPoint GetTriangleCenter(NavMeshTriangle triangle, NavMeshPoint[] vertices)
    {
        return new NavMeshPoint(
            (vertices[triangle.First].X + vertices[triangle.Second].X + vertices[triangle.Third].X) / 3,
            (vertices[triangle.First].Y + vertices[triangle.Second].Y + vertices[triangle.Third].Y) / 3);
    }

    private double TriangleArea(int triangleIndex)
    {
        NavMeshTriangle triangle = m_Triangles[triangleIndex];
        return NavMeshPoint.Cross(m_Vertices[triangle.First], m_Vertices[triangle.Second],
            m_Vertices[triangle.Third]) * 0.5;
    }

    private double PolygonArea(int polygonIndex)
    {
        return m_Polygons[polygonIndex].Area;
    }

    private NavMeshPoint ClosestPointOnTriangle(NavMeshPoint point, NavMeshTriangle triangle)
    {
        NavMeshPoint first = m_Vertices[triangle.First];
        NavMeshPoint second = m_Vertices[triangle.Second];
        NavMeshPoint third = m_Vertices[triangle.Third];
        double firstArea = NavMeshPoint.Cross(first, second, point);
        double secondArea = NavMeshPoint.Cross(second, third, point);
        double thirdArea = NavMeshPoint.Cross(third, first, point);
        if (firstArea >= 0 && secondArea >= 0 && thirdArea >= 0) return point;
        NavMeshPoint onFirst = ClosestPointOnSegment(point, first, second);
        NavMeshPoint onSecond = ClosestPointOnSegment(point, second, third);
        NavMeshPoint onThird = ClosestPointOnSegment(point, third, first);
        return DistanceSquared(point, onFirst) <= DistanceSquared(point, onSecond) &&
               DistanceSquared(point, onFirst) <= DistanceSquared(point, onThird) ? onFirst :
            DistanceSquared(point, onSecond) <= DistanceSquared(point, onThird) ? onSecond : onThird;
    }

    private NavMeshPoint ClosestPointOnPolygon(int polygonIndex, NavMeshPoint point)
    {
        if (ContainsPolygon(polygonIndex, point)) return point;
        return ClosestPointOnPolygonBoundary(polygonIndex, point);
    }

    private NavMeshPoint ClosestPointOnPolygonBoundary(int polygonIndex, NavMeshPoint point)
    {
        ReadOnlySpan<int> vertices = m_Polygons[polygonIndex].Vertices;
        NavMeshPoint closest = default;
        double closestDistanceSquared = double.PositiveInfinity;
        for (int index = 0; index < vertices.Length; index++)
        {
            NavMeshPoint start = m_Vertices[vertices[index]];
            NavMeshPoint end = m_Vertices[vertices[(index + 1) % vertices.Length]];
            NavMeshPoint candidate = ClosestPointOnSegment(point, start, end);
            double distanceSquared = DistanceSquared(point, candidate);
            if (distanceSquared >= closestDistanceSquared) continue;
            closest = candidate;
            closestDistanceSquared = distanceSquared;
        }

        return closest;
    }

    private bool HasTraversablePolygonNeighbor(int polygonIndex, int firstVertex, int secondVertex,
        INavMeshQueryFilter filter)
    {
        foreach (PolygonNeighbor neighbor in m_PolygonNeighbors[polygonIndex])
        {
            bool isSameEdge = neighbor.FirstVertex == firstVertex && neighbor.SecondVertex == secondVertex ||
                              neighbor.FirstVertex == secondVertex && neighbor.SecondVertex == firstVertex;
            if (!isSameEdge) continue;
            int target = neighbor.TargetPolygon;
            return filter.Pass(target, GetPolygonAreaId(target), GetPolygonFlags(target));
        }

        return false;
    }

    private static NavMeshPoint ClosestPointOnSegment(NavMeshPoint point, NavMeshPoint start, NavMeshPoint end)
    {
        double x = end.X - start.X;
        double y = end.Y - start.Y;
        double denominator = x * x + y * y;
        double factor = Math.Clamp(((point.X - start.X) * x + (point.Y - start.Y) * y) / denominator, 0, 1);
        return new NavMeshPoint(start.X + x * factor, start.Y + y * factor);
    }

    private static double DistanceSquared(NavMeshPoint first, NavMeshPoint second)
    {
        double x = second.X - first.X;
        double y = second.Y - first.Y;
        return x * x + y * y;
    }

    private static double DistanceSquaredToBounds(NavMeshPoint point, BvhNode bounds)
    {
        double x = point.X < bounds.MinX ? bounds.MinX - point.X : Math.Max(point.X - bounds.MaxX, 0);
        double y = point.Y < bounds.MinY ? bounds.MinY - point.Y : Math.Max(point.Y - bounds.MaxY, 0);
        return x * x + y * y;
    }

    private static void ValidateTriangle(NavMeshTriangle triangle, NavMeshPoint[] vertices, int triangleIndex)
    {
        if ((uint)triangle.First >= vertices.Length || (uint)triangle.Second >= vertices.Length ||
            (uint)triangle.Third >= vertices.Length || triangle.First == triangle.Second ||
            triangle.Second == triangle.Third ||
            triangle.Third == triangle.First) throw new ArgumentException($"三角形 {triangleIndex} 的顶点索引无效.");
        if (NavMeshPoint.Cross(vertices[triangle.First], vertices[triangle.Second], vertices[triangle.Third]) <= 0)
            throw new ArgumentException($"三角形 {triangleIndex} 必须逆时针且面积大于零.");
    }

    private static PolygonInfo[] BuildPolygons(NavMeshTriangle[] triangles, NavMeshPoint[] vertices,
        ReadOnlySpan<NavMeshConvexPolygon> sourcePolygons)
    {
        if (sourcePolygons.IsEmpty)
        {
            PolygonInfo[] result = new PolygonInfo[triangles.Length];
            for (int index = 0; index < triangles.Length; index++)
            {
                NavMeshTriangle triangle = triangles[index];
                result[index] = CreatePolygonInfo([triangle.First, triangle.Second, triangle.Third], triangle.AreaId,
                    triangle.Flags, vertices);
            }

            return result;
        }

        PolygonInfo[] polygons = new PolygonInfo[sourcePolygons.Length];
        for (int index = 0; index < sourcePolygons.Length; index++)
        {
            NavMeshConvexPolygon polygon = sourcePolygons[index];
            int[] polygonVertices = polygon.AsSpan().ToArray();
            polygons[index] = CreatePolygonInfo(polygonVertices, polygon.AreaId, polygon.Flags, vertices);
        }

        return polygons;
    }

    private static int[] CreateTrianglePolygons(int triangleCount)
    {
        int[] result = new int[triangleCount];
        for (int index = 0; index < result.Length; index++) result[index] = index;
        return result;
    }

    private static PolygonNeighbor[][] BuildPolygonNeighbors(ReadOnlySpan<PolygonInfo> polygons)
    {
        List<PolygonNeighbor>[] neighbors = new List<PolygonNeighbor>[polygons.Length];
        Dictionary<Edge, PolygonEdge> owners = new Dictionary<Edge, PolygonEdge>();
        HashSet<Edge> paired = new HashSet<Edge>();
        for (int polygonIndex = 0; polygonIndex < polygons.Length; polygonIndex++)
        {
            neighbors[polygonIndex] = new List<PolygonNeighbor>();
            ReadOnlySpan<int> vertices = polygons[polygonIndex].Vertices;
            for (int edgeIndex = 0; edgeIndex < vertices.Length; edgeIndex++)
            {
                int first = vertices[edgeIndex];
                int second = vertices[(edgeIndex + 1) % vertices.Length];
                Edge edge = new Edge(first, second);
                if (!owners.Remove(edge, out PolygonEdge owner))
                {
                    if (paired.Contains(edge)) throw new ArgumentException("同一无向边不能被三个以上多边形共享.");
                    owners.Add(edge, new PolygonEdge(polygonIndex, first, second));
                    continue;
                }

                double centerDistance = NavMeshPoint.Distance(polygons[polygonIndex].Center,
                    polygons[owner.PolygonIndex].Center);
                neighbors[polygonIndex].Add(new PolygonNeighbor(owner.PolygonIndex, first, second, centerDistance));
                neighbors[owner.PolygonIndex].Add(new PolygonNeighbor(polygonIndex, owner.FirstVertex,
                    owner.SecondVertex, centerDistance));
                paired.Add(edge);
            }
        }

        PolygonNeighbor[][] result = new PolygonNeighbor[neighbors.Length][];
        for (int index = 0; index < result.Length; index++) result[index] = neighbors[index].ToArray();
        return result;
    }

    private static NavMeshPoint GetPolygonCenter(ReadOnlySpan<int> polygonVertices, NavMeshPoint[] vertices)
    {
        double x = 0;
        double y = 0;
        foreach (int vertexIndex in polygonVertices)
        {
            x += vertices[vertexIndex].X;
            y += vertices[vertexIndex].Y;
        }

        return new NavMeshPoint(x / polygonVertices.Length, y / polygonVertices.Length);
    }

    private static PolygonInfo CreatePolygonInfo(int[] polygonVertices, int areaId, uint flags, NavMeshPoint[] vertices)
    {
        NavMeshPoint first = vertices[polygonVertices[0]];
        double minX = first.X;
        double minY = first.Y;
        double maxX = first.X;
        double maxY = first.Y;
        double twiceArea = 0;
        for (int index = 0; index < polygonVertices.Length; index++)
        {
            NavMeshPoint current = vertices[polygonVertices[index]];
            NavMeshPoint next = vertices[polygonVertices[(index + 1) % polygonVertices.Length]];
            Include(current, ref minX, ref minY, ref maxX, ref maxY);
            twiceArea += current.X * next.Y - current.Y * next.X;
        }

        return new PolygonInfo(polygonVertices, areaId, flags, GetPolygonCenter(polygonVertices, vertices),
            new NavMeshBounds(minX, minY, maxX, maxY), twiceArea * 0.5);
    }

    private static void ValidateConvexPolygon(ReadOnlySpan<int> indices, ReadOnlySpan<NavMeshPoint> vertices,
        int polygonIndex)
    {
        if (indices.Length < 3) throw new ArgumentException($"多边形 {polygonIndex} 至少需要三个顶点.");
        for (int index = 0; index < indices.Length; index++)
        {
            int current = indices[index];
            int next = indices[(index + 1) % indices.Length];
            if ((uint)current >= vertices.Length || current == next)
                throw new ArgumentException($"多边形 {polygonIndex} 的顶点索引无效.");
            for (int other = index + 1; other < indices.Length; other++)
            {
                if (current == indices[other])
                    throw new ArgumentException($"多边形 {polygonIndex} 包含重复顶点索引.");
            }
        }

        bool hasPositiveTurn = false;
        for (int index = 0; index < indices.Length; index++)
        {
            NavMeshPoint first = vertices[indices[index]];
            NavMeshPoint second = vertices[indices[(index + 1) % indices.Length]];
            NavMeshPoint third = vertices[indices[(index + 2) % indices.Length]];
            if (NavMeshPoint.Cross(first, second, third) <= 0)
                throw new ArgumentException($"多边形 {polygonIndex} 必须逆时针、严格凸且面积大于零.");
            hasPositiveTurn = true;
        }

        if (!hasPositiveTurn) throw new ArgumentException($"多边形 {polygonIndex} 面积必须大于零.");
    }

    private static JumpEdge[][] BuildJumpEdges(ReadOnlySpan<NavMeshJumpConnection> connections,
        NavMeshTriangle[] triangles, NavMeshPoint[] vertices, ReadOnlySpan<int> trianglePolygons, int polygonCount)
    {
        List<JumpEdge>[] lists = new List<JumpEdge>[polygonCount];
        for (int index = 0; index < lists.Length; index++) lists[index] = new List<JumpEdge>();
        for (int connectionIndex = 0; connectionIndex < connections.Length; connectionIndex++)
        {
            NavMeshJumpConnection connection = connections[connectionIndex];
            if (!connection.Start.IsFinite || !connection.End.IsFinite || !double.IsFinite(connection.FixedCost) ||
                connection.FixedCost < 0)
                throw new ArgumentException($"跳跃连接 {connectionIndex} 的端点或固定开销无效.", nameof(connections));
            int startTriangle = FindContainingTriangle(connection.Start, triangles, vertices);
            int endTriangle = FindContainingTriangle(connection.End, triangles, vertices);
            if (startTriangle < 0 || endTriangle < 0)
                throw new ArgumentException($"跳跃连接 {connectionIndex} 的端点必须位于导航网格内.", nameof(connections));
            int startPolygon = trianglePolygons[startTriangle];
            int endPolygon = trianglePolygons[endTriangle];
            lists[startPolygon].Add(new JumpEdge(endPolygon, connection.Start, connection.End, connection.FixedCost,
                connectionIndex + 1));
            if (connection.IsBidirectional)
                lists[endPolygon].Add(new JumpEdge(startPolygon, connection.End, connection.Start, connection.FixedCost,
                    -connectionIndex - 1));
        }

        JumpEdge[][] result = new JumpEdge[lists.Length][];
        for (int index = 0; index < result.Length; index++) result[index] = lists[index].ToArray();
        return result;
    }

    private static HeuristicJump[] BuildHeuristicJumps(ReadOnlySpan<NavMeshJumpConnection> connections)
    {
        int count = 0;
        foreach (NavMeshJumpConnection connection in connections) count += connection.IsBidirectional ? 2 : 1;
        HeuristicJump[] result = new HeuristicJump[count];
        int index = 0;
        foreach (NavMeshJumpConnection connection in connections)
        {
            result[index++] = new HeuristicJump(connection.Start, connection.End, connection.FixedCost);
            if (connection.IsBidirectional)
                result[index++] = new HeuristicJump(connection.End, connection.Start, connection.FixedCost);
        }

        return result;
    }

    private static double[] BuildHeuristicJumpTransitionDistances(ReadOnlySpan<HeuristicJump> jumps)
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

    private int PrepareJumpHeuristic(NavMeshPoint goal, double minimumMultiplier, NavMeshQueryWorkspace workspace)
    {
        int count = m_HeuristicJumps.Length;
        if (count == 0) return 0;
        if (count > MaximumHeuristicJumpCount) return -1;
        workspace.ResetJumpHeuristic(count);
        for (int index = 0; index < count; index++)
            workspace.JumpHeuristicCosts[index] = NavMeshPoint.Distance(m_HeuristicJumps[index].End, goal) *
                                                  minimumMultiplier;

        // 在跳跃落点之间执行反向 Dijkstra. 完整图仅限少量跳跃, 以避免为每次查询分配图结构.
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
            HeuristicJump nextJump = m_HeuristicJumps[current];
            for (int index = 0; index < count; index++)
            {
                if (workspace.JumpHeuristicClosed[index]) continue;
                double transitionDistance = m_HeuristicJumpTransitionDistances[index * count + current];
                double candidate = transitionDistance * minimumMultiplier + nextJump.FixedCost + currentCost;
                if (candidate < workspace.JumpHeuristicCosts[index]) workspace.JumpHeuristicCosts[index] = candidate;
            }
        }

        return count;
    }

    private double CalculateHeuristic(int polygonIndex, NavMeshPoint goal, double minimumMultiplier,
        double heuristicWeight, int heuristicJumpCount, NavMeshQueryWorkspace workspace)
    {
        NavMeshPoint position = PolygonCenter(polygonIndex);
        double lowerBound = NavMeshPoint.Distance(position, goal) * minimumMultiplier;
        if (heuristicJumpCount > 0)
        {
            for (int index = 0; index < heuristicJumpCount; index++)
            {
                HeuristicJump jump = m_HeuristicJumps[index];
                double candidate = NavMeshPoint.Distance(position, jump.Start) * minimumMultiplier + jump.FixedCost +
                                   workspace.JumpHeuristicCosts[index];
                if (candidate < lowerBound) lowerBound = candidate;
            }
        }
        else if (heuristicJumpCount < 0)
        {
            lowerBound = 0;
        }

        return lowerBound * heuristicWeight;
    }

    private static int FindContainingTriangle(NavMeshPoint point, NavMeshTriangle[] triangles, NavMeshPoint[] vertices)
    {
        for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex++)
        {
            NavMeshTriangle triangle = triangles[triangleIndex];
            if (NavMeshPoint.Cross(vertices[triangle.First], vertices[triangle.Second], point) >= -1e-12 &&
                NavMeshPoint.Cross(vertices[triangle.Second], vertices[triangle.Third], point) >= -1e-12 &&
                NavMeshPoint.Cross(vertices[triangle.Third], vertices[triangle.First], point) >= -1e-12)
                return triangleIndex;
        }

        return -1;
    }

    private static void AddEdge(int first, int second, int slot, Dictionary<Edge, int> owners,
        HashSet<Edge> pairedEdges,
        int[] neighbors)
    {
        Edge edge = new Edge(first, second);
        if (!owners.Remove(edge, out int otherSlot))
        {
            if (pairedEdges.Contains(edge)) throw new ArgumentException("同一无向边不能被三个以上三角形共享.");
            owners.Add(edge, slot);
            return;
        }

        neighbors[slot] = otherSlot / 3;
        neighbors[otherSlot] = slot / 3;
        pairedEdges.Add(edge);
    }

    private readonly record struct Edge(int First, int Second)
    {
        public int First { get; } = Math.Min(First, Second);
        public int Second { get; } = Math.Max(First, Second);
    }

    private readonly record struct Portal(NavMeshPoint Left, NavMeshPoint Right);

    private readonly record struct PolygonInfo(
        int[] Vertices,
        int AreaId,
        uint Flags,
        NavMeshPoint Center,
        NavMeshBounds Bounds,
        double Area);

    private readonly record struct PolygonEdge(int PolygonIndex, int FirstVertex, int SecondVertex);

    private readonly record struct PolygonNeighbor(
        int TargetPolygon,
        int FirstVertex,
        int SecondVertex,
        double CenterDistance);

    private readonly record struct JumpEdge(int TargetTriangle, NavMeshPoint Start, NavMeshPoint End,
        double FixedCost, int ParentMarker);

    private readonly record struct HeuristicJump(NavMeshPoint Start, NavMeshPoint End, double FixedCost);

    private readonly record struct BvhNode(
        double MinX,
        double MinY,
        double MaxX,
        double MaxY,
        int Start,
        int Count,
        int Left,
        int Right);
}
