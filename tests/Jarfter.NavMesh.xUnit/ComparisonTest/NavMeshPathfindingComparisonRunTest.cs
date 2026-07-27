using Jarfter.Core.Diagnostics;
using Jarfter.Drawing;
using Jarfter.Drawing.GraphicIO;
using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;
using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Query;
using Jarfter.NavMesh.Topology;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.xUnit.ComparisonTest;

/// <summary>
/// 使用与 HexPathfindingComparisonRunTest 完全相同的六边形地图生成规则, 对比 Hex A* 与 Jarfter.NavMesh A*.
/// 障碍物单元在两侧均按整格不可通行处理, 高代价单元保留原始移动倍率.
/// </summary>
public static class NavMeshPathfindingComparisonRunTest
{
    private const int HexRadius = 10;
    private const int Margin = HexRadius + 4;

    private static readonly HexCubeGridPoint s_HexStart = new HexCubeGridPoint(-20, 0);
    private static readonly HexCubeGridPoint s_HexGoal = new HexCubeGridPoint(20, 0);
    private static readonly HexGridCentral<HexNavigationCell> s_HexMap = CreateHexMap();
    private static readonly double s_HexEdgeLength = 1 / Math.Sqrt(3);
    private static readonly double s_HorizontalSpacing = Math.Sqrt(3) * HexRadius;
    private static readonly double s_VerticalSpacing = 1.5 * HexRadius;

    private static readonly IPathfinder s_HexPathfinder = new IPathfinder.AStar(IHeuristic.Default.Instance,
        new HexGridThetaStarNavigationProvider(s_HexMap));

    private static readonly ComparisonMap s_NavMeshMap = CreateNavMeshMap(s_HexMap);
    private static readonly NavMeshQueryWorkspace s_Workspace = s_NavMeshMap.NavMesh.CreateQueryWorkspace();
    private static readonly int[] s_Corridor = new int[s_NavMeshMap.NavMesh.PolygonCount];

