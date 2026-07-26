using System.Collections.ObjectModel;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 提供连续导航地图的不可变障碍物快照和基于 Q 轴分桶的空间索引.
/// 空间索引仅用于减少精确六边形检测次数, 所有通行结论仍由 <see cref="HexCubeArea2D"/> 的精确几何判断得出.
/// </summary>
public sealed class ContinuousNavigationSnapshot : IContinuousNavigationSnapshot
{
    private readonly Dictionary<int, HexCubeArea2D[]> m_AreasByQ;
    private readonly Dictionary<int, ContinuousTraversalArea[]> m_TraversalAreasByQ;
    private readonly ReadOnlyCollection<HexCubeArea2D> m_ReadOnlyObstacles;
    private readonly ReadOnlyCollection<ContinuousTraversalArea> m_ReadOnlyTraversalAreas;
    private readonly double m_MaxRadius;
    private readonly double m_MaxTraversalRadius;

    /// <summary>
    /// 获取不含障碍物和高代价区域的初始地图快照.
    /// </summary>
    public static ContinuousNavigationSnapshot Empty { get; } = new ContinuousNavigationSnapshot(0, [], []);

    /// <summary>
    /// 使用指定障碍物和高代价区域创建不可变导航快照.
    /// 输入集合会在构造时复制, 后续修改原集合不会影响本实例.
    /// </summary>
    /// <param name="revision">快照对应的地图版本.</param>
    /// <param name="obstacles">要复制到快照中的基础障碍物区域.</param>
    /// <param name="traversalAreas">要复制到快照中的高代价区域.</param>
    /// <exception cref="ArgumentOutOfRangeException">当区域坐标、半径或高代价倍率不合法时抛出.</exception>
    public ContinuousNavigationSnapshot(
        long revision,
        IReadOnlyList<HexCubeArea2D> obstacles,
        IReadOnlyList<ContinuousTraversalArea> traversalAreas)
    {
        ArgumentNullException.ThrowIfNull(obstacles);
        ArgumentNullException.ThrowIfNull(traversalAreas);
        ArgumentOutOfRangeException.ThrowIfNegative(revision);

        Revision = revision;
        HexCubeArea2D[] obstacleArray = [.. obstacles];
        m_ReadOnlyObstacles = Array.AsReadOnly(obstacleArray);
        Dictionary<int, List<HexCubeArea2D>> areasByQ = [];
        double maxRadius = 0;

        foreach (HexCubeArea2D area in obstacleArray)
        {
            ValidateArea(area);
            int qBucket = (int)Math.Floor(area.Position.Q);

            if (!areasByQ.TryGetValue(qBucket, out List<HexCubeArea2D>? areas))
            {
                areas = [];
                areasByQ.Add(qBucket, areas);
            }

            areas.Add(area);
            maxRadius = Math.Max(maxRadius, area.RadiusScale);
        }

        m_AreasByQ = new Dictionary<int, HexCubeArea2D[]>(areasByQ.Count);

        foreach ((int q, List<HexCubeArea2D> areas) in areasByQ)
        {
            m_AreasByQ.Add(q, [.. areas]);
        }

        m_MaxRadius = maxRadius;
        ContinuousTraversalArea[] traversalAreaArray = [.. traversalAreas];
        m_ReadOnlyTraversalAreas = Array.AsReadOnly(traversalAreaArray);
        Dictionary<int, List<ContinuousTraversalArea>> traversalAreasByQ = [];
        double maxTraversalRadius = 0;

        foreach (ContinuousTraversalArea area in traversalAreaArray)
        {
            ValidateTraversalArea(area);
            int qBucket = (int)Math.Floor(area.Shape.Position.Q);

            if (!traversalAreasByQ.TryGetValue(qBucket, out List<ContinuousTraversalArea>? areas))
            {
                areas = [];
                traversalAreasByQ.Add(qBucket, areas);
            }

            areas.Add(area);
            maxTraversalRadius = Math.Max(maxTraversalRadius, area.Shape.RadiusScale);
        }

        m_TraversalAreasByQ = new Dictionary<int, ContinuousTraversalArea[]>(traversalAreasByQ.Count);

        foreach ((int q, List<ContinuousTraversalArea> areas) in traversalAreasByQ)
        {
            m_TraversalAreasByQ.Add(q, [.. areas]);
        }

        m_MaxTraversalRadius = maxTraversalRadius;
    }

    /// <inheritdoc />
    public long Revision { get; }

    /// <inheritdoc />
    public IReadOnlyList<HexCubeArea2D> Obstacles => m_ReadOnlyObstacles;

    /// <inheritdoc />
    public IReadOnlyList<ContinuousTraversalArea> TraversalAreas => m_ReadOnlyTraversalAreas;

    /// <inheritdoc />
    public bool UsesUniformTraversalCost => m_TraversalAreasByQ.Count == 0;

