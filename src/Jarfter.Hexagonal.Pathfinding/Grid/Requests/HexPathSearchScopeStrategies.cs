namespace Jarfter.Hexagonal.Pathfinding.Grid.Requests;

/// <summary>
/// 提供内置的格心搜索范围扩张策略.
/// </summary>
public static class HexPathSearchScopeStrategies
{
    /// <summary>
    /// 获取先后使用 2D、4D、8D 距离和上限并最终回退到无限制搜索的策略.
    /// 每个有限阶段至少保留两个额外绕行步, 以支持极短距离移动绕过局部障碍.
    /// </summary>
    public static IHexPathSearchScopeStrategy ExpandingDetour { get; } = new ExpandingDetourStrategy();

    private sealed class ExpandingDetourStrategy : IHexPathSearchScopeStrategy
    {
        public int? GetMaximumDistanceSum(int directDistance, int attemptIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(directDistance);
            ArgumentOutOfRangeException.ThrowIfNegative(attemptIndex);

            return attemptIndex switch
            {
                0 => Math.Max(checked(directDistance * 2), checked(directDistance + 2)),
                1 => Math.Max(checked(directDistance * 4), checked(directDistance + 2)),
                2 => Math.Max(checked(directDistance * 8), checked(directDistance + 2)),
                _ => null
            };
        }
    }
}
