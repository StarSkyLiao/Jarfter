using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 提供以任意坐标六边形障碍物构成的可变连续导航地图.
/// 障碍物变更会记录受影响区域并使当前快照失效; 下一次捕获快照或校验旧路径时才重建索引, 因此连续批量更新不会重复构建相同版本的快照.
/// </summary>
public sealed class ContinuousNavigationMap : IContinuousNavigationMap
{
    private const int DefaultChangeHistoryCapacity = 1024;

    private readonly object m_Sync = new object();
    private readonly Dictionary<long, HexCubeArea2D> m_Obstacles = [];
    private readonly Dictionary<long, ContinuousTraversalArea> m_TraversalAreas = [];
    private readonly Queue<ContinuousMapChange> m_Changes = new Queue<ContinuousMapChange>();
    private readonly int m_ChangeHistoryCapacity;
    private ContinuousNavigationSnapshot m_Snapshot = ContinuousNavigationSnapshot.Empty;
    private bool m_IsSnapshotDirty;
    private long m_CurrentRevision;

    /// <summary>
    /// 使用指定的变更记录容量创建连续导航地图.
    /// 超过容量的旧变更会被移除, 依赖该范围之前地图版本的路径校验将返回 <see cref="ContinuousPathValidationResult.Unknown"/>.
    /// </summary>
    /// <param name="changeHistoryCapacity">保留的地图变更记录数量. 必须为正数.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="changeHistoryCapacity"/> 不为正数时抛出.</exception>
    public ContinuousNavigationMap(int changeHistoryCapacity = DefaultChangeHistoryCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(changeHistoryCapacity);
        m_ChangeHistoryCapacity = changeHistoryCapacity;
    }

    /// <inheritdoc />
    public event Action<ContinuousMapChange>? Changed;

    /// <inheritdoc />
    public long CurrentRevision
    {
        get
        {
            lock (m_Sync)
            {
                return m_CurrentRevision;
            }
        }
    }

    /// <summary>
    /// 新增或替换指定标识的障碍物.
    /// </summary>
    /// <param name="id">障碍物的稳定标识.</param>
    /// <param name="area">障碍物的连续六边形区域.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="area"/> 的坐标或半径不合法时抛出.</exception>
    public void SetObstacle(long id, HexCubeArea2D area)
    {
        ValidateArea(area, nameof(area));
        ContinuousMapChange change;

        lock (m_Sync)
        {
            HexCubeBounds2D changedBounds = area.Bounds;

            if (m_Obstacles.TryGetValue(id, out HexCubeArea2D previous))
            {
                changedBounds = changedBounds.Union(previous.Bounds);
            }

            m_Obstacles[id] = area;
            change = CommitChange(changedBounds);
        }

        Changed?.Invoke(change);
    }

    /// <summary>
    /// 移除指定标识的障碍物.
    /// </summary>
    /// <param name="id">要移除的障碍物标识.</param>
    /// <returns>找到并移除障碍物时返回 true, 否则返回 false.</returns>
    public bool RemoveObstacle(long id)
    {
        ContinuousMapChange change;

        lock (m_Sync)
        {
            if (!m_Obstacles.Remove(id, out HexCubeArea2D previous)) return false;
            change = CommitChange(previous.Bounds);
        }

        Changed?.Invoke(change);
        return true;
    }

    /// <inheritdoc />
    public void SetTraversalArea(long id, HexCubeArea2D area, double traversalMultiplier)
    {
        ValidateArea(area, nameof(area));
        ValidateTraversalMultiplier(traversalMultiplier);
        ContinuousMapChange change;

        lock (m_Sync)
        {
            HexCubeBounds2D changedBounds = area.Bounds;

            if (m_TraversalAreas.TryGetValue(id, out ContinuousTraversalArea previous))
            {
                changedBounds = changedBounds.Union(previous.Shape.Bounds);
            }

            m_TraversalAreas[id] = new ContinuousTraversalArea(area, traversalMultiplier);
            change = CommitChange(changedBounds);
        }

        Changed?.Invoke(change);
    }

