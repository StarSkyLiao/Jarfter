using Jarfter.Core.Collections.Extensions;
using Jarfter.Core.Diagnostics;
using Jarfter.Drawing;
using Jarfter.Drawing.GraphicIO;
using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 验证 A* 在包含高成本地形和交错障碍的六边形地图中能够规划路径, 并导出结果图像.
/// </summary>
public static class HexAStarTest
{
    /// <summary>
    /// 比较 A* 路径搜索的执行耗时.
    /// </summary>
    internal static void RunComparison()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(5) { TargetTime = TimeSpan.FromSeconds(0.5) }, [
            new MethodWrapper<IReadOnlyList<HexCubePoint>>(Run)
        ]);
    }

    /// <summary>
    /// 执行 A* 搜索并将地图和路径保存为 BMP 图像.
    /// </summary>
    public static void RunResult()
    {
        HexGridCentral<HexNavigationCell> map = CreateSnapshot();
        IReadOnlyList<HexCubePoint> path = FindPath(map);
        string filePath = Path.Combine("HexAStarPath.bmp");

        BitmapExtension.SaveAsBmp(RenderMap(map, path), filePath);

        Console.WriteLine(path.View());
        Console.WriteLine($"A* 路径图已生成: {Path.GetFullPath(filePath)}");
    }

    /// <summary>
    /// 执行一次 A* 搜索并返回从起点到终点的路径.
    /// </summary>
    /// <returns>从起点到终点的六边形坐标序列.</returns>
    internal static IReadOnlyList<HexCubePoint> Run()
    {
        HexGridCentral<HexNavigationCell> map = CreateSnapshot();
        return FindPath(map);
    }

    private static IReadOnlyList<HexCubePoint> FindPath(HexGridCentral<HexNavigationCell> map)
    {
        IPathfinder pathfinder = new IPathfinder.AStar(IHeuristic.Default.Instance, point =>
        {
            HexNavigationCell hexNavigationCell = map[point];
            if (hexNavigationCell.ObstacleApothemScale > 0) return -1;
            return hexNavigationCell.TraversalMultiplier;
        });
        return pathfinder.FindPath(new HexCubePoint(-20, 0), new HexCubePoint(20, 0));
    }

    private static Bitmap RenderMap(HexGridCentral<HexNavigationCell> map, IReadOnlyList<HexCubePoint> path)
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

                HexNavigationCell cell = map[point];
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

    private static Color32 GetCellColor(HexNavigationCell cell)
    {
        if (cell.ObstacleApothemScale > 0) return new Color32(71, 85, 105);
        if (cell.TraversalMultiplier > 1) return new Color32(254, 215, 170);
        return new Color32(241, 245, 249);
    }

    private static HexGridCentral<HexNavigationCell> CreateSnapshot()
    {
        HexGridCentral<HexNavigationCell> map = new HexGridCentral<HexNavigationCell>(32);
        map.InitializeCell(new HexNavigationCell(1));


        AddHighCostArea(map);
        AddBarrier(map, -10, -20, 15, -9, -5);
        AddBarrier(map, 0, -24, 24, 8, 12);
        AddBarrier(map, 10, -16, 20, -6, -2);

        return map;
    }

    private static void AddHighCostArea(HexGridCentral<HexNavigationCell> map)
    {
        // 中央区域的高成本地形会排除部分较短但代价更高的绕行路线.
        for (int q = -6; q <= 6; q++)
        {
            for (int r = -3; r <= 3; r++)
            {
                HexCubePoint point = new HexCubePoint(q, r);

                if (map.Contains(point))
                {
                    map[point] = new HexNavigationCell(3);
                }
            }
        }
    }

    private static void AddBarrier(HexGridCentral<HexNavigationCell> map, int q, int minimumR, int maximumR, int gapMinimumR, int gapMaximumR)
    {
        // 三道墙的缺口交错分布, 使路径需要反复改变行进方向而不能只做一次绕行.
        for (int r = minimumR; r <= maximumR; r++)
        {
            if (r >= gapMinimumR && r <= gapMaximumR)
            {
                continue;
            }

            HexCubePoint point = new HexCubePoint(q, r);

            if (map.Contains(point))
            {
                map[point] = new HexNavigationCell(1, 1);
            }
        }
    }

    private record struct HexNavigationCell(double TraversalMultiplier = 1, double ObstacleApothemScale = 0);
}
