using Jarfter.Core.Collections.Extensions;
using Jarfter.Core.Diagnostics;
using Jarfter.Drawing;
using Jarfter.Drawing.GraphicIO;
using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;
using Jarfter.HexCube.Pathfinding.Continuous;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 运行连续六边形 NavMesh 的性能测试, 并导出路径图像.
/// </summary>
public static class ContinuousHexNavMeshRunTest
{
    private const int PixelRadius = 10;

    private static readonly ContinuousNavigationBounds s_Bounds =
        new ContinuousNavigationBounds(new HexCubeArea2D(HexCubePoint.Zero, 32));
    private static readonly ContinuousNavigationMap s_Map = CreateMap();
    private static readonly IContinuousNavigationSnapshot s_Snapshot = s_Map.CaptureSnapshot();
    private static readonly IContinuousPathfinder s_Pathfinder = new ContinuousHexNavMeshPathfinder(
        s_Bounds);
    private static readonly ContinuousPathRequest s_Request = new ContinuousPathRequest(
        HexPathfindingConfig.Start,
        HexPathfindingConfig.Goal,
        AgentRadius: 0);

    /// <summary>
    /// 对固定地图快照执行 NavMesh 性能测试.
    /// 首次调用会构建网格, 后续迭代复用同一快照的缓存网格.
    /// </summary>
    public static void RunComparison()
    {
        // 热查询会复用已构建的 NavMesh, 用于反映地图未变化时的单次寻路开销.
        Benchmark.RunQuickTest(new BenchmarkOption(10) { TargetTime = TimeSpan.FromSeconds(0.15) }, [
            new MethodWrapper<ContinuousPathResult>(Run)
        ]);

        // 直接调用构建入口, 不经过寻路器缓存, 用于反映地图变化后的重建开销.
        Benchmark.RunQuickTest(new BenchmarkOption(10) { TargetTime = TimeSpan.FromSeconds(0.15) }, [
            new MethodWrapper<ContinuousHexNavMesh>(BuildNavMesh)
        ]);

        // 直接创建不可变快照并建立空间索引, 不包含可变地图字典的编辑操作和 NavMesh 构建.
        Benchmark.RunQuickTest(new BenchmarkOption(10) { TargetTime = TimeSpan.FromSeconds(0.15) }, [
            new MethodWrapper<ContinuousNavigationSnapshot>(BuildSnapshot)
        ]);
    }

    /// <summary>
    /// 执行 NavMesh 寻路并将结果输出为 BMP 图像.
    /// </summary>
    public static void RunResult()
    {
        ContinuousPathResult result = Run();
        SaveResult(result, "ContinuousHexNavMeshPath.bmp", new Color32(8, 145, 178), "连续六边形 NavMesh");
    }

    /// <summary>
    /// 使用固定地图快照执行一次连续六边形 NavMesh 寻路.
    /// </summary>
    /// <returns>从固定起点到固定终点的连续路径搜索结果.</returns>
    internal static ContinuousPathResult Run()
    {
        return s_Pathfinder.FindPath(s_Request, s_Snapshot);
    }

    /// <summary>
    /// 获取固定 NavMesh 运行地图的不可变快照.
    /// </summary>
    internal static IContinuousNavigationSnapshot Snapshot => s_Snapshot;

    /// <summary>
    /// 获取固定 NavMesh 运行地图的起终点请求.
    /// </summary>
    internal static ContinuousPathRequest Request => s_Request;

    /// <summary>
    /// 使用固定地图快照从零构建一次 NavMesh.
    /// 该方法不读取寻路器缓存, 用于单独测量地图变更后的 NavMesh 重建开销.
    /// </summary>
    /// <returns>与固定地图快照绑定的新 NavMesh.</returns>
    internal static ContinuousHexNavMesh BuildNavMesh()
    {
        return ContinuousHexNavMesh.Build(
            s_Snapshot,
            s_Bounds,
            0,
            0);
    }

    /// <summary>
    /// 使用固定障碍物和高代价区域从零创建不可变导航快照.
    /// 该方法仅测量快照数据复制与空间索引构建, 不包含 NavMesh 构建.
    /// </summary>
    /// <returns>与固定地图数据绑定的新导航快照.</returns>
    internal static ContinuousNavigationSnapshot BuildSnapshot()
    {
        return new ContinuousNavigationSnapshot(s_Snapshot.Revision, s_Snapshot.Obstacles, s_Snapshot.TraversalAreas);
    }

