using Jarfter.NavMesh.Geometry;
using Jarfter.NavMesh.Query;
using Mesh = Jarfter.NavMesh.Topology.NavMesh;

namespace Jarfter.NavMesh.Crowd;

/// <summary>
/// 基于导航网格路径的轻量二维 Crowd 调度器.
/// 负责 agent 的目标请求和沿路径推进, 当前版本不提供 agent 间局部避障.
/// 修改和 Update 操作应由单一线程串行调用.
/// </summary>
public sealed class NavMeshCrowd
{
    private readonly Dictionary<int, Agent> m_Agents = new Dictionary<int, Agent>();
    private Mesh m_NavMesh;
    private int m_NextAgentId;

    /// <summary>
    /// 使用不可变导航网格创建 Crowd 调度器.
    /// </summary>
    /// <param name="navMesh">用于寻路和移动的导航网格.</param>
    public NavMeshCrowd(Mesh navMesh)
    {
        ArgumentNullException.ThrowIfNull(navMesh);
        m_NavMesh = navMesh;
    }

    /// <summary>
    /// 获取当前已注册 agent 数量.
    /// </summary>
    public int AgentCount => m_Agents.Count;

    /// <summary>
    /// 添加 agent, 并将起点投影到最近可行走位置.
    /// </summary>
    /// <param name="position">agent 的期望起点.</param>
    /// <param name="maxSpeed">agent 的最大移动速度, 必须为有限非负 double.</param>
    /// <returns>可用于后续操作 agent 的标识.</returns>
    public int AddAgent(NavMeshPoint position, double maxSpeed)
    {
        if (!double.IsFinite(maxSpeed) || maxSpeed < 0) throw new ArgumentOutOfRangeException(nameof(maxSpeed));
        if (m_NextAgentId == int.MaxValue) throw new InvalidOperationException("Crowd agent 标识已耗尽.");
        if (!m_NavMesh.TryFindNearestPoint(position, out _, out NavMeshPoint projected))
            throw new InvalidOperationException("导航网格中没有可供 agent 使用的三角形.");
        int agentId = m_NextAgentId++;
        m_Agents.Add(agentId, new Agent(projected, maxSpeed));
        return agentId;
    }

    /// <summary>
    /// 移除指定 agent.
    /// </summary>
    /// <param name="agentId">待移除 agent 的标识.</param>
    /// <returns>找到并移除了 agent 时为 <see langword="true"/>.</returns>
    public bool RemoveAgent(int agentId)
    {
        return m_Agents.Remove(agentId);
    }

    /// <summary>
    /// 获取指定 agent 的当前状态.
    /// </summary>
    /// <param name="agentId">agent 标识.</param>
    /// <param name="state">成功时的 agent 状态.</param>
    /// <returns>找到 agent 时为 <see langword="true"/>.</returns>
    public bool TryGetAgentState(int agentId, out NavMeshCrowdAgentState state)
    {
        if (!m_Agents.TryGetValue(agentId, out Agent? agent))
        {
            state = default;
            return false;
        }

        state = new NavMeshCrowdAgentState(agentId, agent.Position, agent.Target, agent.Path is not null);
        return true;
    }

    /// <summary>
    /// 请求 agent 向目标点移动.
    /// </summary>
    /// <param name="agentId">agent 标识.</param>
    /// <param name="target">期望移动目标.</param>
    /// <returns>找到 agent 且成功建立路径时为 <see langword="true"/>.</returns>
    public bool RequestMoveTarget(int agentId, NavMeshPoint target)
    {
        return RequestMoveTarget(agentId, target, NavMeshQueryDefaults.Filter);
    }

    /// <summary>
    /// 请求 agent 使用指定过滤器向目标点移动.
    /// </summary>
    /// <param name="agentId">agent 标识.</param>
    /// <param name="target">期望移动目标.</param>
    /// <param name="filter">决定路径三角形是否可通行的过滤器.</param>
    /// <returns>找到 agent 且成功建立路径时为 <see langword="true"/>.</returns>
    public bool RequestMoveTarget(int agentId, NavMeshPoint target, INavMeshQueryFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (!m_Agents.TryGetValue(agentId, out Agent? agent)) return false;
        NavMeshPath? path = m_NavMesh.FindPath(
            agent.Position, target, m_NavMesh.CreateQueryWorkspace(),
            filter, NavMeshQueryDefaults.CostPolicy
        );
        if (path is null) return false;
        agent.Target = target;
        agent.Path = path;
        agent.NextPointIndex = 1;
        return true;
    }

    /// <summary>
    /// 使用给定时间步长推进全部 agent.
    /// </summary>
    /// <param name="deltaTime">本次更新经过的秒数, 必须为有限非负 double.</param>
    public void Update(double deltaTime)
    {
        if (!double.IsFinite(deltaTime) || deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        foreach (Agent agent in m_Agents.Values) AdvanceAgent(agent, deltaTime);
    }

    /// <summary>
    /// 替换 Crowd 使用的导航网格, 并重新投影 agent 位置与目标路径.
    /// </summary>
    /// <param name="navMesh">新的不可变导航网格快照.</param>
    public void UpdateNavMesh(Mesh navMesh)
    {
        ArgumentNullException.ThrowIfNull(navMesh);
        m_NavMesh = navMesh;
        foreach (Agent agent in m_Agents.Values)
        {
            if (!m_NavMesh.TryFindNearestPoint(agent.Position, out _, out NavMeshPoint projected))
                throw new InvalidOperationException("新的导航网格没有可供 agent 使用的三角形.");
            agent.Position = projected;
            if (agent.Target is not { } target || !RequestPath(agent, target, NavMeshQueryDefaults.Filter))
            {
                agent.Path = null;
                agent.Target = null;
            }
        }
    }

    private bool RequestPath(Agent agent, NavMeshPoint target, INavMeshQueryFilter filter)
    {
        NavMeshPath? path = m_NavMesh.FindPath(
            agent.Position, target, m_NavMesh.CreateQueryWorkspace(),
            filter, NavMeshQueryDefaults.CostPolicy
        );
        if (path is null) return false;
        agent.Target = target;
        agent.Path = path;
        agent.NextPointIndex = 1;
        return true;
    }

    private static void AdvanceAgent(Agent agent, double deltaTime)
    {
        if (agent.Path is null) return;
        double remainingDistance = agent.MaxSpeed * deltaTime;
        while (agent.NextPointIndex < agent.Path.Points.Count)
        {
            NavMeshPoint waypoint = agent.Path.Points[agent.NextPointIndex];
            double x = waypoint.X - agent.Position.X;
            double y = waypoint.Y - agent.Position.Y;
            double distance = Math.Sqrt(x * x + y * y);
            if (distance > remainingDistance && distance > 0)
            {
                double factor = remainingDistance / distance;
                agent.Position = new NavMeshPoint(agent.Position.X + x * factor, agent.Position.Y + y * factor);
                return;
            }

            agent.Position = waypoint;
            remainingDistance -= distance;
            agent.NextPointIndex++;
        }

        agent.Path = null;
    }

    private sealed class Agent(NavMeshPoint position, double maxSpeed)
    {
        public NavMeshPoint Position { get; set; } = position;
        public double MaxSpeed { get; } = maxSpeed;
        public NavMeshPoint? Target { get; set; }
        public NavMeshPath? Path { get; set; }
        public int NextPointIndex { get; set; }
    }
}
