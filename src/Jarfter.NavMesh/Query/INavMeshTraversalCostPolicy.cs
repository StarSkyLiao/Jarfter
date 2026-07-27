namespace Jarfter.NavMesh.Query;

/// <summary>
/// 为路径查询提供区域相关的非负移动倍率.
/// MinimumMultiplier 必须是不超过任何实际返回倍率的全局下界.
/// </summary>
public interface INavMeshTraversalCostPolicy
{
    /// <summary>
    /// 获取所有可通行区域的最小移动倍率.
    /// </summary>
    double MinimumMultiplier { get; }

    /// <summary>
    /// 获取从一个区域进入另一个区域时的移动倍率.
    /// </summary>
    /// <param name="fromAreaId">起始区域标识.</param>
    /// <param name="toAreaId">目标区域标识.</param>
    /// <returns>不小于 MinimumMultiplier 的有限正数倍率.</returns>
    double GetMultiplier(int fromAreaId, int toAreaId);
}
