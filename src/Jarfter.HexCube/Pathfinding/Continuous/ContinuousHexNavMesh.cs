using System.Collections.ObjectModel;
using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示在有限六边形边界内构建的规则六边形 NavMesh.
/// 每个单元均完全位于边界内且不与扩大后的障碍物相交, 因此可作为后续 Portal 图搜索的保守自由空间面.
/// </summary>
public sealed class ContinuousHexNavMesh
{
    private readonly int[] m_CellIndices;
    private readonly HexCubeGridPoint[] m_CellPositions;
    private readonly HexCubeArea2D[] m_Cells;
    private readonly ReadOnlyCollection<HexCubeArea2D> m_ReadOnlyCells;
    private readonly int m_GridRadius;
    private readonly int m_GridWidth;

    private ContinuousHexNavMesh(
        long revision,
        ContinuousNavigationBounds bounds,
        double agentRadius,
        double clearance,
        double cellSpacing,
        int gridRadius,
        int[] cellIndices,
        HexCubeGridPoint[] cellPositions,
        HexCubeArea2D[] cells)
    {
        Revision = revision;
        Bounds = bounds;
        AgentRadius = agentRadius;
        Clearance = clearance;
        CellSpacing = cellSpacing;
        m_GridRadius = gridRadius;
        m_GridWidth = gridRadius * 2 + 1;
        m_CellIndices = cellIndices;
        m_CellPositions = cellPositions;
        m_Cells = cells;
        m_ReadOnlyCells = Array.AsReadOnly(cells);
    }

    /// <summary>
    /// 获取构建此 NavMesh 时使用的地图版本.
    /// </summary>
    public long Revision { get; }

    /// <summary>
    /// 获取 NavMesh 的有限工作边界.
    /// </summary>
    public ContinuousNavigationBounds Bounds { get; }

    /// <summary>
    /// 获取构建时已经计入障碍物膨胀的移动单位半径.
    /// </summary>
    public double AgentRadius { get; }

    /// <summary>
    /// 获取构建时已经计入障碍物膨胀的额外安全距离.
    /// </summary>
    public double Clearance { get; }

    /// <summary>
    /// 获取相邻 NavMesh 单元中心的六边形坐标间距.
    /// </summary>
    public double CellSpacing { get; }

    /// <summary>
    /// 获取可通行六边形单元.
    /// 集合顺序与内部节点索引一致, 仅供只读查询.
    /// </summary>
    public IReadOnlyList<HexCubeArea2D> Cells => m_ReadOnlyCells;

    /// <summary>
    /// 根据不可变地图快照构建规则六边形 NavMesh.
    /// 规则单元会完全落在 <paramref name="bounds"/> 内, 并避开已按单位半径和安全距离扩大的障碍物.
    /// </summary>
    /// <param name="snapshot">用于构建的不可变地图快照.</param>
    /// <param name="bounds">限制构建范围的有限导航边界.</param>
    /// <param name="agentRadius">移动单位半径.</param>
    /// <param name="clearance">额外安全距离.</param>
    /// <param name="cellSpacing">相邻单元中心的间距. 必须为有限正数.</param>
    /// <returns>与输入快照版本绑定的不可变 NavMesh.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当单位半径、安全距离或单元间距不合法时抛出.</exception>
    public static ContinuousHexNavMesh Build(
        IContinuousNavigationSnapshot snapshot,
        ContinuousNavigationBounds bounds,
        double agentRadius,
        double clearance,
        double cellSpacing = 1)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(bounds);
        ValidateBuildArguments(agentRadius, clearance, cellSpacing);

        double cellRadius = cellSpacing / 2;
        double availableRadius = bounds.Shape.RadiusScale - cellRadius;
        int gridRadius = availableRadius > 0 ? (int)Math.Floor(availableRadius / cellSpacing) : 0;
        int gridWidth = gridRadius * 2 + 1;
        int candidateCellCount = gridWidth * gridWidth;
        int[] cellIndices = new int[candidateCellCount];
        List<HexCubeGridPoint> cellPositions = new List<HexCubeGridPoint>(candidateCellCount);
        List<HexCubeArea2D> cells = new List<HexCubeArea2D>(candidateCellCount);
        double expansion = agentRadius + clearance;
        ContinuousNavigationSnapshot? indexedSnapshot = snapshot as ContinuousNavigationSnapshot;

        for (int q = -gridRadius; q <= gridRadius; q++)
        {
            for (int r = -gridRadius; r <= gridRadius; r++)
            {
                HexCubeGridPoint gridPosition = new HexCubeGridPoint(q, r);
                HexCubeArea2D cell = new HexCubeArea2D(
                    bounds.Shape.Position + new HexCubePoint(q * cellSpacing, r * cellSpacing),
                    cellRadius);

                if (!bounds.Shape.Contains(cell)) continue;

                bool intersectsObstacle = indexedSnapshot is not null
                    ? indexedSnapshot.IntersectsExpandedObstacle(cell, expansion)
                    : IntersectsExpandedObstacle(cell, snapshot.Obstacles, expansion);

                if (intersectsObstacle) continue;

                cellIndices[GetCellIndexOffset(gridPosition, gridRadius, gridWidth)] = cells.Count + 1;
                cellPositions.Add(gridPosition);
                cells.Add(cell);
            }
        }

