using System.Diagnostics.CodeAnalysis;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 提供以六边形格心为搜索节点的 Theta* 寻路.
/// 算法通过父节点的直接视线连接减少不必要的中间航点, 并使用实际穿格长度累计地形成本.
/// </summary>
public static class HexGridThetaStar
{
    /// <summary>
    /// 在指定不可变导航快照中尝试查找从起点格心到终点格心的低成本可见路径.
    /// 起点和终点必须是无格心障碍的地图格子, 返回路径中的相邻节点均具有可通行视线.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">起点格心坐标.</param>
    /// <param name="goal">终点格心坐标.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="path">查找成功时得到的格心航点路径.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <returns>当起终点均可作为节点且存在可达路径时返回 <see langword="true"/>; 否则返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="snapshot"/> 或 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当足迹、边距或快照最小地形倍率无效时抛出.</exception>
    public static bool TryFindPath(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        [NotNullWhen(true)] out HexGridPath? path,
        double clearanceApothemScale = 0)
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

        HexagonalWorldPoint goalCenter = layout.GetCenter(goal);
        Dictionary<HexagonalCubePoint, NodeRecord> records = new Dictionary<HexagonalCubePoint, NodeRecord>();
        PriorityQueue<OpenNode, double> openSet = new PriorityQueue<OpenNode, double>();
        HashSet<HexagonalCubePoint> closedSet = new HashSet<HexagonalCubePoint>();
        records.Add(start, new NodeRecord(0, default, false));
        openSet.Enqueue(
            new OpenNode(start, 0),
            GetHeuristicCost(layout.GetCenter(start), goalCenter, snapshot.MinimumTraversalMultiplier));

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
                if (closedSet.Contains(neighbor) || !TryGetTraversableCell(snapshot, neighbor, out _))
                {
                    continue;
                }

                if (!TryGetBestConnection(
                        snapshot,
                        layout,
                        records,
                        openNode.Point,
                        currentRecord,
                        neighbor,
                        footprint,
                        clearanceApothemScale,
                        out HexagonalCubePoint parent,
                        out double neighborCost))
                {
                    continue;
                }

                if (records.TryGetValue(neighbor, out NodeRecord existingRecord) && neighborCost >= existingRecord.Cost)
                {
                    continue;
                }

                records[neighbor] = new NodeRecord(neighborCost, parent, true);
                HexagonalWorldPoint neighborCenter = layout.GetCenter(neighbor);
                double priority = neighborCost + GetHeuristicCost(
                    neighborCenter,
                    goalCenter,
                    snapshot.MinimumTraversalMultiplier);
                openSet.Enqueue(new OpenNode(neighbor, neighborCost), priority);
            }
        }

        path = null;
        return false;
    }

    private static bool TryGetBestConnection(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        IReadOnlyDictionary<HexagonalCubePoint, NodeRecord> records,
        HexagonalCubePoint current,
        NodeRecord currentRecord,
        HexagonalCubePoint neighbor,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        out HexagonalCubePoint parent,
        out double cost)
    {
        HexagonalCubePoint connectionStart = current;
        double connectionStartCost = currentRecord.Cost;

        if (currentRecord.HasParent)
        {
            NodeRecord parentRecord = records[currentRecord.Parent];
            connectionStart = currentRecord.Parent;
            connectionStartCost = parentRecord.Cost;
        }

        if (HexLineOfSight.TryGetTraversalCost(
                snapshot,
                layout,
                layout.GetCenter(connectionStart),
                layout.GetCenter(neighbor),
                footprint,
                out double connectionCost,
                clearanceApothemScale))
        {
            parent = connectionStart;
            cost = connectionStartCost + connectionCost;
            return true;
        }

        if (connectionStart != current
            && HexLineOfSight.TryGetTraversalCost(
                snapshot,
                layout,
                layout.GetCenter(current),
                layout.GetCenter(neighbor),
                footprint,
                out connectionCost,
                clearanceApothemScale))
        {
            parent = current;
            cost = currentRecord.Cost + connectionCost;
            return true;
        }

        parent = default;
        cost = 0;
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
        HexagonalWorldPoint point,
        HexagonalWorldPoint goal,
        double minimumTraversalMultiplier)
    {
        return point.DistanceTo(goal) * minimumTraversalMultiplier;
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
