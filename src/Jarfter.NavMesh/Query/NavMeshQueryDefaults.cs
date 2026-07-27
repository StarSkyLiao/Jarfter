namespace Jarfter.NavMesh.Query;

/// <summary>
/// 提供不改变默认寻路行为的查询过滤器和移动代价策略.
/// </summary>
public static class NavMeshQueryDefaults
{
    /// <summary>
    /// 获取允许全部 flags 和区域通行的默认过滤器.
    /// </summary>
    public static INavMeshQueryFilter Filter { get; } = new NavMeshQueryFilter();

    /// <summary>
    /// 获取所有区域倍率均为 1 的默认移动代价策略.
    /// </summary>
    public static INavMeshTraversalCostPolicy CostPolicy { get; } = new UnitCostPolicy();

    private sealed class UnitCostPolicy : INavMeshTraversalCostPolicy
    {
        public double MinimumMultiplier => 1;
        public double GetMultiplier(int fromAreaId, int toAreaId) => 1;
    }
}
