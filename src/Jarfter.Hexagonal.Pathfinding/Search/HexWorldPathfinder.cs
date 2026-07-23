using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 提供以任意连续世界坐标作为起终点的六边形路径查找.
/// 在直接可见时返回单段路径; 否则通过附近可见格心锚点接入已装配的格心路径查找器.
/// </summary>
public sealed class HexWorldPathfinder : IHexWorldPathfinder
{
    /// <summary>
    /// 初始化 <see cref="HexWorldPathfinder"/> 的新实例.
    /// </summary>
    /// <param name="gridPathfinder">负责格心锚点之间搜索的路径查找器.</param>
    /// <param name="options">连续端点接入格心搜索的选项; 为 <see langword="null"/> 时使用默认选项.</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="gridPathfinder"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当锚点搜索半径小于一时抛出.</exception>
    public HexWorldPathfinder(IHexGridPathfinder gridPathfinder, HexWorldPathfinderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(gridPathfinder);

        options ??= new HexWorldPathfinderOptions();

        if (options.AnchorSearchRadius < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.AnchorSearchRadius, "锚点搜索半径必须至少为一.");
        }

        if (!Enum.IsDefined(options.AnchorSelection))
        {
            throw new ArgumentOutOfRangeException(nameof(options));
        }

        if (!Enum.IsDefined(options.PathSmoothingMode))
        {
            throw new ArgumentOutOfRangeException(nameof(options));
        }

        ArgumentNullException.ThrowIfNull(options.CostPolicy);

        if (!double.IsFinite(options.CostPolicy.MinimumCostPerUnitLength) || options.CostPolicy.MinimumCostPerUnitLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options));
        }

        GridPathfinder = gridPathfinder;
        Options = options;
    }

    /// <summary>
    /// 获取负责格心锚点之间搜索的路径查找器.
    /// </summary>
    public IHexGridPathfinder GridPathfinder { get; }

    /// <summary>
    /// 获取此实例使用的连续端点接入选项.
    /// </summary>
    public HexWorldPathfinderOptions Options { get; }

    /// <summary>
    /// 在指定不可变导航快照中尝试查找从连续起点到连续终点的低成本可见路径.
    /// 返回路径的首尾航点分别等于传入的 <paramref name="start"/> 和 <paramref name="goal"/>.
    /// </summary>
    /// <param name="snapshot">要读取的不可变导航地图快照.</param>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">移动对象的连续世界坐标起点.</param>
    /// <param name="goal">移动对象的连续世界坐标终点.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <param name="requestOptions">本次格心搜索的节点、超时与取消限制; 为 <see langword="null"/> 时不限制.</param>
    /// <returns>表示异步搜索操作的值任务. 成功时结果为连续世界坐标路径; 不可达、超时或超出节点预算时结果为 <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="snapshot"/> 或 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当足迹、边距或坐标无效时抛出.</exception>
    /// <exception cref="OperationCanceledException">当 <paramref name="requestOptions"/> 中的取消令牌被取消时抛出.</exception>
    public ValueTask<HexWorldPath?> FindPathAsync(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        HexPathfindingRequestOptions? requestOptions = null)
    {
        return new ValueTask<HexWorldPath?>(Task.Run(
            () => TryFindPathCoreAsync(
                snapshot,
                layout,
                start,
                goal,
                footprint,
                clearanceApothemScale,
                requestOptions),
            requestOptions?.CancellationToken ?? CancellationToken.None));
    }

    private async Task<HexWorldPath?> TryFindPathCoreAsync(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        HexPathfindingRequestOptions? requestOptions = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(layout);
        requestOptions?.Validate();
        requestOptions?.CancellationToken.ThrowIfCancellationRequested();

        if (HexLineOfSight.TryGetTraversalCost(
                snapshot,
                layout,
                start,
                goal,
                footprint,
                out double directCost,
                clearanceApothemScale,
                Options.CostPolicy))
        {
            return new HexWorldPath([start, goal], directCost, snapshot.Version);
        }

        if (!TryGetAnchor(
                snapshot,
                layout,
                start,
                footprint,
                clearanceApothemScale,
                requestOptions,
                out HexagonalCubePoint startAnchor,
                out _)
            || !TryGetAnchor(
                snapshot,
                layout,
                goal,
                footprint,
                clearanceApothemScale,
                requestOptions,
                out HexagonalCubePoint goalAnchor,
                out _))
        {
            return null;
        }

        HexGridPath? gridPath = await GridPathfinder.FindPathAsync(
            snapshot,
            layout,
            startAnchor,
            goalAnchor,
            footprint,
            clearanceApothemScale,
            Options.CostPolicy,
            requestOptions);

        if (gridPath is null)
        {
            return null;
        }

        List<HexagonalWorldPoint> waypoints = [start];

        foreach (HexagonalCubePoint point in gridPath.Points)
        {
            AddWaypoint(waypoints, layout.GetCenter(point));
        }

        AddWaypoint(waypoints, goal);

        if (Options.PathSmoothingMode == HexPathSmoothingMode.LineOfSight)
        {
            waypoints = SmoothWaypoints(snapshot, layout, waypoints, footprint, clearanceApothemScale);
        }

        if (!TryGetPathCost(snapshot, layout, waypoints, footprint, clearanceApothemScale, out double cost))
        {
            return null;
        }

        return new HexWorldPath([.. waypoints], cost, snapshot.Version);
    }

    private bool TryGetAnchor(
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
                Options.CostPolicy))
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
        foreach (HexagonalCubePoint candidate in nearest.RangeIn(Options.AnchorSearchRadius))
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
                    Options.CostPolicy))
            {
                continue;
            }

            double candidateScore = Options.AnchorSelection switch
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

    private List<HexagonalWorldPoint> SmoothWaypoints(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        List<HexagonalWorldPoint> waypoints,
        HexagonalFootprint footprint,
        double clearanceApothemScale)
    {
        List<HexagonalWorldPoint> smoothedWaypoints = [waypoints[0]];
        int currentIndex = 0;

        while (currentIndex < waypoints.Count - 1)
        {
            int nextIndex = waypoints.Count - 1;

            while (nextIndex > currentIndex + 1
                && !HexLineOfSight.HasLineOfSight(
                    snapshot,
                    layout,
                    waypoints[currentIndex],
                    waypoints[nextIndex],
                    footprint,
                    clearanceApothemScale,
                    Options.CostPolicy))
            {
                nextIndex--;
            }

            smoothedWaypoints.Add(waypoints[nextIndex]);
            currentIndex = nextIndex;
        }

        return smoothedWaypoints;
    }

    private bool TryGetPathCost(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        List<HexagonalWorldPoint> waypoints,
        HexagonalFootprint footprint,
        double clearanceApothemScale,
        out double cost)
    {
        double totalCost = 0;

        for (int index = 1; index < waypoints.Count; index++)
        {
            if (!HexLineOfSight.TryGetTraversalCost(
                    snapshot,
                    layout,
                    waypoints[index - 1],
                    waypoints[index],
                    footprint,
                    out double segmentCost,
                    clearanceApothemScale,
                    Options.CostPolicy))
            {
                cost = 0;
                return false;
            }

            totalCost += segmentCost;
        }

        cost = totalCost;
        return true;
    }

    private static void AddWaypoint(List<HexagonalWorldPoint> waypoints, HexagonalWorldPoint point)
    {
        if (waypoints[^1] != point)
        {
            waypoints.Add(point);
        }
    }
}
