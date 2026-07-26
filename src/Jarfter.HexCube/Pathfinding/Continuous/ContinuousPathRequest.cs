using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示一次连续路径搜索的输入参数.
/// 移动单位始终为与地图平行的正六边形, 因此只需提供其半径; 半径为 0 时表示点单位.
/// </summary>
/// <param name="Start">移动单位中心的起点坐标.</param>
/// <param name="Goal">移动单位中心的终点坐标.</param>
/// <param name="AgentRadius">移动单位的六边形边长比例. 0 表示点单位.</param>
/// <param name="Clearance">移动单位与障碍物之间额外保留的边长比例.</param>
/// <param name="HeuristicWeight">启发函数权重. 必须为有限正数.</param>
public readonly record struct ContinuousPathRequest(
    HexCubePoint Start,
    HexCubePoint Goal,
    double AgentRadius = 0,
    double Clearance = 1e-9,
    double HeuristicWeight = 1)
{
    /// <summary>
    /// 根据起始六边形区域创建连续路径搜索请求.
    /// 区域的位置作为起点, 半径作为移动单位半径; 因为所有六边形均保持平行, 寻路过程无需保留额外的朝向信息.
    /// </summary>
    /// <param name="agent">位于起点的移动单位区域.</param>
    /// <param name="goal">移动单位中心的终点坐标.</param>
    /// <param name="clearance">移动单位与障碍物之间额外保留的边长比例.</param>
    /// <param name="heuristicWeight">启发函数权重. 必须为有限正数.</param>
    public ContinuousPathRequest(HexCubeArea2D agent, HexCubePoint goal, double clearance = 1e-9, double heuristicWeight = 1)
        : this(agent.Position, goal, agent.RadiusScale, clearance, heuristicWeight)
    {
    }
}
