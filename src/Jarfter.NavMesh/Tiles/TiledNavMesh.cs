using Jarfter.Core.Collections.ObjectModel;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Topology;
using System.Buffers;
using System.Runtime.InteropServices;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.Tiles;

/// <summary>
/// 管理多个静态 tile, 并将其合成为可跨共享边寻路的不可变导航网格快照.
/// tile 中相同 double 坐标的顶点会被合并, 因此相邻 tile 必须使用完全一致的边界顶点.
/// 修改操作应由单一写入线程串行调用, 已取得的 Snapshot 可继续安全读取.
/// </summary>
public sealed class TiledNavMesh
{
    private readonly Dictionary<NavMeshTileId, TiledNavMeshTile> m_Tiles =
        new Dictionary<NavMeshTileId, TiledNavMeshTile>();
    private readonly Dictionary<TiledNavMeshEdgeKey, PortalOwners> m_PortalOwners =
        new Dictionary<TiledNavMeshEdgeKey, PortalOwners>();
    private readonly List<TiledNavMeshPortal> m_Portals = new List<TiledNavMeshPortal>();
    private NavMeshJumpConnection[] m_CrossTileJumpConnections = [];
    private TiledNavMeshSnapshot? m_TileSnapshot;
    private Mesh? m_Snapshot;

    /// <summary>
    /// 获取当前已装载 tile 数量.
    /// </summary>
    public int TileCount => m_Tiles.Count;

    /// <summary>
    /// 获取当前配置的跨 tile 跳跃连接数量.
    /// </summary>
    public int CrossTileJumpConnectionCount => m_CrossTileJumpConnections.Length;

    /// <summary>
    /// 获取由全部已装载 tile 合成的兼容单体导航网格快照.
    /// 此属性首次读取或 tile 更新后首次读取时才会物化全局网格. 新代码应优先使用
    /// <see cref="TileSnapshot"/>, 以避免局部更新后立即进行全量重建.
    /// </summary>
    public Mesh? Snapshot => m_TileSnapshot is null ? null : m_Snapshot ??= BuildCompatibilitySnapshot(m_TileSnapshot);

    /// <summary>
    /// 获取由已装载 tile 组成的不可变组合快照.
    /// 更新时仅复用未变更 tile 的网格引用, 不会合并 polygon 或重建全局 BVH.
    /// </summary>
    public TiledNavMeshSnapshot? TileSnapshot => m_TileSnapshot;

    /// <summary>
    /// 添加新 tile 或替换同一标识的旧 tile, 随后发布组合快照.
    /// </summary>
    /// <param name="tileId">tile 的全局标识.</param>
    /// <param name="navMesh">使用全局坐标构建的不可变 tile 导航网格.</param>
    public void AddOrReplaceTile(NavMeshTileId tileId, Mesh navMesh)
    {
        ArgumentNullException.ThrowIfNull(navMesh);
        ApplyUpdates([new NavMeshTileUpdate(tileId, navMesh)]);
    }

    /// <summary>
    /// 替换全部跨 tile 跳跃连接并发布新组合快照.
    /// 连接端点必须位于当前每条连接对应的可行走 tile polygon 内.
    /// </summary>
    /// <param name="connections">新的跨 tile 跳跃连接集合.</param>
    /// <returns>连接集合发生变化并成功发布快照时为 <see langword="true"/>.</returns>
    public bool SetCrossTileJumpConnections(ReadOnlySpan<NavMeshJumpConnection> connections)
    {
        NavMeshJumpConnection[] replacement = connections.ToArray();
        if (replacement.AsSpan().SequenceEqual(m_CrossTileJumpConnections)) return false;
        NavMeshJumpConnection[] original = m_CrossTileJumpConnections;
        m_CrossTileJumpConnections = replacement;
        try
        {
            PublishTileSnapshot();
            return true;
        }
        catch
        {
            m_CrossTileJumpConnections = original;
            throw;
        }
    }

