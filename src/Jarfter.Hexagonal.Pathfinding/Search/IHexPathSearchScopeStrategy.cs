namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 定义格心搜索在多阶段局部范围内扩张的策略.
/// 返回的距离和上限用于约束候选节点到起点和终点的六边形距离之和.
/// 有限范围必须不小于直接距离, 且相邻有限范围必须严格扩大; 策略必须在 32 个阶段内返回无限制范围.
/// </summary>
public interface IHexPathSearchScopeStrategy
{
    /// <summary>
    /// 获取指定阶段允许的节点距离和上限.
    /// </summary>
    /// <param name="directDistance">起点到终点的直接六边形距离.</param>
    /// <param name="attemptIndex">从 0 开始的搜索阶段索引.</param>
    /// <returns>当前阶段不小于直接距离的距离和上限; 返回 <see langword="null"/> 时表示不限制范围并结束扩张.</returns>
    int? GetMaximumDistanceSum(int directDistance, int attemptIndex);
}
