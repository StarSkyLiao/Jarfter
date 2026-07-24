using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Pathfinding.Grid;

/// <summary>
/// 从稀疏记录或工作区父节点记录还原不可变格心路径结果.
/// </summary>
internal static class HexGridPathReconstructor
{
    /// <summary>
    /// 从稀疏搜索记录还原路径.
    /// </summary>
    internal static HexGridPath ReconstructSparsePath(
        IReadOnlyDictionary<HexagonalCubePoint, HexGridSearch.SparseNodeRecord> records,
        HexagonalCubePoint goal,
        double cost,
        long navigationVersion,
        HexPathfindingStatisticsCollector? statisticsCollector)
    {
        List<HexagonalCubePoint> points = [];
        HexagonalCubePoint current = goal;

        while (true)
        {
            points.Add(current);
            HexGridSearch.SparseNodeRecord record = records[current];
            if (!record.HasParent)
            {
                break;
            }

            current = record.Parent;
        }

        points.Reverse();
        return new HexGridPath([.. points], cost, navigationVersion, statisticsCollector?.CreateStatistics());
    }

    /// <summary>
    /// 从中心稠密工作区的父节点索引还原路径.
    /// </summary>
    internal static HexGridPath ReconstructBakedPath(
        HexGridPathfindingWorkspace workspace,
        int goalIndex,
        double cost,
        long navigationVersion,
        HexPathfindingStatisticsCollector? statisticsCollector)
    {
        int count = 1;
        int currentIndex = goalIndex;

        while (workspace.TryGetRecord(currentIndex, out _, out int parentIndex) && parentIndex >= 0)
        {
            count++;
            currentIndex = parentIndex;
        }

        HexagonalCubePoint[] points = new HexagonalCubePoint[count];
        currentIndex = goalIndex;

        for (int index = count - 1; index >= 0; index--)
        {
            points[index] = workspace.Bake.GetPoint(currentIndex);
            workspace.TryGetRecord(currentIndex, out _, out currentIndex);
        }

        return new HexGridPath(points, cost, navigationVersion, statisticsCollector?.CreateStatistics());
    }
}
