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
    private readonly Dictionary<NavMeshTileId, Mesh> m_Tiles = new Dictionary<NavMeshTileId, Mesh>();
    private Mesh? m_Snapshot;

    /// <summary>
    /// 获取当前已装载 tile 数量.
    /// </summary>
    public int TileCount => m_Tiles.Count;

    /// <summary>
    /// 获取由全部已装载 tile 合成的不可变导航网格快照.
    /// 尚未装载 tile 时为 <see langword="null"/>.
    /// </summary>
    public Mesh? Snapshot => m_Snapshot;

    /// <summary>
    /// 添加新 tile 或替换同一标识的旧 tile, 随后重建快照.
    /// </summary>
    /// <param name="tileId">tile 的全局标识.</param>
    /// <param name="navMesh">使用全局坐标构建的不可变 tile 导航网格.</param>
    public void AddOrReplaceTile(NavMeshTileId tileId, Mesh navMesh)
    {
        ArgumentNullException.ThrowIfNull(navMesh);
        ApplyUpdates([new NavMeshTileUpdate(tileId, navMesh)]);
    }

    /// <summary>
    /// 移除指定 tile 并在成功时重建快照.
    /// </summary>
    /// <param name="tileId">待移除的 tile 标识.</param>
    /// <returns>找到并移除了 tile 时为 <see langword="true"/>.</returns>
    public bool RemoveTile(NavMeshTileId tileId)
    {
        return ApplyUpdates([new NavMeshTileUpdate(tileId, null)]);
    }

    /// <summary>
    /// 原子应用一组 tile 增加、替换或移除操作, 并至多重建一次全局快照.
    /// 当局部区域包含多个变更 tile 时, 此方法避免逐 tile 发布中间快照.
    /// 重建失败时会还原 tile 集合并保留原快照.
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
                    bool existed = m_Tiles.TryGetValue(update.TileId, out Mesh? original);
                    originalTiles.Add(new TileState(update.TileId, original, existed));
                }

                if (update.NavMesh is null)
                {
                    changed |= m_Tiles.Remove(update.TileId);
                    continue;
                }

                if (m_Tiles.TryGetValue(update.TileId, out Mesh? current) && ReferenceEquals(current, update.NavMesh))
                    continue;
                m_Tiles[update.TileId] = update.NavMesh;
                changed = true;
            }

            if (!changed) return false;
            RebuildSnapshot();
            return true;
        }
        catch
        {
            foreach (TileState original in originalTiles)
            {
                if (original.Existed)
                    m_Tiles[original.TileId] = original.NavMesh!;
                else
                    m_Tiles.Remove(original.TileId);
            }

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
        m_Snapshot = null;
    }

    private void RebuildSnapshot()
    {
        if (m_Tiles.Count == 0)
        {
            m_Snapshot = null;
            return;
        }

        List<NavMeshPoint> vertices = Factory.RentList<NavMeshPoint>();
        List<NavMeshConvexPolygon> polygons = Factory.RentList<NavMeshConvexPolygon>();
        List<NavMeshJumpConnection> jumpConnections = Factory.RentList<NavMeshJumpConnection>();
        int[]? remappedBuffer = null;
        try
        {
            Dictionary<NavMeshPoint, int> vertexIndices = new Dictionary<NavMeshPoint, int>();
            foreach (Mesh tile in m_Tiles.Values)
            {
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

            // 先构造局部变量, 确保重建异常时旧快照仍可供读取.
            Mesh snapshot = Mesh.Create(CollectionsMarshal.AsSpan(vertices), CollectionsMarshal.AsSpan(polygons),
                CollectionsMarshal.AsSpan(jumpConnections));
            m_Snapshot = snapshot;
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

    private readonly record struct TileState(NavMeshTileId TileId, Mesh? NavMesh, bool Existed);
}
