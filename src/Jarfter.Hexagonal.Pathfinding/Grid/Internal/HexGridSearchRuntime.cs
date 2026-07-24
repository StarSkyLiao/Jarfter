using System.Diagnostics;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Grid.Requests;
using Jarfter.Hexagonal.Pathfinding.Grid.Results;
using Jarfter.Hexagonal.Pathfinding.Navigation.Model;

namespace Jarfter.Hexagonal.Pathfinding.Grid.Internal;

/// <summary>
/// 提供格心搜索循环共用的运行限制、范围判定、启发式和成本策略校验.
/// </summary>
internal static class HexGridSearchRuntime
{
    /// <summary>
    /// 尝试为一个将要展开的节点消耗预算并更新统计.
    /// </summary>
    internal static bool TryConsumeExpansionBudget(
        HexPathfindingRequestOptions? requestOptions,
        HexGridSearchRunState? runState,
        ref int expandedNodeCount,
        HexPathfindingStatisticsCollector? statisticsCollector)
    {
        if (runState is not null)
        {
            return runState.TryConsumeExpansionBudget(requestOptions!.MaximumExpandedNodeCount);
        }

        if (requestOptions is { MaximumExpandedNodeCount: > 0 } && expandedNodeCount >= requestOptions.MaximumExpandedNodeCount)
        {
            return false;
        }

        expandedNodeCount++;
        statisticsCollector?.AddExpandedNode();
        return true;
    }

    /// <summary>
    /// 判断候选格心是否位于当前范围策略允许的距离和内.
    /// </summary>
    internal static bool IsWithinSearchScope(
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalCubePoint point,
        int? maximumDistanceSum)
    {
        return maximumDistanceSum is null
            || start.DistanceTo(point) + point.DistanceTo(goal) <= maximumDistanceSum;
    }

    /// <summary>
    /// 验证有限范围相对于直接距离和前一阶段范围严格扩张.
    /// </summary>
    internal static void ValidateSearchScope(
        int? maximumDistanceSum,
        int directDistance,
        int? previousMaximumDistanceSum,
        IHexPathSearchScopeStrategy scopeStrategy)
    {
        if (maximumDistanceSum is null)
        {
            return;
        }

        if (maximumDistanceSum < directDistance
            || previousMaximumDistanceSum is int previous && maximumDistanceSum <= previous)
        {
            throw new ArgumentOutOfRangeException(nameof(scopeStrategy));
        }
    }

    /// <summary>
    /// 获取格心到终点的可采纳直线启发式成本.
    /// </summary>
    internal static double GetHeuristicCost(
        HexagonalWorldPoint point,
        HexagonalWorldPoint goal,
        double minimumCostPerUnitLength)
    {
        return point.DistanceTo(goal) * minimumCostPerUnitLength;
    }

    /// <summary>
    /// 验证最低单位长度成本可作为非负启发式下界.
    /// </summary>
    internal static void ValidateCostPolicy(IHexTraversalCostPolicy costPolicy, string parameterName)
    {
        if (!double.IsFinite(costPolicy.MinimumCostPerUnitLength) || costPolicy.MinimumCostPerUnitLength < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    /// <summary>
    /// 判断本次搜索是否已超过调用方给定的时限.
    /// </summary>
    internal static bool IsTimeoutExpired(HexPathfindingRequestOptions? requestOptions, long startTimestamp)
    {
        return requestOptions is not null
            && requestOptions.Timeout != Timeout.InfiniteTimeSpan
            && Stopwatch.GetElapsedTime(startTimestamp) >= requestOptions.Timeout;
    }

    /// <summary>
    /// 根据搜索模式和请求选项确定是否启用单次搜索的直视缓存.
    /// </summary>
    internal static bool ShouldEnableLineOfSightCache(
        HexGridSearchMode mode,
        HexPathfindingRequestOptions? requestOptions)
    {
        return requestOptions?.LineOfSightCacheMode switch
        {
            HexLineOfSightCacheMode.Enabled => true,
            HexLineOfSightCacheMode.Disabled => false,
            _ => mode == HexGridSearchMode.ThetaStar
        };
    }
}