    /// <summary>
    /// 将跨 tile 跳跃连接复制到调用方提供的缓冲区.
    /// </summary>
    /// <param name="destination">接收跳跃连接的缓冲区.</param>
    /// <returns>实际写入数量. 缓冲区不足时返回 destination 的长度.</returns>
    public int CopyCrossTileJumpConnections(Span<NavMeshJumpConnection> destination)
    {
        int written = Math.Min(destination.Length, m_CrossTileJumpConnections.Length);
        m_CrossTileJumpConnections.AsSpan(0, written).CopyTo(destination);
        return written;
    }

    /// <summary>
    /// 移除指定 tile 并在成功时发布组合快照.
    /// </summary>
    /// <param name="tileId">待移除的 tile 标识.</param>
    /// <returns>找到并移除了 tile 时为 <see langword="true"/>.</returns>
    public bool RemoveTile(NavMeshTileId tileId)
    {
        return ApplyUpdates([new NavMeshTileUpdate(tileId, null)]);
    }

    /// <summary>
    /// 原子应用一组 tile 增加、替换或移除操作, 并至多发布一次组合快照.
    /// 未变更 tile 的不可变网格引用会直接复用. 调用 <see cref="Snapshot"/> 时才会按需合成兼容全局网格.
    /// 更新失败时会还原 tile 集合并保留原快照.
    /// </summary>
    /// <param name="updates">按顺序应用的 tile 更新. 同一标识可出现多次, 最后一次状态生效.</param>
    /// <returns>至少一个 tile 的最终状态发生变化时为 <see langword="true"/>.</returns>
    public bool ApplyUpdates(ReadOnlySpan<NavMeshTileUpdate> updates)
    {
        if (updates.IsEmpty) return false;
        List<TileState> originalTiles = Factory.RentList<TileState>();
        try
        {
            bool changed = false;
            foreach (NavMeshTileUpdate update in updates)
            {
                if (!ContainsTile(originalTiles, update.TileId))
                {
                    bool existed = m_Tiles.TryGetValue(update.TileId, out TiledNavMeshTile? original);
                    originalTiles.Add(new TileState(update.TileId, original, existed));
                }

                if (update.NavMesh is null)
                {
                    if (m_Tiles.Remove(update.TileId, out TiledNavMeshTile? removed))
                    {
                        RemoveBoundaryPortals(removed);
                        RemoveTilePortals(update.TileId);
                        changed = true;
                    }
                    continue;
                }

                if (m_Tiles.TryGetValue(update.TileId, out TiledNavMeshTile? current) &&
                    ReferenceEquals(current.NavMesh, update.NavMesh))
                    continue;
                TiledNavMeshTile replacement = new TiledNavMeshTile(update.TileId, update.NavMesh);
                if (current is not null)
                {
                    RemoveBoundaryPortals(current);
                    RemoveTilePortals(update.TileId);
                }
                m_Tiles[update.TileId] = replacement;
                AddBoundaryPortals(replacement);
                AddTilePortals(replacement);
                changed = true;
            }

            if (!changed) return false;
            PublishTileSnapshot();
            return true;
        }
        catch
        {
            foreach (TileState original in originalTiles)
            {
                if (original.Existed)
                    m_Tiles[original.TileId] = original.Tile!;
                else
                    m_Tiles.Remove(original.TileId);
            }

            RebuildPortalOwners();
            RebuildPortals();

            throw;
        }
        finally
        {
            Factory.Release(originalTiles);
        }
    }

    /// <summary>
    /// 移除全部 tile 和当前快照.
    /// </summary>
    public void Clear()
    {
        m_Tiles.Clear();
        m_PortalOwners.Clear();
        m_Portals.Clear();
        m_CrossTileJumpConnections = [];
        m_TileSnapshot = null;
        m_Snapshot = null;
    }

    private void PublishTileSnapshot()
    {
        m_TileSnapshot = m_Tiles.Count == 0
            ? null
            : new TiledNavMeshSnapshot(m_Tiles.Values, CollectionsMarshal.AsSpan(m_Portals), m_CrossTileJumpConnections);
        m_Snapshot = null;
    }

