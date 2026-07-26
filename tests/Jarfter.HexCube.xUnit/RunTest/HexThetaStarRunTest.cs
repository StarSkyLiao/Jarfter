using Jarfter.Core.Collections.Extensions;
using Jarfter.Drawing;
using Jarfter.Drawing.GraphicIO;
using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 运行 Theta* 路径搜索的性能测试, 并导出包含高代价地形和单位六边形障碍物的路径图像.
/// </summary>
public static class HexThetaStarRunTest
{
    private static readonly IPathfinder s_Pathfinder = CreatePathfinder(HexPathfindingConfig.HexMap);

    /// <summary>
    /// 执行 Theta* 搜索并将地图和路径保存为 BMP 图像.
    /// </summary>
    public static void RunResult()
    {
        PathfindingResult result = Run();
        SaveResult(result, "HexThetaStarPath.bmp", new Color32(147, 51, 234), "Theta*");
    }

    /// <summary>
    /// 执行一次 Theta* 搜索并返回从起点到终点的路径及总代价.
    /// </summary>
    /// <returns>路径搜索结果.</returns>
    internal static PathfindingResult Run()
    {
        return Run(s_Pathfinder);
    }

    /// <summary>
    /// 使用与当前运行地图一致的快照创建 Lazy Theta* 寻路器.
    /// </summary>
    /// <returns>配置完成的 Lazy Theta* 寻路器.</returns>
    internal static IPathfinder CreateLazyPathfinder() =>
        new IPathfinder.LazyThetaStar(IHeuristic.Euclidean.Instance, new NavigationProvider(
            HexPathfindingConfig.HexMap)
        );

    /// <summary>
    /// 在当前运行地图上执行指定寻路器.
    /// </summary>
    /// <param name="pathfinder">要执行的寻路器.</param>
    /// <returns>从固定起点到固定终点的路径搜索结果.</returns>
    internal static PathfindingResult Run(IPathfinder pathfinder)
    {
        return pathfinder.FindPath(HexPathfindingConfig.Start, HexPathfindingConfig.Goal);
    }

    /// <summary>
    /// 将路径绘制为当前运行地图的 BMP 图像并输出生成位置.
    /// </summary>
    /// <param name="result">要绘制的路径搜索结果.</param>
    /// <param name="fileName">相对输出文件名.</param>
    /// <param name="pathColor">路径线颜色.</param>
    /// <param name="algorithmName">用于控制台输出的算法名称.</param>
    internal static void SaveResult(PathfindingResult result, string fileName, Color32 pathColor, string algorithmName)
    {
        IReadOnlyList<HexCubePoint> path = result.Path;
        string filePath = Path.Combine(fileName);

        BitmapExtension.SaveAsBmp(HexPathfindingConfig.RenderMap(
                HexPathfindingConfig.HexMap, path, pathColor), filePath
        );

        Console.WriteLine(path.View());
        Console.WriteLine($"{algorithmName} 总移动代价: {result.TotalCost}");
        Console.WriteLine($"{algorithmName} 路径图已生成: {Path.GetFullPath(filePath)}");
    }

    private static IPathfinder CreatePathfinder(HexGridCentral<HexPathfindingConfig.HexNavigationCell> map) =>
        new IPathfinder.ThetaStar(IHeuristic.Euclidean.Instance, new NavigationProvider(map));

    private sealed class NavigationProvider(HexGridCentral<HexPathfindingConfig.HexNavigationCell> map) : IThetaStarNavigationProvider
    {
        private readonly ObstacleIndex m_ObstacleIndex = new ObstacleIndex(map);
        private readonly TerrainIndex m_TerrainIndex = new TerrainIndex(map);

        /// <inheritdoc />
        public double GetMoveCost(HexCubePoint destination)
        {
            if (!map.TryGetValue(destination, out HexPathfindingConfig.HexNavigationCell cell)) return -1;
            return cell.ObstacleApothemScale > 0 ? -1 : cell.TraversalMultiplier;
        }

        /// <inheritdoc />
        public bool HasLineOfSight(HexCubeLine2D line)
        {
            return !m_ObstacleIndex.Intersects(line);
        }

        /// <inheritdoc />
        public double GetLineCost(HexCubeLine2D line)
        {
            return m_TerrainIndex.CalculateCost(line);
        }

        private sealed class ObstacleIndex
        {
            private readonly Dictionary<int, HexCubeArea2D[]> m_AreasByQ;
            private readonly double m_MaxRadius;

