namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 定义线段穿过导航格子时的移动成本计算策略.
/// 实现提供的最小单位长度成本必须不大于任意可通行格子的实际单位长度成本, 以保证 A* 与 Theta* 的启发函数可采纳.
/// </summary>
public interface IHexTraversalCostPolicy
{
    /// <summary>
    /// 获取可通行路径每单位世界长度的保守最小成本.
    /// </summary>
    double MinimumCostPerUnitLength { get; }

    /// <summary>
    /// 计算指定长度的线段穿过一个导航格子时的移动成本.
    /// </summary>
    /// <param name="length">线段位于该格子主穿越区间内的非负世界长度.</param>
    /// <param name="cell">线段主穿越的导航格子数据.</param>
    /// <returns>该线段区间的非负有限移动成本.</returns>
    double GetTraversalCost(double length, HexNavigationCell cell);
}
