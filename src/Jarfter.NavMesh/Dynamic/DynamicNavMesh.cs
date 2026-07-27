using Jarfter.Core.Collections.ObjectModel;
using Jarfter.NavMesh.Build;
using Jarfter.NavMesh.Geometry;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.Dynamic;

/// <summary>
/// 管理运行时障碍物并发布不可变导航网格快照.
/// 每次障碍物变更都会完整重建, 因而适合低频动态障碍物或 tile 级重建场景.
/// 修改操作应由单一写入线程串行调用, 已取得的 Snapshot 可继续安全读取.
/// </summary>
public sealed class DynamicNavMesh
{
    private readonly NavMeshPolygon m_Boundary;
    private readonly NavMeshBuildOptions m_Options;
    private readonly Dictionary<int, NavMeshPolygon> m_Obstacles = new Dictionary<int, NavMeshPolygon>();
    private int m_NextObstacleId;

    /// <summary>
    /// 使用初始构建输入创建动态导航网格并立即构建首个快照.
    /// </summary>
    /// <param name="input">外边界与初始障碍物.</param>
    /// <param name="options">构建和移动对象半径选项.</param>
    public DynamicNavMesh(NavMeshBuildInput input, NavMeshBuildOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        m_Boundary = input.Boundary;
        m_Options = options ?? new NavMeshBuildOptions();
        foreach (NavMeshPolygon polygon in input.Obstacles)
            m_Obstacles.Add(m_NextObstacleId++, polygon);
        Snapshot = BuildSnapshot();
    }

    /// <summary>
    /// 获取当前可供查询的不可变导航网格快照.
    /// </summary>
    public Mesh Snapshot { get; private set; }

    /// <summary>
    /// 获取当前动态障碍物数量.
    /// </summary>
    public int ObstacleCount => m_Obstacles.Count;

    /// <summary>
    /// 添加障碍物并发布新快照.
    /// 构建失败时保留当前快照且不添加障碍物.
    /// </summary>
    /// <param name="obstacle">需从可行走区域扣除的障碍物环.</param>
    /// <returns>可用于后续替换或移除障碍物的标识.</returns>
    public int AddObstacle(NavMeshPolygon obstacle)
    {
        ArgumentNullException.ThrowIfNull(obstacle);
        if (m_NextObstacleId == int.MaxValue) throw new InvalidOperationException("动态障碍物标识已耗尽.");
        int obstacleId = m_NextObstacleId;
        m_Obstacles.Add(obstacleId, obstacle);
        try
        {
            PublishRebuiltSnapshot();
            m_NextObstacleId++;
            return obstacleId;
        }
        catch
        {
            m_Obstacles.Remove(obstacleId);
            throw;
        }
    }

    /// <summary>
    /// 替换指定障碍物并发布新快照.
    /// 构建失败时保留原障碍物和当前快照.
    /// </summary>
    /// <param name="obstacleId">待替换障碍物的标识.</param>
    /// <param name="obstacle">新的障碍物环.</param>
    /// <returns>找到并替换障碍物时为 <see langword="true"/>.</returns>
    public bool ReplaceObstacle(int obstacleId, NavMeshPolygon obstacle)
    {
        ArgumentNullException.ThrowIfNull(obstacle);
        if (!m_Obstacles.TryGetValue(obstacleId, out NavMeshPolygon? original)) return false;
        m_Obstacles[obstacleId] = obstacle;
        try
        {
            PublishRebuiltSnapshot();
            return true;
        }
        catch
        {
            m_Obstacles[obstacleId] = original;
            throw;
        }
    }

    /// <summary>
    /// 移除指定障碍物并发布新快照.
    /// </summary>
    /// <param name="obstacleId">待移除障碍物的标识.</param>
    /// <returns>找到并移除障碍物时为 <see langword="true"/>.</returns>
    public bool RemoveObstacle(int obstacleId)
    {
        if (!m_Obstacles.Remove(obstacleId, out NavMeshPolygon? obstacle)) return false;
        try
        {
            PublishRebuiltSnapshot();
            return true;
        }
        catch
        {
            m_Obstacles.Add(obstacleId, obstacle);
            throw;
        }
    }

    private void PublishRebuiltSnapshot()
    {
        Mesh snapshot = BuildSnapshot();
        Snapshot = snapshot;
    }

    private Mesh BuildSnapshot()
    {
        List<NavMeshPolygon> obstacles = Factory.RentList<NavMeshPolygon>();
        try
        {
            obstacles.AddRange(m_Obstacles.Values);
            return NavMeshBuilder.Build(new NavMeshBuildInput(m_Boundary, obstacles), m_Options);
        }
        finally
        {
            Factory.Release(obstacles);
        }
    }
}
