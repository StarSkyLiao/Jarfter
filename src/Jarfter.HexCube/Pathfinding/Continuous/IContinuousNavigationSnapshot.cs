using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示一次连续路径搜索期间不可变的地图快照.
/// 障碍物均为与坐标轴平行的 <see cref="HexCubeArea2D"/>; 实现应按请求中的单位半径和安全距离扩大障碍物后判断可通行性.
/// </summary>
public interface IContinuousNavigationSnapshot
{
    /// <summary>
    /// 获取该快照对应的地图版本.
    /// </summary>
    long Revision { get; }

    /// <summary>
    /// 获取快照中的基础障碍物区域.
    /// 路径搜索器仅可读取该集合, 不得修改集合内容.
    /// </summary>
    IReadOnlyList<HexCubeArea2D> Obstacles { get; }

    /// <summary>
    /// 获取快照中的高代价区域.
    /// 路径搜索器仅可读取该集合, 不得修改集合内容.
    /// </summary>
    IReadOnlyList<ContinuousTraversalArea> TraversalAreas { get; }

    /// <summary>
    /// 获取一个值, 指示地图中是否不存在高代价区域.
    /// 返回 true 时, <see cref="GetLineCost"/> 必须返回 <see cref="HexCubeLine2D.Length"/>.
    /// </summary>
    bool UsesUniformTraversalCost { get; }

    /// <summary>
    /// 判断指定位置能否容纳具有给定半径的移动单位.
    /// 位置位于扩大后障碍物的边界上时也视为不可通行.
    /// </summary>
    /// <param name="position">待判断的单位中心位置.</param>
    /// <param name="agentRadius">移动单位半径.</param>
    /// <param name="clearance">额外安全距离.</param>
    /// <returns>位置可通行时返回 true, 否则返回 false.</returns>
    bool IsPositionNavigable(HexCubePoint position, double agentRadius, double clearance);

    /// <summary>
    /// 判断具有给定半径的移动单位能否沿指定线段直接移动.
    /// 线段仅接触扩大后障碍物的单个顶点时允许通行, 以支持以障碍物顶点作为绕行折点.
    /// </summary>
    /// <param name="line">待判断的单位中心移动线段.</param>
    /// <param name="agentRadius">移动单位半径.</param>
    /// <param name="clearance">额外安全距离.</param>
    /// <returns>线段可直接通行时返回 true, 否则返回 false.</returns>
    bool HasLineOfSight(HexCubeLine2D line, double agentRadius, double clearance);

    /// <summary>
    /// 获取沿指定可通行线段移动的总代价.
    /// 当前规则为线段长度加上每个高代价区域内经过长度乘以对应的额外倍率; 重叠区域的额外倍率会叠加.
    /// </summary>
    /// <param name="line">待计算代价的可通行移动线段.</param>
    /// <returns>有限的正移动代价.</returns>
    double GetLineCost(HexCubeLine2D line);
}