        return new ContinuousHexNavMesh(
            snapshot.Revision,
            bounds,
            agentRadius,
            clearance,
            cellSpacing,
            gridRadius,
            cellIndices,
            [.. cellPositions],
            [.. cells]);
    }

    /// <summary>
    /// 尝试获取包含指定连续位置的可通行单元索引.
    /// </summary>
    /// <param name="position">要定位的连续位置.</param>
    /// <param name="cellIndex">找到时返回单元索引.</param>
    /// <returns>位置位于可通行单元内或边界上时返回 true, 否则返回 false.</returns>
    public bool TryGetContainingCellIndex(HexCubePoint position, out int cellIndex)
    {
        HexCubePoint normalizedPosition = (position - Bounds.Shape.Position) / CellSpacing;
        HexCubeGridPoint nearestPosition = normalizedPosition.AsRound();

        if (TryGetContainingCellIndex(nearestPosition, position, out cellIndex)) return true;

        for (int direction = 0; direction < 6; direction++)
        {
            if (TryGetContainingCellIndex(nearestPosition.NeighborAtUnchecked(direction), position, out cellIndex)) return true;
        }

        // 仅在边界附近的浮点舍入异常时退回完整扫描, 保持与原始实现一致的包含语义.
        for (int index = 0; index < m_Cells.Length; index++)
        {
            if (!m_Cells[index].Contains(position)) continue;

            cellIndex = index;
            return true;
        }

        cellIndex = -1;
        return false;
    }

    /// <summary>
    /// 尝试获取指定单元在给定六边形方向上的相邻单元索引.
    /// </summary>
    /// <param name="cellIndex">当前单元索引.</param>
    /// <param name="direction">相邻方向, 范围为 [0, 5].</param>
    /// <param name="neighborIndex">找到时返回相邻单元索引.</param>
    /// <returns>存在对应的可通行相邻单元时返回 true, 否则返回 false.</returns>
    internal bool TryGetNeighborIndex(int cellIndex, int direction, out int neighborIndex)
    {
        HexCubeGridPoint neighborPosition = m_CellPositions[cellIndex].NeighborAtUnchecked(direction);
        return TryGetCellIndex(neighborPosition, out neighborIndex);
    }

    /// <summary>
    /// 获取指定单元的六边形区域.
    /// </summary>
    /// <param name="cellIndex">单元索引.</param>
    /// <returns>指定的可通行单元区域.</returns>
    internal HexCubeArea2D GetCell(int cellIndex) => m_Cells[cellIndex];

    /// <summary>
    /// 获取当前单元沿指定方向通向相邻单元的共享 Portal.
    /// </summary>
    /// <param name="cellIndex">当前单元索引.</param>
    /// <param name="direction">相邻方向, 范围为 [0, 5].</param>
    /// <returns>当前单元与该方向相邻单元共享的边.</returns>
    internal HexCubeLine2D GetPortal(int cellIndex, int direction)
    {
        HexCubeArea2D cell = m_Cells[cellIndex];
        int nextVertexIndex = direction == 5 ? 0 : direction + 1;
        return new HexCubeLine2D(cell.GetVertex(direction), cell.GetVertex(nextVertexIndex));
    }

    private static bool IntersectsExpandedObstacle(
        HexCubeArea2D cell,
        IReadOnlyList<HexCubeArea2D> obstacles,
        double expansion)
    {
        foreach (HexCubeArea2D obstacle in obstacles)
        {
            HexCubePoint delta = cell.Position - obstacle.Position;
            double combinedRadius = cell.RadiusScale + obstacle.RadiusScale + expansion;

            if (Math.Abs(delta.Q) <= combinedRadius && Math.Abs(delta.R) <= combinedRadius && Math.Abs(delta.S) <= combinedRadius)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetContainingCellIndex(HexCubeGridPoint gridPosition, HexCubePoint position, out int cellIndex)
    {
        return TryGetCellIndex(gridPosition, out cellIndex) && m_Cells[cellIndex].Contains(position);
    }

    private bool TryGetCellIndex(HexCubeGridPoint gridPosition, out int cellIndex)
    {
        int q = gridPosition.Q + m_GridRadius;
        int r = gridPosition.R + m_GridRadius;

        if ((uint)q >= (uint)m_GridWidth || (uint)r >= (uint)m_GridWidth)
        {
            cellIndex = -1;
            return false;
        }

        cellIndex = m_CellIndices[q * m_GridWidth + r] - 1;
        return cellIndex >= 0;
    }

    private static int GetCellIndexOffset(HexCubeGridPoint gridPosition, int gridRadius, int gridWidth)
    {
        return (gridPosition.Q + gridRadius) * gridWidth + gridPosition.R + gridRadius;
    }

    private static void ValidateBuildArguments(double agentRadius, double clearance, double cellSpacing)
    {
        if (!(agentRadius >= 0) || !double.IsFinite(agentRadius))
        {
            throw new ArgumentOutOfRangeException(nameof(agentRadius), agentRadius, "Agent radius must be a finite non-negative number.");
        }

        if (!(clearance >= 0) || !double.IsFinite(clearance))
        {
            throw new ArgumentOutOfRangeException(nameof(clearance), clearance, "Clearance must be a finite non-negative number.");
        }

        if (!(cellSpacing > 0) || !double.IsFinite(cellSpacing))
        {
            throw new ArgumentOutOfRangeException(nameof(cellSpacing), cellSpacing, "Cell spacing must be a finite positive number.");
        }

        if (!double.IsFinite(agentRadius + clearance))
        {
            throw new ArgumentOutOfRangeException(nameof(clearance), clearance, "Expanded radius must be finite.");
        }
    }
}