            public ObstacleIndex(HexGridCentral<HexPathfindingConfig.HexNavigationCell> map)
            {
                Dictionary<int, List<HexCubeArea2D>> areasByQ = [];
                double maxRadius = 0;

                for (int q = -map.Radius; q <= map.Radius; q++)
                {
                    for (int r = -map.Radius; r <= map.Radius; r++)
                    {
                        HexCubePoint position = new HexCubePoint(q, r);
                        if (!map.TryGetValue(position, out HexPathfindingConfig.HexNavigationCell cell) || cell.ObstacleApothemScale <= 0) continue;

                        if (!areasByQ.TryGetValue(q, out List<HexCubeArea2D>? areas))
                        {
                            areas = [];
                            areasByQ.Add(q, areas);
                        }

                        areas.Add(new HexCubeArea2D(position, cell.ObstacleApothemScale));
                        maxRadius = Math.Max(maxRadius, cell.ObstacleApothemScale);
                    }
                }

                m_AreasByQ = new Dictionary<int, HexCubeArea2D[]>(areasByQ.Count);

                foreach ((int q, List<HexCubeArea2D> areas) in areasByQ)
                {
                    m_AreasByQ.Add(q, [.. areas]);
                }

                m_MaxRadius = maxRadius;
            }

            public bool Intersects(HexCubeLine2D line)
            {
                double minimumQ = Math.Min(line.Start.Q, line.End.Q);
                double maximumQ = Math.Max(line.Start.Q, line.End.Q);
                double minimumR = Math.Min(line.Start.R, line.End.R);
                double maximumR = Math.Max(line.Start.R, line.End.R);
                double minimumS = Math.Min(line.Start.S, line.End.S);
                double maximumS = Math.Max(line.Start.S, line.End.S);
                int firstQ = (int)Math.Ceiling(minimumQ - m_MaxRadius);
                int lastQ = (int)Math.Floor(maximumQ + m_MaxRadius);

                for (int q = firstQ; q <= lastQ; q++)
                {
                    if (!m_AreasByQ.TryGetValue(q, out HexCubeArea2D[]? areas)) continue;

                    foreach (HexCubeArea2D obstacle in areas)
                    {
                        double radius = obstacle.RadiusScale;

                        // Q 轴已由索引过滤; R/S 轴包围盒可避免绝大多数精确线段裁剪.
                        if (maximumR < obstacle.Position.R - radius || minimumR > obstacle.Position.R + radius) continue;
                        if (maximumS < obstacle.Position.S - radius || minimumS > obstacle.Position.S + radius) continue;
                        if (obstacle.IntersectsHex(line)) return true;
                    }
                }

                return false;
            }
        }

        private sealed class TerrainIndex
        {
            private readonly Dictionary<int, TraversalArea[]> m_AreasByQ;

            public TerrainIndex(HexGridCentral<HexPathfindingConfig.HexNavigationCell> map)
            {
                Dictionary<int, List<TraversalArea>> areasByQ = [];

                for (int q = -map.Radius; q <= map.Radius; q++)
                {
                    for (int r = -map.Radius; r <= map.Radius; r++)
                    {
                        HexCubePoint position = new HexCubePoint(q, r);
                        if (!map.TryGetValue(position, out HexPathfindingConfig.HexNavigationCell cell) || cell.TraversalMultiplier <= 1) continue;

                        // 边长为 0.5 的区域恰好对应一个网格单元, 用于按实际穿过长度计算地形代价.
                        TraversalArea area = new TraversalArea(new HexCubeArea2D(position, 0.5), cell.TraversalMultiplier);

                        if (!areasByQ.TryGetValue(q, out List<TraversalArea>? areas))
                        {
                            areas = [];
                            areasByQ.Add(q, areas);
                        }

                        areas.Add(area);
                    }
                }

                m_AreasByQ = new Dictionary<int, TraversalArea[]>(areasByQ.Count);

                foreach ((int q, List<TraversalArea> areas) in areasByQ)
                {
                    m_AreasByQ.Add(q, [.. areas]);
                }
            }

            public double CalculateCost(HexCubeLine2D line)
            {
                double lineLength = line.Length;
                double totalCost = lineLength;
                double minimumQ = Math.Min(line.Start.Q, line.End.Q);
                double maximumQ = Math.Max(line.Start.Q, line.End.Q);
                double minimumR = Math.Min(line.Start.R, line.End.R);
                double maximumR = Math.Max(line.Start.R, line.End.R);
                double minimumS = Math.Min(line.Start.S, line.End.S);
                double maximumS = Math.Max(line.Start.S, line.End.S);
                int firstQ = (int)Math.Ceiling(minimumQ - 0.5);
                int lastQ = (int)Math.Floor(maximumQ + 0.5);

                for (int q = firstQ; q <= lastQ; q++)
                {
                    if (!m_AreasByQ.TryGetValue(q, out TraversalArea[]? areas)) continue;

                    foreach (TraversalArea area in areas)
                    {
                        HexCubeArea2D shape = area.Shape;

                        if (maximumR < shape.Position.R - 0.5 || minimumR > shape.Position.R + 0.5) continue;
                        if (maximumS < shape.Position.S - 0.5 || minimumS > shape.Position.S + 0.5) continue;
                        if (!shape.TryGetIntersectionRange(line, out double tMin, out double tMax)) continue;

                        totalCost += lineLength * (tMax - tMin) * (area.TraversalMultiplier - 1);
                    }
                }

                return totalCost;
            }

            private readonly record struct TraversalArea(HexCubeArea2D Shape, double TraversalMultiplier);
        }
    }

}
