using System.Diagnostics.CodeAnalysis;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 提供以六边形格心为搜索节点的 A* 寻路.
/// 此基线算法将包含格心障碍的格子视为不可作为节点经过, 适用于验证地图可达性和地形代价.
/// </summary>
public static class HexGridAStar
{
    /// <summary>
    /// 在指定不可变导航快照中尝试查找从起点格心到终点格心的最低成本路径.
    /// 每一步的移动成本等于相邻格心距离乘以进入格子的地形倍率.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="path">查找成功时得到的离散格心路径.</param>
    /// <returns>当起终点均可作为节点且存在可达路径时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="snapshot"/> 或 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当快照的最小地形倍率无效时抛出.</exception>
    public static bool TryFindPath(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        [NotNullWhen(true)] out HexGridPath? path)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(layout);

        if (!double.IsFinite(snapshot.MinimumTraversalMultiplier) || snapshot.MinimumTraversalMultiplier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(snapshot));
        }

        if (!TryGetTraversableCell(snapshot, start, out _) || !TryGetTraversableCell(snapshot, goal, out _))
        {
            path = null;
            return false;
        }

        if (start == goal)
        {
            path = new HexGridPath([start], 0);
            return true;
        }

        double stepLength = 2 * layout.UnitApothem;
        Dictionary<HexagonalCubePoint, NodeRecord> records = new Dictionary<HexagonalCubePoint, NodeRecord>();
        PriorityQueue<OpenNode, double> openSet = new PriorityQueue<OpenNode, double>();
        HashSet<HexagonalCubePoint> closedSet = new HashSet<HexagonalCubePoint>();
        records.Add(start, new NodeRecord(0, default, false));
        openSet.Enqueue(new OpenNode(start, 0), GetHeuristicCost(start, goal, stepLength, snapshot.MinimumTraversalMultiplier));

        while (openSet.TryDequeue(out OpenNode openNode, out _))
        {
            if (!records.TryGetValue(openNode.Point, out NodeRecord currentRecord)
                || openNode.Cost != currentRecord.Cost
                || !closedSet.Add(openNode.Point))
            {
                continue;
            }

            if (openNode.Point == goal)
            {
                path = ReconstructPath(records, goal, currentRecord.Cost);
                return true;
            }

            for (int direction = 0; direction < 6; direction++)
            {
                HexagonalCubePoint neighbor = openNode.Point.NeighborAtUnchecked(direction);
                if (closedSet.Contains(neighbor) || !TryGetTraversableCell(snapshot, neighbor, out HexNavigationCell cell))
                {
                    continue;
                }

                double neighborCost = currentRecord.Cost + stepLength * cell.TraversalMultiplier;
                if (records.TryGetValue(neighbor, out NodeRecord existingRecord) && neighborCost >= existingRecord.Cost)
                {
                    continue;
                }

                records[neighbor] = new NodeRecord(neighborCost, openNode.Point, true);
                double priority = neighborCost + GetHeuristicCost(
                    neighbor,
                    goal,
                    stepLength,
                    snapshot.MinimumTraversalMultiplier);
                openSet.Enqueue(new OpenNode(neighbor, neighborCost), priority);
            }
        }

        path = null;
        return false;
    }

    private static bool TryGetTraversableCell(
        IHexNavigationSnapshot snapshot,
        HexagonalCubePoint point,
        out HexNavigationCell cell)
    {
        return snapshot.TryGetCell(point, out cell) && !cell.HasObstacle;
    }

    private static double GetHeuristicCost(
        HexagonalCubePoint point,
        HexagonalCubePoint goal,
        double stepLength,
        double minimumTraversalMultiplier)
    {
        return point.DistanceTo(goal) * stepLength * minimumTraversalMultiplier;
    }

    private static HexGridPath ReconstructPath(
        IReadOnlyDictionary<HexagonalCubePoint, NodeRecord> records,
        HexagonalCubePoint goal,
        double cost)
    {
        List<HexagonalCubePoint> points = [];
        HexagonalCubePoint current = goal;

        while (true)
        {
            points.Add(current);
            NodeRecord record = records[current];
            if (!record.HasParent) break;
            current = record.Parent;
        }

        points.Reverse();
        return new HexGridPath([.. points], cost);
    }

    private readonly record struct OpenNode(HexagonalCubePoint Point, double Cost);

    private readonly record struct NodeRecord(double Cost, HexagonalCubePoint Parent, bool HasParent);
}