    private static void SaveResult(ContinuousPathResult result, string fileName, Color32 pathColor, string algorithmName)
    {
        string filePath = Path.Combine(fileName);
        BitmapExtension.SaveAsBmp(RenderMap(result, pathColor), filePath);

        Console.WriteLine(result.Path.View());
        Console.WriteLine($"{algorithmName} 总移动代价: {result.TotalCost}");
        Console.WriteLine($"{algorithmName} 路径图已生成: {Path.GetFullPath(filePath)}");
    }

    private static ContinuousNavigationMap CreateMap()
    {
        ContinuousNavigationMap map = new ContinuousNavigationMap();
        HexGridCentral<HexNavigationCell> gridMap = HexPathfindingConfig.HexMap;
        long obstacleId = 0;
        long traversalAreaId = 0;

        for (int q = -gridMap.Radius; q <= gridMap.Radius; q++)
        {
            for (int r = -gridMap.Radius; r <= gridMap.Radius; r++)
            {
                HexCubeGridPoint point = new HexCubeGridPoint(q, r);
                if (!gridMap.TryGetValue(point, out HexNavigationCell cell)) continue;

                if (cell.ObstacleApothemScale > 0)
                {
                    map.SetObstacle(++obstacleId, new HexCubeArea2D(point, cell.ObstacleApothemScale));
                }

                if (cell.TraversalMultiplier > 1)
                {
                    // 与 HexGridThetaStarNavigationProvider 保持一致: 边长为 0.5 的区域对应一个网格单元.
                    map.SetTraversalArea(++traversalAreaId, new HexCubeArea2D(point, 0.5), cell.TraversalMultiplier);
                }
            }
        }

        return map;
    }

    private static Bitmap RenderMap(ContinuousPathResult result, Color32 pathColor)
    {
        HexGridCentral<HexNavigationCell> gridMap = HexPathfindingConfig.HexMap;
        const int margin = PixelRadius + 4;
        double horizontalSpacing = Math.Sqrt(3) * PixelRadius;
        double verticalSpacing = 1.5 * PixelRadius;
        int width = (int)Math.Ceiling(2 * gridMap.Radius * horizontalSpacing) + margin * 2;
        int height = (int)Math.Ceiling(2 * gridMap.Radius * verticalSpacing) + margin * 2;
        Bitmap bitmap = new Bitmap(width, height);
        bitmap.FillAll(new Color32(248, 250, 252));

        for (int q = -gridMap.Radius; q <= gridMap.Radius; q++)
        {
            for (int r = -gridMap.Radius; r <= gridMap.Radius; r++)
            {
                HexCubeGridPoint point = new HexCubeGridPoint(q, r);
                if (!gridMap.TryGetValue(point, out HexNavigationCell cell)) continue;

                bitmap.DrawRegularHexagon(
                    ToPixel(point),
                    PixelRadius,
                    new Color32(203, 213, 225),
                    GetCellColor(cell));
            }
        }

        for (int index = 1; index < result.Path.Count; index++)
        {
            bitmap.DrawLine(ToPixel(result.Path[index - 1]), ToPixel(result.Path[index]), pathColor, 3);
        }

        int agentPixelRadius = Math.Max(4, (int)Math.Round(s_Request.AgentRadius * PixelRadius));
        bitmap.DrawRegularHexagon(ToPixel(s_Request.Start), agentPixelRadius, new Color32(21, 128, 61), new Color32(134, 239, 172), 2);
        bitmap.DrawRegularHexagon(ToPixel(s_Request.Goal), agentPixelRadius, new Color32(185, 28, 28), new Color32(252, 165, 165), 2);
        return bitmap;
    }

    private static Color32 GetCellColor(HexNavigationCell cell)
    {
        if (cell.ObstacleApothemScale > 0) return new Color32(71, 85, 105);
        if (cell.TraversalMultiplier > 1) return new Color32(254, 215, 170);
        return new Color32(241, 245, 249);
    }

    private static (int x, int y) ToPixel(HexCubePoint point)
    {
        double horizontalSpacing = Math.Sqrt(3) * PixelRadius;
        double verticalSpacing = 1.5 * PixelRadius;
        int margin = PixelRadius + 4;
        int mapRadius = HexPathfindingConfig.HexMap.Radius;
        return (
            margin + (int)Math.Round(mapRadius * horizontalSpacing + (point.Q + point.R / 2d) * horizontalSpacing),
            margin + (int)Math.Round(mapRadius * verticalSpacing + point.R * verticalSpacing));
    }
}