    /// <inheritdoc />
    public bool RemoveTraversalArea(long id)
    {
        ContinuousMapChange change;

        lock (m_Sync)
        {
            if (!m_TraversalAreas.Remove(id, out ContinuousTraversalArea previous)) return false;
            change = CommitChange(previous.Shape.Bounds);
        }

        Changed?.Invoke(change);
        return true;
    }

    /// <inheritdoc />
    public IContinuousNavigationSnapshot CaptureSnapshot()
    {
        lock (m_Sync)
        {
            return GetCurrentSnapshot();
        }
    }

    /// <inheritdoc />
    public ContinuousPathValidationResult ValidateRemainingPath(ContinuousPathResult path, int remainingSegmentIndex)
    {
        if (remainingSegmentIndex < 0 || remainingSegmentIndex >= path.Path.Count - 1)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingSegmentIndex), remainingSegmentIndex, "Remaining segment index must identify an existing path segment.");
        }

        ContinuousNavigationSnapshot snapshot;
        ContinuousMapChange[] changes;

        lock (m_Sync)
        {
            if (path.MapRevision > m_CurrentRevision) return ContinuousPathValidationResult.Unknown;
            if (path.MapRevision == m_CurrentRevision) return ContinuousPathValidationResult.Valid;
            if (m_Changes.Count == 0 || m_Changes.Peek().Revision > path.MapRevision + 1) return ContinuousPathValidationResult.Unknown;

            snapshot = GetCurrentSnapshot();
            changes = [.. m_Changes.Where(change => change.Revision > path.MapRevision)];
        }

        IReadOnlyList<HexCubePoint> points = path.Path;

        for (int index = remainingSegmentIndex; index < points.Count - 1; index++)
        {
            HexCubeLine2D line = new HexCubeLine2D(points[index], points[index + 1]);
            HexCubeBounds2D lineBounds = HexCubeBounds2D.FromLine(line);

            foreach (ContinuousMapChange change in changes)
            {
                if (!lineBounds.Intersects(change.ChangedBounds.Expand(path.AgentRadius + path.Clearance))) continue;

                // 只在路径段经过脏区时做精确检测, 避免无关地图变化触发全路径重算.
                if (!snapshot.HasLineOfSight(line, path.AgentRadius, path.Clearance))
                {
                    return ContinuousPathValidationResult.Invalid;
                }

                break;
            }
        }

        return ContinuousPathValidationResult.Valid;
    }

    private ContinuousMapChange CommitChange(HexCubeBounds2D changedBounds)
    {
        m_CurrentRevision++;
        ContinuousMapChange change = new ContinuousMapChange(m_CurrentRevision, changedBounds);
        m_Changes.Enqueue(change);

        if (m_Changes.Count > m_ChangeHistoryCapacity)
        {
            m_Changes.Dequeue();
        }

        m_IsSnapshotDirty = true;
        return change;
    }

    /// <summary>
    /// 获取当前版本的快照, 并仅在障碍物变更后第一次需要快照时重建空间索引.
    /// 调用方持有的旧快照不会被修改, 因此可安全用于已经开始的路径搜索.
    /// </summary>
    private ContinuousNavigationSnapshot GetCurrentSnapshot()
    {
        if (!m_IsSnapshotDirty) return m_Snapshot;

        m_Snapshot = new ContinuousNavigationSnapshot(m_CurrentRevision, [.. m_Obstacles.Values], [.. m_TraversalAreas.Values]);
        m_IsSnapshotDirty = false;
        return m_Snapshot;
    }

    private static void ValidateArea(HexCubeArea2D area, string parameterName)
    {
        HexCubePoint position = area.Position;

        if (!double.IsFinite(position.Q) || !double.IsFinite(position.R) || !(area.RadiusScale >= 0) || !double.IsFinite(area.RadiusScale))
        {
            throw new ArgumentOutOfRangeException(parameterName, area, "Obstacle position and radius must be finite, and radius must be non-negative.");
        }
    }

    private static void ValidateTraversalMultiplier(double traversalMultiplier)
    {
        if (!(traversalMultiplier > 1) || !double.IsFinite(traversalMultiplier))
        {
            throw new ArgumentOutOfRangeException(nameof(traversalMultiplier), traversalMultiplier, "Traversal multiplier must be a finite number greater than one.");
        }
    }
}