    /// <summary>
    /// 运行同地图 Hex A* 与 NavMesh A* 的热查询对比.
    /// 基准只包含寻路, NavMesh 网格与 Hex 地图均在初始化阶段构建完毕.
    /// </summary>
    public static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(10) { LoopCount = 10 }, [
            new MethodWrapper<double>(RunHexAStar),
            new MethodWrapper<double>(RunNavMeshCorridorAStar)
        ]);
    }

    /// <summary>
    /// 分别导出 Hex A* 与 NavMesh A* 在同一张地图上的 BMP 路径图.
    /// 输出文件位于当前工作目录, 可直接比较两条路线是否均避开障碍并绕开高代价区域.
    /// </summary>
    public static void RunResult()
    {
        PathfindingResult hexResult = FindHexAStarPath();
        NavMeshPath navMeshPath =
            FindNavMeshAStarPath() ?? throw new InvalidOperationException("NavMesh A* 未找到可绘制的路径.");
        if (hexResult.Path.Count == 0) throw new InvalidOperationException("Hex A* 未找到可绘制的路径.");

        string hexFilePath = "HexNavMeshComparison.HexAStar.bmp";
        string navMeshFilePath = "HexNavMeshComparison.NavMeshAStar.bmp";
        BitmapExtension.SaveAsBmp(RenderHexPath(hexResult.Path), hexFilePath);
        BitmapExtension.SaveAsBmp(RenderNavMeshPath(navMeshPath.Points), navMeshFilePath);

        Console.WriteLine($"Hex A* 总移动代价: {hexResult.TotalCost}");
        Console.WriteLine($"NavMesh A* 总移动代价: {navMeshPath.TotalCost}");
        Console.WriteLine($"Hex A* 路径图已生成: {Path.GetFullPath(hexFilePath)}");
        Console.WriteLine($"NavMesh A* 路径图已生成: {Path.GetFullPath(navMeshFilePath)}");
    }

    /// <summary>
    /// 执行一次与基准地图相同的 Hex A* 查询.
    /// </summary>
    /// <returns>结果路径中的六边形节点数量.</returns>
    internal static double RunHexAStar()
    {
        return FindHexAStarPath().TotalCost;
    }

    /// <summary>
    /// 执行一次与基准地图相同的 NavMesh A* 查询.
    /// </summary>
    /// <returns>结果路径的总代价.</returns>
    internal static double RunNavMeshAStar()
    {
        return FindNavMeshAStarPath()?.TotalCost ?? 0;
    }

    /// <summary>
    /// 执行一次与 Detour FindPath 对应的仅 corridor NavMesh A* 查询.
    /// </summary>
    /// <returns>A* 在凸多边形中心图上的搜索总代价.</returns>
    internal static double RunNavMeshCorridorAStar()
    {
        return s_NavMeshMap.NavMesh.TryFindCorridor(ToNavMeshPoint(s_HexStart), ToNavMeshPoint(s_HexGoal), s_Workspace,
            NavMeshQueryDefaults.Filter, s_NavMeshMap.CostPolicy, s_Corridor, out _, out double totalCost)
            ? totalCost
            : 0;
    }

    private static PathfindingResult FindHexAStarPath()
    {
        return s_HexPathfinder.FindPath(s_HexStart, s_HexGoal);
    }

    private static NavMeshPath? FindNavMeshAStarPath()
    {
        return s_NavMeshMap.NavMesh.FindPath(ToNavMeshPoint(s_HexStart), ToNavMeshPoint(s_HexGoal), s_Workspace,
            NavMeshQueryDefaults.Filter, s_NavMeshMap.CostPolicy);
    }

    private static HexGridCentral<HexNavigationCell> CreateHexMap()
    {
        HexGridCentral<HexNavigationCell> map = new HexGridCentral<HexNavigationCell>(32);
        map.InitializeCell(new HexNavigationCell(1));
        AddHighCostArea(map);
        AddBarrier(map, -10, -20, 15, -9, -5);
        AddBarrier(map, 0, -24, 24, 8, 12);
        AddBarrier(map, 10, -16, 20, -6, -2);
        AddIrregularTerrainAndObstacles(map);
        return map;
    }

    private static Bitmap RenderHexPath(IReadOnlyList<HexCubeGridPoint> path)
    {
        Bitmap bitmap = CreateMapBitmap();
        for (int index = 1; index < path.Count; index++)
            bitmap.DrawLine(ToPixel(path[index - 1]), ToPixel(path[index]), new Color32(37, 99, 235), 3);
        DrawEndpoints(bitmap);
        return bitmap;
    }

    private static Bitmap RenderNavMeshPath(IReadOnlyList<NavMeshPoint> path)
    {
        Bitmap bitmap = CreateMapBitmap();
        for (int index = 1; index < path.Count; index++)
            bitmap.DrawLine(ToPixel(path[index - 1]), ToPixel(path[index]), new Color32(220, 38, 38), 3);
        DrawEndpoints(bitmap);
        return bitmap;
    }

    private static Bitmap CreateMapBitmap()
    {
        int width = (int)Math.Ceiling(2 * s_HexMap.Radius * s_HorizontalSpacing) + Margin * 2;
        int height = (int)Math.Ceiling(2 * s_HexMap.Radius * s_VerticalSpacing) + Margin * 2;
        Bitmap bitmap = new Bitmap(width, height);
        bitmap.FillAll(new Color32(248, 250, 252));

        for (int q = -s_HexMap.Radius; q <= s_HexMap.Radius; q++)
        {
            for (int r = -s_HexMap.Radius; r <= s_HexMap.Radius; r++)
            {
                HexCubeGridPoint point = new HexCubeGridPoint(q, r);
                if (!s_HexMap.TryGetValue(point, out HexNavigationCell cell)) continue;
                bitmap.DrawRegularHexagon(ToPixel(point), HexRadius, new Color32(203, 213, 225), GetCellColor(cell));
            }
        }

        return bitmap;
    }

    private static void DrawEndpoints(Bitmap bitmap)
    {
        bitmap.DrawRegularHexagon(ToPixel(s_HexStart), HexRadius, new Color32(21, 128, 61), new Color32(134, 239, 172),
            2);
        bitmap.DrawRegularHexagon(ToPixel(s_HexGoal), HexRadius, new Color32(185, 28, 28), new Color32(252, 165, 165),
            2);
    }

    private static Color32 GetCellColor(HexNavigationCell cell)
    {
        if (cell.ObstacleApothemScale > 0) return new Color32(71, 85, 105);
        if (cell.TraversalMultiplier > 1) return new Color32(254, 215, 170);
        return new Color32(241, 245, 249);
    }

    private static void AddHighCostArea(HexGridCentral<HexNavigationCell> map)
    {
        for (int q = -6; q <= 6; q++)
        {
            for (int r = -3; r <= 3; r++)
            {
                HexCubeGridPoint point = new HexCubeGridPoint(q, r);
                if (map.Contains(point)) map[point] = new HexNavigationCell(3);
            }
        }
    }

    private static void AddBarrier(HexGridCentral<HexNavigationCell> map, int q, int minimumR, int maximumR,
        int gapMinimumR, int gapMaximumR)
    {
        for (int r = minimumR; r <= maximumR; r++)
        {
            if (r >= gapMinimumR && r <= gapMaximumR) continue;
            HexCubeGridPoint point = new HexCubeGridPoint(q, r);
            if (map.Contains(point)) map[point] = new HexNavigationCell(1, 1);
        }
    }

    private static void AddIrregularTerrainAndObstacles(HexGridCentral<HexNavigationCell> map)
    {
        Random random = new Random(20260726);
        for (int q = -map.Radius; q <= map.Radius; q++)
        {
            for (int r = -map.Radius; r <= map.Radius; r++)
            {
                HexCubeGridPoint point = new HexCubeGridPoint(q, r);
                if (!map.TryGetValue(point, out HexNavigationCell cell) || cell.ObstacleApothemScale > 0 ||
                    point.HexDistanceTo(s_HexStart) <= 2 || point.HexDistanceTo(s_HexGoal) <= 2)
                    continue;
                if (q is -10 or 0 or 10) continue;
                double randomValue = random.NextDouble();
                if (randomValue < 0.05)
                    map[point] = new HexNavigationCell(1, 0.3 + random.NextDouble() * 0.4);
                else if (randomValue < 0.3)
                    map[point] = new HexNavigationCell(1.25 + random.NextDouble() * 2.25);
            }
        }
    }

    private static ComparisonMap CreateNavMeshMap(HexGridCentral<HexNavigationCell> map)
    {
        List<NavMeshPoint> vertices = new List<NavMeshPoint>();
        List<NavMeshConvexPolygon> polygons = new List<NavMeshConvexPolygon>();
        Dictionary<VertexKey, int> vertexIndices = new Dictionary<VertexKey, int>();
        Dictionary<double, int> areaIds = new Dictionary<double, int> { [1] = 0 };
        List<double> multipliers = [1];
        Span<int> outerIndices = stackalloc int[6];
        for (int q = -map.Radius; q <= map.Radius; q++)
        {
            for (int r = -map.Radius; r <= map.Radius; r++)
            {
                HexCubeGridPoint cellPoint = new HexCubeGridPoint(q, r);
                if (!map.TryGetValue(cellPoint, out HexNavigationCell cell) || cell.ObstacleApothemScale > 0) continue;
                int areaId = GetAreaId(cell.TraversalMultiplier, areaIds, multipliers);
                NavMeshPoint center = ToNavMeshPoint(cellPoint);
                for (int index = 0; index < outerIndices.Length; index++)
                    outerIndices[index] = GetVertexIndex(GetHexVertex(center, index), vertices, vertexIndices);
                polygons.Add(new NavMeshConvexPolygon(outerIndices, areaId));
            }
        }

        return new ComparisonMap(Mesh.Create([.. vertices], [.. polygons]), new AreaCostPolicy([.. multipliers]));
    }

    private static int GetAreaId(double multiplier, Dictionary<double, int> areaIds, List<double> multipliers)
    {
        if (areaIds.TryGetValue(multiplier, out int areaId)) return areaId;
        areaId = multipliers.Count;
        areaIds.Add(multiplier, areaId);
        multipliers.Add(multiplier);
        return areaId;
    }

    private static int GetVertexIndex(NavMeshPoint point, List<NavMeshPoint> vertices,
        Dictionary<VertexKey, int> vertexIndices)
    {
        VertexKey key = new VertexKey((long)Math.Round(point.X * 1e12), (long)Math.Round(point.Y * 1e12));
        if (vertexIndices.TryGetValue(key, out int index)) return index;
        index = vertices.Count;
        vertices.Add(point);
        vertexIndices.Add(key, index);
        return index;
    }

    private static NavMeshPoint ToNavMeshPoint(HexCubeGridPoint point)
    {
        return new NavMeshPoint(point.Q + point.R / 2d, Math.Sqrt(3) * point.R / 2);
    }

    private static (int x, int y) ToPixel(HexCubeGridPoint point)
    {
        return (
            Margin + (int)Math.Round(s_HexMap.Radius * s_HorizontalSpacing +
                                     (point.Q + point.R / 2d) * s_HorizontalSpacing),
            Margin + (int)Math.Round(s_HexMap.Radius * s_VerticalSpacing + point.R * s_VerticalSpacing));
    }

    private static (int x, int y) ToPixel(NavMeshPoint point)
    {
        return (
            Margin + (int)Math.Round(s_HexMap.Radius * s_HorizontalSpacing + point.X * HexRadius / s_HexEdgeLength),
            Margin + (int)Math.Round(s_HexMap.Radius * s_VerticalSpacing + point.Y * HexRadius / s_HexEdgeLength));
    }

    private static NavMeshPoint GetHexVertex(NavMeshPoint center, int index)
    {
        double angle = Math.PI / 6 + Math.PI / 3 * index;
        return new NavMeshPoint(center.X + Math.Cos(angle) * s_HexEdgeLength,
            center.Y + Math.Sin(angle) * s_HexEdgeLength);
    }

    private readonly record struct VertexKey(long X, long Y);

    private sealed record ComparisonMap(Mesh NavMesh, AreaCostPolicy CostPolicy);

    private sealed class AreaCostPolicy(double[] multipliers) : INavMeshTraversalCostPolicy
    {
        public double MinimumMultiplier => 1;
        public double GetMultiplier(int fromAreaId, int toAreaId) => multipliers[toAreaId];
    }
}
