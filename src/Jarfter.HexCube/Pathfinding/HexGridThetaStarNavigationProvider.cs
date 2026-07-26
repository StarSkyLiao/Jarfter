using Jarfter.HexCube.Grids;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 为 <see cref="IPathfinder.AStar"/>, <see cref="IPathfinder.ThetaStar"/> 和 <see cref="IPathfinder.LazyThetaStar"/> 提供默认的有限六边形地图导航数据.
/// 构造时会复制输入地图并预构建障碍物和高代价地形索引, 因此同一实例在寻路期间始终读取一致的地图快照.
/// </summary>
public sealed class HexGridThetaStarNavigationProvider : IThetaStarNavigationProvider
{
    private readonly HexGridCentral<HexNavigationCell> m_Map;
    private readonly ObstacleIndex m_ObstacleIndex;
    private readonly TerrainIndex m_TerrainIndex;

    /// <summary>
    /// 使用指定地图的当前状态创建导航数据提供程序.
    /// 后续对 <paramref name="map"/> 的修改不会影响本实例.
    /// </summary>
    /// <param name="map">要复制为导航快照的有限六边形地图.</param>
    public HexGridThetaStarNavigationProvider(HexGridCentral<HexNavigationCell> map)
    {
        ArgumentNullException.ThrowIfNull(map);

        m_Map = CopySnapshot(map);
        m_ObstacleIndex = new ObstacleIndex(m_Map);
        m_TerrainIndex = new TerrainIndex(m_Map);
        UsesUniformTraversalCost = HasUniformTraversalCost(m_Map);
    }

    /// <inheritdoc />
    public bool UsesUniformTraversalCost { get; }

    /// <inheritdoc />
    public double GetMoveCost(HexCubeGridPoint destination)
    {
        if (!m_Map.TryGetValue(destination, out HexNavigationCell cell)) return -1;
        return cell.ObstacleApothemScale > 0 ? -1 : cell.TraversalMultiplier;
    }

    /// <inheritdoc />
    public bool HasLineOfSight(HexCubeLine2D line) => !m_ObstacleIndex.Intersects(line);

    /// <inheritdoc />
    public bool TryGetLineCost(HexCubeLine2D line, out double cost)
    {
        if (m_ObstacleIndex.Intersects(line))
        {
            cost = 0;
            return false;
        }

        cost = m_TerrainIndex.CalculateCost(line);
        return true;
    }

    /// <inheritdoc />
    public double GetLineCost(HexCubeLine2D line) => m_TerrainIndex.CalculateCost(line);

    private static HexGridCentral<HexNavigationCell> CopySnapshot(HexGridCentral<HexNavigationCell> source)
    {
        HexGridCentral<HexNavigationCell> snapshot = new HexGridCentral<HexNavigationCell>(source.Radius);

        for (int q = -source.Radius; q <= source.Radius; q++)
        {
            for (int r = -source.Radius; r <= source.Radius; r++)
            {
                HexCubeGridPoint position = new HexCubeGridPoint(q, r);
                if (source.TryGetValue(position, out HexNavigationCell cell)) snapshot[position] = cell;
            }
        }

        return snapshot;
    }

    private static bool HasUniformTraversalCost(HexGridCentral<HexNavigationCell> map)
    {
        foreach (HexNavigationCell cell in map.Elements)
        {
            if (cell.ObstacleApothemScale <= 0 && cell.TraversalMultiplier != 1) return false;
        }

        return true;
    }

    private sealed class ObstacleIndex
    {
        private readonly Dictionary<int, HexCubeArea2D[]> m_AreasByQ;
        private readonly double m_MaxRadius;

        public ObstacleIndex(HexGridCentral<HexNavigationCell> map)
        {
            Dictionary<int, List<HexCubeArea2D>> areasByQ = [];
            double maxRadius = 0;

            for (int q = -map.Radius; q <= map.Radius; q++)
            {
                for (int r = -map.Radius; r <= map.Radius; r++)
                {
                    HexCubeGridPoint position = new HexCubeGridPoint(q, r);
                    if (!map.TryGetValue(position, out HexNavigationCell cell) || cell.ObstacleApothemScale <= 0) continue;

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

        public TerrainIndex(HexGridCentral<HexNavigationCell> map)
        {
            Dictionary<int, List<TraversalArea>> areasByQ = [];

            for (int q = -map.Radius; q <= map.Radius; q++)
            {
                for (int r = -map.Radius; r <= map.Radius; r++)
                {
                    HexCubeGridPoint position = new HexCubeGridPoint(q, r);
                    if (!map.TryGetValue(position, out HexNavigationCell cell) || cell.TraversalMultiplier <= 1) continue;

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
