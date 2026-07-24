using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Grid;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.World;

/// <summary>
/// 为连续世界坐标端点选择可见且可通行的格心锚点.
/// 选择规则由 <see cref="HexWorldPathfinderOptions.AnchorSelection"/> 决定, 并在局部候选范围内执行直视验证.
/// </summary>
internal static class HexWorldPathAnchorSelector
{
    /// <summary>
    /// 尝试为一个连续端点选择接入格心搜索的锚点.
    /// </summary>
    internal static bool TryGetAnchor(
        HexWorldPathfinderOptions options,
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint position,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        HexPathfindingRequestOptions? requestOptions,
        out HexagonalCubePoint anchor,
        out double cost)
    {
        requestOptions?.CancellationToken.ThrowIfCancellationRequested();

        // 零长度查询会验证对象当前位置未与附近膨胀障碍重叠, 且位置位于快照范围内.
        if (!HexLineOfSight.TryGetTraversalCost(
                snapshot,
                layout,
                position,
                position,
                footprint,
                out _,
                clearanceApothemScale,
                options.CostPolicy))
        {
            anchor = default;
            cost = 0;
            return false;
        }

        HexagonalCubePoint nearest = layout.GetNearestPoint(position);
        double bestCost = double.PositiveInfinity;
        HexagonalCubePoint bestAnchor = default;
        cost = 0;

        // 枚举配置范围内的候选格心, 在局部阻塞时为连续端点选择可见锚点.
        foreach (HexagonalCubePoint candidate in nearest.RangeIn(options.AnchorSearchRadius))
        {
            requestOptions?.CancellationToken.ThrowIfCancellationRequested();

            if (!snapshot.TryGetCell(candidate, out HexNavigationCell cell) || cell.HasObstacle)
            {
                continue;
            }

            if (!HexLineOfSight.TryGetTraversalCost(
                    snapshot,
                    layout,
                    position,
                    layout.GetCenter(candidate),
                    footprint,
                    out double candidateCost,
                    clearanceApothemScale,
                    options.CostPolicy))
            {
                continue;
            }

            double candidateScore = options.AnchorSelection switch
            {
                HexWorldPathAnchorSelection.LowestTraversalCost => candidateCost,
                HexWorldPathAnchorSelection.NearestWorldDistance => position.DistanceTo(layout.GetCenter(candidate)),
                _ => throw new InvalidOperationException()
            };

            if (candidateScore >= bestCost)
            {
                continue;
            }

            bestAnchor = candidate;
            bestCost = candidateScore;
            cost = candidateCost;
        }

        if (double.IsPositiveInfinity(bestCost))
        {
            anchor = default;
            cost = 0;
            return false;
        }

        anchor = bestAnchor;
        return true;
    }
}