    private void AddBoundaryPortals(TiledNavMeshTile tile)
    {
        foreach (TiledNavMeshBoundaryEdge edgeTemplate in tile.BoundaryEdges)
        {
            TiledNavMeshPortalEndpoint endpoint = new TiledNavMeshPortalEndpoint(tile.TileId,
                edgeTemplate.PolygonIndex, edgeTemplate.Segment);
            TiledNavMeshEdgeKey edge = new TiledNavMeshEdgeKey(endpoint.Segment);
            if (!m_PortalOwners.TryGetValue(edge, out PortalOwners owners))
            {
                m_PortalOwners.Add(edge, new PortalOwners(endpoint));
                continue;
            }

            if (owners.Count != 1 || owners.First.TileId == endpoint.TileId)
                throw new ArgumentException("同一边界线段不能由三个以上 tile 或同一 tile 的多个边界边共享.");
            m_PortalOwners[edge] = new PortalOwners(owners.First, endpoint);
        }
    }

    private void RemoveBoundaryPortals(TiledNavMeshTile tile)
    {
        foreach (TiledNavMeshBoundaryEdge edgeTemplate in tile.BoundaryEdges)
        {
            TiledNavMeshPortalEndpoint endpoint = new TiledNavMeshPortalEndpoint(tile.TileId,
                edgeTemplate.PolygonIndex, edgeTemplate.Segment);
            TiledNavMeshEdgeKey edge = new TiledNavMeshEdgeKey(endpoint.Segment);
            if (!m_PortalOwners.TryGetValue(edge, out PortalOwners owners))
                throw new InvalidOperationException("tile 边界门户索引不一致.");
            if (owners.Count == 1 && owners.First == endpoint)
            {
                m_PortalOwners.Remove(edge);
                continue;
            }

            if (owners.Count == 2 && owners.First == endpoint)
            {
                m_PortalOwners[edge] = new PortalOwners(owners.Second);
                continue;
            }

            if (owners.Count == 2 && owners.Second == endpoint)
            {
                m_PortalOwners[edge] = new PortalOwners(owners.First);
                continue;
            }

            throw new InvalidOperationException("tile 边界门户端点不属于索引拥有者.");
        }
    }

    private void RebuildPortalOwners()
    {
        m_PortalOwners.Clear();
        foreach (TiledNavMeshTile tile in m_Tiles.Values) AddBoundaryPortals(tile);
    }

    private void AddTilePortals(TiledNavMeshTile tile, bool onlyLaterTiles = false)
    {
        foreach (TiledNavMeshTile other in m_Tiles.Values)
        {
            if (other.TileId == tile.TileId || (onlyLaterTiles && CompareTileIds(tile.TileId, other.TileId) >= 0) ||
                !tile.Bounds.Overlaps(other.Bounds))
                continue;
            foreach (TiledNavMeshBoundaryEdge firstEdge in tile.BoundaryEdges)
            {
                foreach (TiledNavMeshBoundaryEdge secondEdge in other.BoundaryEdges)
                {
                    if (!TryGetOverlappingSegment(firstEdge.Segment, secondEdge.Segment, out NavMeshSegment segment))
                        continue;
                    if (CompareTileIds(tile.TileId, other.TileId) <= 0)
                    {
                        m_Portals.Add(new TiledNavMeshPortal(tile.TileId, firstEdge.PolygonIndex, other.TileId,
                            secondEdge.PolygonIndex, segment));
                    }
                    else
                    {
                        m_Portals.Add(new TiledNavMeshPortal(other.TileId, secondEdge.PolygonIndex, tile.TileId,
                            firstEdge.PolygonIndex, segment));
                    }
                }
            }
        }
    }

    private void RemoveTilePortals(NavMeshTileId tileId)
    {
        for (int index = m_Portals.Count - 1; index >= 0; index--)
        {
            TiledNavMeshPortal portal = m_Portals[index];
            if (portal.FirstTileId != tileId && portal.SecondTileId != tileId) continue;
            m_Portals.RemoveAt(index);
        }
    }

    private void RebuildPortals()
    {
        m_Portals.Clear();
        foreach (TiledNavMeshTile tile in m_Tiles.Values) AddTilePortals(tile, true);
    }

