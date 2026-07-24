using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Grid.Requests;
using Jarfter.Hexagonal.Pathfinding.Grid.Results;
using Jarfter.Hexagonal.Pathfinding.Grid.Runtime;
using Jarfter.Hexagonal.Pathfinding.Navigation.Central;
using Jarfter.Hexagonal.Pathfinding.Navigation.Model;

namespace Jarfter.Hexagonal.Pathfinding.Grid.Internal;

/// <summary>
/// 负责按局部范围策略执行多个格心搜索阶段.
/// 每个阶段都从空搜索状态开始, 但共享同一个累计运行状态, 因而超时、节点预算、统计和直视缓存均以整个请求为单位计算.
/// </summary>
internal static class HexGridSearchScopeRunner
{
    private const int MaximumAttemptCount = 32;

    /// <summary>
    /// 对常规稀疏搜索后端执行范围扩张.
    /// </summary>
    internal static HexGridPath? FindPath(
        HexGridSearchMode mode,
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy? costPolicy,
        HexPathfindingRequestOptions requestOptions,
        IHexPathSearchScopeStrategy scopeStrategy)
    {
        HexGridSearchRunState runState = new HexGridSearchRunState(mode, requestOptions, usesStatelessLineOfSightCache: true);
        int directDistance = start.DistanceTo(goal);
        int? previousMaximumDistanceSum = null;

        for (int attemptIndex = 0; attemptIndex < MaximumAttemptCount; attemptIndex++)
        {
            int? maximumDistanceSum = scopeStrategy.GetMaximumDistanceSum(directDistance, attemptIndex);
            HexGridSearchRuntime.ValidateSearchScope(maximumDistanceSum, directDistance, previousMaximumDistanceSum, scopeStrategy);

            if (HexGridSearchRuntime.IsTimeoutExpired(requestOptions, runState.StartTimestamp))
            {
                return null;
            }

            HexGridPath? path = HexGridSearch.FindPath(
                mode,
                snapshot,
                layout,
                start,
                goal,
                footprint,
                clearanceApothemScale,
                costPolicy,
                requestOptions,
                maximumDistanceSum,
                runState);
            if (path is not null || maximumDistanceSum is null)
            {
                return path;
            }

            previousMaximumDistanceSum = maximumDistanceSum;
        }

        throw new ArgumentOutOfRangeException(nameof(scopeStrategy), "范围扩张策略必须在 32 个阶段内返回无限制范围.");
    }

    /// <summary>
    /// 对中心稠密工作区搜索后端执行范围扩张.
    /// </summary>
    internal static HexGridPath? FindPath(
        HexGridSearchMode mode,
        HexGridCentralNavigationSnapshot snapshot,
        HexGridPathfindingWorkspace workspace,
        HexagonalLayout layout,
        HexagonalCubePoint start,
        HexagonalCubePoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        IHexTraversalCostPolicy? costPolicy,
        HexPathfindingRequestOptions requestOptions,
        IHexPathSearchScopeStrategy scopeStrategy)
    {
        HexGridSearchRunState runState = new HexGridSearchRunState(mode, requestOptions, usesStatelessLineOfSightCache: false);
        int directDistance = start.DistanceTo(goal);
        int? previousMaximumDistanceSum = null;

        for (int attemptIndex = 0; attemptIndex < MaximumAttemptCount; attemptIndex++)
        {
            int? maximumDistanceSum = scopeStrategy.GetMaximumDistanceSum(directDistance, attemptIndex);
            HexGridSearchRuntime.ValidateSearchScope(maximumDistanceSum, directDistance, previousMaximumDistanceSum, scopeStrategy);

            if (HexGridSearchRuntime.IsTimeoutExpired(requestOptions, runState.StartTimestamp))
            {
                return null;
            }

            HexGridPath? path = HexGridSearch.FindPath(
                mode,
                snapshot,
                workspace,
                layout,
                start,
                goal,
                footprint,
                clearanceApothemScale,
                costPolicy,
                requestOptions,
                maximumDistanceSum,
                runState);
            if (path is not null || maximumDistanceSum is null)
            {
                return path;
            }

            previousMaximumDistanceSum = maximumDistanceSum;
        }

        throw new ArgumentOutOfRangeException(nameof(scopeStrategy), "范围扩张策略必须在 32 个阶段内返回无限制范围.");
    }
}
