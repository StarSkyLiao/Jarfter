using Jarfter.Core.Diagnostics;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Tiles;
using Jarfter.NavMesh.Topology;
using System.Runtime.InteropServices;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.xUnit.ComparisonTest;

/// <summary>
/// 测量替换单个 tile 时, <see cref="TiledNavMesh"/> 发布组合快照与物化兼容全局快照的耗时.
/// 基准地图包含固定数量的 tile 与凸 polygon, 计时过程不包含 tile 自身的构建.
/// </summary>
public static class TiledNavMeshRebuildComparisonRunTest
{
    private const int TileCountPerAxis = 8;
    private const int CellsPerTileAxis = 16;
    private static readonly RebuildBenchmarkState s_State = CreateState();

    /// <summary>
    /// 运行单 tile 替换并物化兼容全局快照的热基准.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(10) { TargetTime = TimeSpan.FromSeconds(0.15) }, [
            new MethodWrapper<int>(RunSingleTileRebuild)
        ]);
    }

    /// <summary>
    /// 运行单 tile 替换且仅发布组合快照的热基准.
    /// 该入口独立校准, 避免与兼容全局快照的毫秒级重建共享循环次数.
    /// </summary>
    public static void RunTileSnapshotUpdateComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(10) { TargetTime = TimeSpan.FromSeconds(0.15) }, [
            new MethodWrapper<int>(RunSingleTileSnapshotUpdate)
        ]);
    }

    /// <summary>
    /// 替换一个已装载 tile, 触发当前实现的完整快照重建.
    /// </summary>
    /// <returns>新快照包含的 polygon 数量, 用于防止基准调用被消除.</returns>
    internal static int RunSingleTileRebuild()
    {
        Mesh replacement = s_State.UseAlternateTile ? s_State.AlternateTile : s_State.OriginalTile;
        s_State.UseAlternateTile = !s_State.UseAlternateTile;
        if (!s_State.TiledNavMesh.ApplyUpdates([new NavMeshTileUpdate(new NavMeshTileId(0, 0), replacement)]))
            throw new InvalidOperationException("替换不同的 tile 实例必须发布新快照.");
        return s_State.TiledNavMesh.Snapshot?.PolygonCount ?? 0;
    }

    /// <summary>
    /// 替换一个已装载 tile, 仅发布组合快照而不读取兼容单体快照.
    /// </summary>
    /// <returns>组合快照中的 tile 数量, 用于防止基准调用被消除.</returns>
    internal static int RunSingleTileSnapshotUpdate()
    {
        Mesh replacement = s_State.UseAlternateTile ? s_State.AlternateTile : s_State.OriginalTile;
        s_State.UseAlternateTile = !s_State.UseAlternateTile;
        if (!s_State.TiledNavMesh.ApplyUpdates([new NavMeshTileUpdate(new NavMeshTileId(0, 0), replacement)]))
            throw new InvalidOperationException("替换不同的 tile 实例必须发布组合快照.");
        return s_State.TiledNavMesh.TileSnapshot?.TileCount ?? 0;
    }

    private static RebuildBenchmarkState CreateState()
    {
        TiledNavMesh tiledNavMesh = new TiledNavMesh();
        List<NavMeshTileUpdate> updates = new List<NavMeshTileUpdate>(TileCountPerAxis * TileCountPerAxis);
        Mesh? originalTile = null;
        for (int tileY = 0; tileY < TileCountPerAxis; tileY++)
        {
            for (int tileX = 0; tileX < TileCountPerAxis; tileX++)
            {
                Mesh tile = CreateTile(tileX, tileY);
                updates.Add(new NavMeshTileUpdate(new NavMeshTileId(tileX, tileY), tile));
                if (tileX == 0 && tileY == 0) originalTile = tile;
            }
        }

        if (!tiledNavMesh.ApplyUpdates(CollectionsMarshal.AsSpan(updates)))
            throw new InvalidOperationException("初始 tile 集合必须发布首个快照.");
        return new RebuildBenchmarkState(tiledNavMesh, originalTile!, CreateTile(0, 0));
    }

    private static Mesh CreateTile(int tileX, int tileY)
    {
        int vertexCountPerAxis = CellsPerTileAxis + 1;
        List<NavMeshPoint> vertices = new List<NavMeshPoint>(vertexCountPerAxis * vertexCountPerAxis);
        List<NavMeshConvexPolygon> polygons = new List<NavMeshConvexPolygon>(CellsPerTileAxis * CellsPerTileAxis);
        int firstX = tileX * CellsPerTileAxis;
        int firstY = tileY * CellsPerTileAxis;
        for (int y = 0; y < vertexCountPerAxis; y++)
        {
            for (int x = 0; x < vertexCountPerAxis; x++)
                vertices.Add(new NavMeshPoint(firstX + x, firstY + y));
        }

        for (int y = 0; y < CellsPerTileAxis; y++)
        {
            for (int x = 0; x < CellsPerTileAxis; x++)
            {
                int lowerLeft = y * vertexCountPerAxis + x;
                polygons.Add(new NavMeshConvexPolygon([
                    lowerLeft,
                    lowerLeft + 1,
                    lowerLeft + vertexCountPerAxis + 1,
                    lowerLeft + vertexCountPerAxis
                ]));
            }
        }

        return Mesh.Create(CollectionsMarshal.AsSpan(vertices), CollectionsMarshal.AsSpan(polygons));
    }

    private sealed class RebuildBenchmarkState
    {
        public RebuildBenchmarkState(TiledNavMesh tiledNavMesh, Mesh originalTile, Mesh alternateTile)
        {
            TiledNavMesh = tiledNavMesh;
            OriginalTile = originalTile;
            AlternateTile = alternateTile;
        }

        public TiledNavMesh TiledNavMesh { get; }

        public Mesh OriginalTile { get; }

        public Mesh AlternateTile { get; }

        public bool UseAlternateTile { get; set; } = true;
    }
}
