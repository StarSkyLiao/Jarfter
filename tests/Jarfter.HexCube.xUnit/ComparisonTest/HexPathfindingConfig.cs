using Jarfter.Drawing;
using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

internal static class HexPathfindingConfig
{
    internal static readonly HexGridCentral<HexNavigationCell> HexMap = CreateSnapshot();
    internal static readonly HexGridThetaStarNavigationProvider NavigationProvider =
        new HexGridThetaStarNavigationProvider(HexMap);
    internal static readonly HexCubePoint Start = new HexCubePoint(-20, 0);
    internal static readonly HexCubePoint Goal = new HexCubePoint(20, 0);

    internal static Bitmap RenderMap(HexGridCentral<HexNavigationCell> map,
        IReadOnlyList<HexCubePoint> path, Color32 pathColor)
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

        if (path.Count == 0) throw new InvalidOperationException("Theta* 未找到可绘制的路径.");

        // Theta* 的相邻路径点可跨越多个网格单元, 需要直接连接路径节点以呈现可视直线段.
        for (int index = 1; index < path.Count; index++)
        {
            bitmap.DrawLine(ToPixel(path[index - 1]), ToPixel(path[index]), pathColor, 3);
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
}