    private static int CompareTileIds(NavMeshTileId left, NavMeshTileId right)
    {
        int layer = left.Layer.CompareTo(right.Layer);
        if (layer != 0) return layer;
        int y = left.Y.CompareTo(right.Y);
        return y != 0 ? y : left.X.CompareTo(right.X);
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

    private static Mesh BuildCompatibilitySnapshot(TiledNavMeshSnapshot tileSnapshot)
    {
        List<NavMeshPoint> vertices = Factory.RentList<NavMeshPoint>();
        List<NavMeshConvexPolygon> polygons = Factory.RentList<NavMeshConvexPolygon>();
        List<NavMeshJumpConnection> jumpConnections = Factory.RentList<NavMeshJumpConnection>();
        int[]? remappedBuffer = null;
        try
        {
            Dictionary<NavMeshPoint, int> vertexIndices = new Dictionary<NavMeshPoint, int>();
            foreach (TiledNavMeshTile tileEntry in tileSnapshot.TileSpan)
            {
                Mesh tile = tileEntry.NavMesh;
                ReadOnlySpan<NavMeshPoint> tileVertices = tile.VertexSpan;
                if (remappedBuffer is null || remappedBuffer.Length < tileVertices.Length)
                {
                    if (remappedBuffer is not null) ArrayPool<int>.Shared.Return(remappedBuffer);
                    remappedBuffer = ArrayPool<int>.Shared.Rent(tileVertices.Length);
                }

                Span<int> remappedVertices = remappedBuffer.AsSpan(0, tileVertices.Length);
                for (int vertexIndex = 0; vertexIndex < tileVertices.Length; vertexIndex++)
                {
                    NavMeshPoint vertex = tileVertices[vertexIndex];
                    if (!vertexIndices.TryGetValue(vertex, out int remappedIndex))
                    {
                        remappedIndex = vertices.Count;
                        vertices.Add(vertex);
                        vertexIndices.Add(vertex, remappedIndex);
                    }

                    remappedVertices[vertexIndex] = remappedIndex;
                }

                for (int polygonIndex = 0; polygonIndex < tile.PolygonCount; polygonIndex++)
                {
                    ReadOnlySpan<int> sourceVertices = tile.GetPolygonVertexIndices(polygonIndex);
                    int[] polygonVertices = new int[sourceVertices.Length];
                    for (int index = 0; index < polygonVertices.Length; index++)
                        polygonVertices[index] = remappedVertices[sourceVertices[index]];
                    polygons.Add(NavMeshConvexPolygon.CreateOwned(polygonVertices, tile.GetPolygonAreaId(polygonIndex),
                        tile.GetPolygonFlags(polygonIndex)));
                }

                foreach (NavMeshJumpConnection jumpConnection in tile.JumpConnectionSpan)
                    jumpConnections.Add(jumpConnection);
            }

            return Mesh.Create(CollectionsMarshal.AsSpan(vertices), CollectionsMarshal.AsSpan(polygons),
                CollectionsMarshal.AsSpan(jumpConnections));
        }
        finally
        {
            if (remappedBuffer is not null) ArrayPool<int>.Shared.Return(remappedBuffer);
            Factory.Release(jumpConnections);
            Factory.Release(polygons);
            Factory.Release(vertices);
        }
    }

    private static bool ContainsTile(List<TileState> states, NavMeshTileId tileId)
    {
        foreach (TileState state in states)
        {
            if (state.TileId == tileId) return true;
        }

        return false;
    }

    private readonly record struct TileState(NavMeshTileId TileId, TiledNavMeshTile? Tile, bool Existed);

    private readonly record struct PortalOwners(
        TiledNavMeshPortalEndpoint First,
        TiledNavMeshPortalEndpoint Second,
        byte Count)
    {
        public PortalOwners(TiledNavMeshPortalEndpoint first) : this(first, default, 1)
        {
        }

        public PortalOwners(TiledNavMeshPortalEndpoint first, TiledNavMeshPortalEndpoint second) : this(first, second,
            2)
        {
        }
    }
}
