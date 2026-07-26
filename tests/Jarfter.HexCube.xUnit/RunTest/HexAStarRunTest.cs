using Jarfter.Core.Collections.Extensions;
using Jarfter.Drawing;
using Jarfter.Drawing.GraphicIO;
using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证 A* 在包含高成本地形和交错障碍的六边形地图中能够规划路径, 并导出结果图像.
/// </summary>
public static class HexAStarRunTest
{
    private static readonly IPathfinder s_Pathfinder = CreatePathfinder(HexPathfindingConfig.HexMap);

    /// <summary>
    /// 执行 A* 搜索并将地图和路径保存为 BMP 图像.
    /// </summary>
    public static void RunResult()
    {
        PathfindingResult result = Run();
        IReadOnlyList<HexCubePoint> path = result.Path;
        string filePath = Path.Combine("HexAStarPath.bmp");

        BitmapExtension.SaveAsBmp(RenderMap(HexPathfindingConfig.HexMap, path), filePath);

        Console.WriteLine(path.View());
        Console.WriteLine($"A* 总移动代价: {result.TotalCost}");
        Console.WriteLine($"A* 路径图已生成: {Path.GetFullPath(filePath)}");
    }

    /// <summary>
    /// 执行一次 A* 搜索并返回从起点到终点的路径及总代价.
    /// </summary>
    /// <returns>路径搜索结果.</returns>
    internal static PathfindingResult Run()
    {
        return s_Pathfinder.FindPath(HexPathfindingConfig.Start, HexPathfindingConfig.Goal);
    }

    private static IPathfinder CreatePathfinder(HexGridCentral<HexPathfindingConfig.HexNavigationCell> map) =>
        new IPathfinder.AStar(IHeuristic.Default.Instance, new NavigationMoveCostProvider(map));

    private static Bitmap RenderMap(HexGridCentral<HexPathfindingConfig.HexNavigationCell> map, IReadOnlyList<HexCubePoint> path)
    {
        const int hexRadius = 10;
        const int margin = hexRadius + 4;
        double horizontalSpacing = Math.Sqrt(3) * hexRadius;
        double verticalSpacing = 1.5 * hexRadius;

        int width = (int)Math.Ceiling(2 * map.Radius * horizontalSpacing) + margin * 2;
        int height = (int)Math.Ceiling(2 * map.Radius * verticalSpacing) + margin * 2;
        Bitmap bitmap = new Bitmap(width, height);
        bitmap.FillAll(new Color32(248, 250, 252));

        for (int q = -map.Radius; q <= map.Radius; q++)
        {
            for (int r = -map.Radius; r <= map.Radius; r++)
            {
                HexCubePoint point = new HexCubePoint(q, r);
                if (!map.Contains(point)) continue;

                HexPathfindingConfig.HexNavigationCell cell = map[point];
                bitmap.DrawRegularHexagon(
                    ToPixel(point),
                    hexRadius,
                    new Color32(203, 213, 225),
                    GetCellColor(cell));
            }
        }

        if (path.Count == 0) throw new InvalidOperationException("A* 未找到可绘制的路径.");

        // 路径线绘制在网格单元之上, 以便清晰显示跨越障碍的实际绕行轨迹.
        for (int index = 1; index < path.Count; index++)
        {
            bitmap.DrawLine(ToPixel(path[index - 1]), ToPixel(path[index]), new Color32(37, 99, 235), 3);
        }

        bitmap.DrawRegularHexagon(ToPixel(path[0]), hexRadius, new Color32(21, 128, 61), new Color32(134, 239, 172), 2);
        bitmap.DrawRegularHexagon(ToPixel(path[^1]), hexRadius, new Color32(185, 28, 28), new Color32(252, 165, 165), 2);
        return bitmap;

        (int x, int y) ToPixel(HexCubePoint point) =>
        (
            margin + (int)Math.Round(map.Radius * horizontalSpacing + (point.Q + point.R / 2) * horizontalSpacing),
            margin + (int)Math.Round(map.Radius * verticalSpacing + point.R * verticalSpacing)
        );
    }

    private static Color32 GetCellColor(HexPathfindingConfig.HexNavigationCell cell)
    {
        if (cell.ObstacleApothemScale > 0) return new Color32(71, 85, 105);
        if (cell.TraversalMultiplier > 1) return new Color32(254, 215, 170);
        return new Color32(241, 245, 249);
    }

    private sealed class NavigationMoveCostProvider(HexGridCentral<HexPathfindingConfig.HexNavigationCell> map)
        : IMoveCostProvider
    {
        /// <inheritdoc />
        public double GetMoveCost(HexCubePoint destination)
        {
            HexPathfindingConfig.HexNavigationCell cell = map[destination];
            return cell.ObstacleApothemScale > 0 ? -1 : cell.TraversalMultiplier;
        }
    }

}
