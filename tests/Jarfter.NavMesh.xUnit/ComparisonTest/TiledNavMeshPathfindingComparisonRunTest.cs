using Jarfter.Core.Diagnostics;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Query;
using Jarfter.NavMesh.Tiles;
using Jarfter.NavMesh.Topology;
using System.Runtime.InteropServices;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.xUnit.ComparisonTest;

/// <summary>
/// 对比组合 tile 快照与兼容全局快照的热 corridor 查询开销.
/// 地图构建、兼容全局快照物化和跨 tile portal 索引均在计时前完成.
/// </summary>
public static class TiledNavMeshPathfindingComparisonRunTest
{
    private const int TileCountPerAxis = 8;
    private const int CellsPerTileAxis = 16;
    private static readonly NavMeshPoint s_Start = new NavMeshPoint(0.25, 0.25);
    private static readonly NavMeshPoint s_Goal = new NavMeshPoint(TileCountPerAxis * CellsPerTileAxis - 0.25,
        TileCountPerAxis * CellsPerTileAxis - 0.25);
    private static readonly PathfindingBenchmarkState s_State = CreateState();

    /// <summary>
    /// 运行组合快照与兼容全局快照的热 corridor 查询对比.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(10) { TargetTime = TimeSpan.FromSeconds(0.15) }, [
            new MethodWrapper<double>(RunTiledCorridorAStar),
            new MethodWrapper<double>(RunCompatibilityCorridorAStar)
        ]);
    }

    /// <summary>
    /// 使用不物化全局网格的组合快照执行一次热 corridor 查询.
    /// </summary>
    /// <returns>polygon 中心图的累计搜索成本, 用于防止基准调用被消除.</returns>
    internal static double RunTiledCorridorAStar()
    {
        return s_State.TiledSnapshot.TryFindCorridor(s_Start, s_Goal, s_State.TiledWorkspace,
            NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy, s_State.TiledCorridor, out _,
            out double searchCost)
            ? searchCost
            : 0d;
    }

    /// <summary>
    /// 使用已物化的兼容全局网格执行一次热 corridor 查询.
    /// </summary>
    /// <returns>polygon 中心图的累计搜索成本, 用于防止基准调用被消除.</returns>
    internal static double RunCompatibilityCorridorAStar()
    {
        return s_State.CompatibilitySnapshot.TryFindCorridor(s_Start, s_Goal, s_State.CompatibilityWorkspace,
            NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy, s_State.CompatibilityCorridor, out _,
            out double searchCost)
            ? searchCost
            : 0d;
    }

    private static PathfindingBenchmarkState CreateState()
    {
        TiledNavMesh tiledNavMesh = new TiledNavMesh();
        List<NavMeshTileUpdate> updates = new List<NavMeshTileUpdate>(TileCountPerAxis * TileCountPerAxis);
        for (int tileY = 0; tileY < TileCountPerAxis; tileY++)
        {
            for (int tileX = 0; tileX < TileCountPerAxis; tileX++)
                updates.Add(new NavMeshTileUpdate(new NavMeshTileId(tileX, tileY), CreateTile(tileX, tileY)));
        }

        if (!tiledNavMesh.ApplyUpdates(CollectionsMarshal.AsSpan(updates)))
            throw new InvalidOperationException("初始 tile 集合必须发布首个快照.");
        TiledNavMeshSnapshot tiledSnapshot = tiledNavMesh.TileSnapshot ??
                                             throw new InvalidOperationException("组合快照必须已发布.");
        Mesh compatibilitySnapshot = tiledNavMesh.Snapshot ??
                                   throw new InvalidOperationException("兼容全局快照必须已物化.");
        TiledNavMeshQueryWorkspace tiledWorkspace = new TiledNavMeshQueryWorkspace();
        TiledNavMeshPolygon[] tiledCorridor = new TiledNavMeshPolygon[tiledSnapshot.PolygonCount];
        NavMeshQueryWorkspace compatibilityWorkspace = compatibilitySnapshot.CreateQueryWorkspace();
        int[] compatibilityCorridor = new int[compatibilitySnapshot.PolygonCount];

        // 计时前建立 portal 图与 PriorityQueue 的数组容量, 保证基准仅测量重复查询.
        if (!tiledSnapshot.TryFindCorridor(s_Start, s_Goal, tiledWorkspace, NavMeshQueryDefaults.Filter,
                NavMeshQueryDefaults.CostPolicy, tiledCorridor, out _, out _))
            throw new InvalidOperationException("组合快照预热查询必须可达.");
        if (!compatibilitySnapshot.TryFindCorridor(s_Start, s_Goal, compatibilityWorkspace,
                NavMeshQueryDefaults.Filter, NavMeshQueryDefaults.CostPolicy, compatibilityCorridor, out _, out _))
            throw new InvalidOperationException("兼容全局快照预热查询必须可达.");
        return new PathfindingBenchmarkState(tiledSnapshot, tiledWorkspace, tiledCorridor, compatibilitySnapshot,
            compatibilityWorkspace, compatibilityCorridor);
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

    private sealed class PathfindingBenchmarkState
    {
        public PathfindingBenchmarkState(TiledNavMeshSnapshot tiledSnapshot, TiledNavMeshQueryWorkspace tiledWorkspace,
            TiledNavMeshPolygon[] tiledCorridor, Mesh compatibilitySnapshot,
            NavMeshQueryWorkspace compatibilityWorkspace, int[] compatibilityCorridor)
        {
            TiledSnapshot = tiledSnapshot;
            TiledWorkspace = tiledWorkspace;
            TiledCorridor = tiledCorridor;
            CompatibilitySnapshot = compatibilitySnapshot;
            CompatibilityWorkspace = compatibilityWorkspace;
            CompatibilityCorridor = compatibilityCorridor;
        }

        public TiledNavMeshSnapshot TiledSnapshot { get; }

        public TiledNavMeshQueryWorkspace TiledWorkspace { get; }

        public TiledNavMeshPolygon[] TiledCorridor { get; }

        public Mesh CompatibilitySnapshot { get; }

        public NavMeshQueryWorkspace CompatibilityWorkspace { get; }

        public int[] CompatibilityCorridor { get; }
    }
}
