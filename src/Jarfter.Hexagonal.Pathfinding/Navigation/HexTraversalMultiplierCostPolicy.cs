namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 使用 <see cref="HexNavigationCell.TraversalMultiplier"/> 计算移动成本的默认策略.
/// </summary>
public sealed class HexTraversalMultiplierCostPolicy : IHexTraversalCostPolicy
{
    /// <summary>
    /// 获取无状态默认移动成本策略的单例实例.
    /// </summary>
    public static HexTraversalMultiplierCostPolicy Instance { get; } = new HexTraversalMultiplierCostPolicy();

    private HexTraversalMultiplierCostPolicy()
    {
    }

    /// <inheritdoc />
    public double MinimumCostPerUnitLength => 1;

    /// <inheritdoc />
    public double GetTraversalCost(double length, HexNavigationCell cell)
    {
        return length * cell.TraversalMultiplier;
    }
}