    /// <inheritdoc />
    public bool IsPositionNavigable(HexCubePoint position, double agentRadius, double clearance)
    {
        double expansion = ValidateExpansion(agentRadius, clearance);
        double maximumRadius = m_MaxRadius + expansion;
        int firstQ = (int)Math.Floor(position.Q - maximumRadius);
        int lastQ = (int)Math.Floor(position.Q + maximumRadius);

        for (int q = firstQ; q <= lastQ; q++)
        {
            if (!m_AreasByQ.TryGetValue(q, out HexCubeArea2D[]? areas)) continue;

            foreach (HexCubeArea2D area in areas)
            {
                HexCubeArea2D effectiveArea = area.Inflate(expansion);
                HexCubePoint obstaclePosition = effectiveArea.Position;
                double radius = effectiveArea.RadiusScale;

                if (Math.Abs(position.R - obstaclePosition.R) > radius || Math.Abs(position.S - obstaclePosition.S) > radius) continue;
                if (effectiveArea.Contains(position)) return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public bool HasLineOfSight(HexCubeLine2D line, double agentRadius, double clearance)
    {
        double expansion = ValidateExpansion(agentRadius, clearance);
        double maximumRadius = m_MaxRadius + expansion;
        HexCubeBounds2D lineBounds = HexCubeBounds2D.FromLine(line);
        int firstQ = (int)Math.Floor(lineBounds.MinimumQ - maximumRadius);
        int lastQ = (int)Math.Floor(lineBounds.MaximumQ + maximumRadius);

        for (int q = firstQ; q <= lastQ; q++)
        {
            if (!m_AreasByQ.TryGetValue(q, out HexCubeArea2D[]? areas)) continue;

            foreach (HexCubeArea2D area in areas)
            {
                HexCubePoint obstaclePosition = area.Position;
                double radius = area.RadiusScale + expansion;

                // Q 轴已由索引粗筛, 仍需在三个轴上按实际半径收紧范围, 避免构造临时包围盒.
                if (IsOutsideBounds(lineBounds, obstaclePosition, radius)) continue;
                if (new HexCubeArea2D(obstaclePosition, radius).IntersectsInterior(line)) return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public double GetLineCost(HexCubeLine2D line)
    {
        double lineLength = line.Length;
        double cost = lineLength;

        if (UsesUniformTraversalCost) return cost;

        HexCubeBounds2D lineBounds = HexCubeBounds2D.FromLine(line);
        int firstQ = (int)Math.Floor(lineBounds.MinimumQ - m_MaxTraversalRadius);
        int lastQ = (int)Math.Floor(lineBounds.MaximumQ + m_MaxTraversalRadius);

        for (int q = firstQ; q <= lastQ; q++)
        {
            if (!m_TraversalAreasByQ.TryGetValue(q, out ContinuousTraversalArea[]? areas)) continue;

            foreach (ContinuousTraversalArea area in areas)
            {
                HexCubeArea2D shape = area.Shape;

                if (IsOutsideBounds(lineBounds, shape.Position, shape.RadiusScale)) continue;
                if (!shape.TryGetIntersectionRange(line, out double tMin, out double tMax)) continue;

                cost += lineLength * (tMax - tMin) * (area.TraversalMultiplier - 1);
            }
        }

        return cost;
    }

    /// <summary>
    /// 使用 Q 轴空间索引判断 NavMesh 单元是否与扩张后的任意障碍物相交.
    /// 精确相交判定仍与未建立索引时的逐障碍物判断完全一致.
    /// </summary>
    /// <param name="cell">待判定的 NavMesh 单元.</param>
    /// <param name="expansion">障碍物的额外扩张距离.</param>
    /// <returns>存在相交的扩张障碍物时返回 true, 否则返回 false.</returns>
    internal bool IntersectsExpandedObstacle(HexCubeArea2D cell, double expansion)
    {
        double maximumDistance = cell.RadiusScale + m_MaxRadius + expansion;
        int firstQ = (int)Math.Floor(cell.Position.Q - maximumDistance);
        int lastQ = (int)Math.Floor(cell.Position.Q + maximumDistance);

        for (int q = firstQ; q <= lastQ; q++)
        {
            if (!m_AreasByQ.TryGetValue(q, out HexCubeArea2D[]? areas)) continue;

            foreach (HexCubeArea2D obstacle in areas)
            {
                HexCubePoint delta = cell.Position - obstacle.Position;
                double combinedRadius = cell.RadiusScale + obstacle.RadiusScale + expansion;

                if (Math.Abs(delta.Q) <= combinedRadius && Math.Abs(delta.R) <= combinedRadius && Math.Abs(delta.S) <= combinedRadius)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsOutsideBounds(HexCubeBounds2D lineBounds, HexCubePoint position, double radius)
    {
        return lineBounds.MaximumQ < position.Q - radius || lineBounds.MinimumQ > position.Q + radius ||
               lineBounds.MaximumR < position.R - radius || lineBounds.MinimumR > position.R + radius ||
               lineBounds.MaximumS < position.S - radius || lineBounds.MinimumS > position.S + radius;
    }

    private static double ValidateExpansion(double agentRadius, double clearance)
    {
        if (!(agentRadius >= 0) || !double.IsFinite(agentRadius))
        {
            throw new ArgumentOutOfRangeException(nameof(agentRadius), agentRadius, "Agent radius must be a finite non-negative number.");
        }

        if (!(clearance >= 0) || !double.IsFinite(clearance))
        {
            throw new ArgumentOutOfRangeException(nameof(clearance), clearance, "Clearance must be a finite non-negative number.");
        }

        double expansion = agentRadius + clearance;

        if (!double.IsFinite(expansion))
        {
            throw new ArgumentOutOfRangeException(nameof(clearance), clearance, "Expanded radius must be finite.");
        }

        return expansion;
    }

    private static void ValidateArea(HexCubeArea2D area)
    {
        HexCubePoint position = area.Position;

        if (!double.IsFinite(position.Q) || !double.IsFinite(position.R) || !(area.RadiusScale >= 0) || !double.IsFinite(area.RadiusScale))
        {
            throw new ArgumentOutOfRangeException(nameof(area), area, "Obstacle position and radius must be finite, and radius must be non-negative.");
        }
    }

    private static void ValidateTraversalArea(ContinuousTraversalArea area)
    {
        ValidateArea(area.Shape);

        if (!(area.TraversalMultiplier > 1) || !double.IsFinite(area.TraversalMultiplier))
        {
            throw new ArgumentOutOfRangeException(nameof(area), area, "Traversal multiplier must be a finite number greater than one.");
        }
    }
}
