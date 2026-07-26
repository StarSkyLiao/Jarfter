using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 为 Theta* 寻路提供网格移动代价和线段可通行性.
/// 实现应使用与地图一致的几何规则判断视线, 例如将障碍物表示为 <see cref="HexCubeArea2D"/>, 并调用其与 <see cref="HexCubeLine2D"/> 的相交判断.
/// 单次 <see cref="IPathfinder.FindPath"/> 调用期间, 所有查询结果必须来自同一份不可变的地图快照.
/// </summary>
public interface IThetaStarNavigationProvider : IMoveCostProvider
{
    /// <summary>
    /// 获取一个值, 指示所有可通行区域是否使用相同的移动代价.
    /// 返回 true 时, <see cref="IMoveCostProvider.GetMoveCost"/> 必须始终返回 1, 且 <see cref="GetLineCost"/> 必须返回 <see cref="HexCubeLine2D.Length"/>.
    /// 此约束使 Theta* 在父节点直达可见时跳过普通相邻边的比较.
    /// </summary>
    bool UsesUniformTraversalCost => false;

    /// <summary>
    /// 判断指定线段是否可以直接通行.
    /// 判断必须包含起点之外的全部经过区域和终点区域.
    /// </summary>
    /// <param name="line">待判断的移动线段.</param>
    /// <returns>当线段未与不可通行区域相交时返回 true, 否则返回 false.</returns>
    bool HasLineOfSight(HexCubeLine2D line);

    /// <summary>
    /// 判断指定线段是否可通行, 并在可通行时获取其总代价.
    /// 默认实现依次调用 <see cref="HasLineOfSight"/> 和 <see cref="GetLineCost"/>, 以保持已有实现的行为不变.
    /// </summary>
    /// <param name="line">待判断和计算代价的移动线段.</param>
    /// <param name="cost">线段可通行时的总代价.</param>
    /// <returns>线段可通行时返回 true, 否则返回 false.</returns>
    bool TryGetLineCost(HexCubeLine2D line, out double cost)
    {
        if (!HasLineOfSight(line))
        {
            cost = 0;
            return false;
        }

        cost = GetLineCost(line);
        return true;
    }

    /// <summary>
    /// 获取沿指定可通行线段移动的总代价.
    /// 该方法只会在 <see cref="HasLineOfSight"/> 返回 true 后调用.
    /// </summary>
    /// <param name="line">待计算代价的移动线段.</param>
    /// <returns>有限的非负移动代价.</returns>
    double GetLineCost(HexCubeLine2D line);
}
