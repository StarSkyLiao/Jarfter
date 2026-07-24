using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;
using Jarfter.Hexagonal.Pathfinding.Grid;
using Jarfter.Hexagonal.Pathfinding.Navigation;

namespace Jarfter.Hexagonal.Pathfinding.World;

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
    /// <param name="requestOptions">本次格心搜索的节点、超时、取消与缓存策略; 为 <see langword="null"/> 时使用默认策略.</param>
    /// <returns>成功时得到连续世界坐标路径; 不可达、超时或超出节点预算时返回 <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="snapshot"/> 或 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当足迹、边距或坐标无效时抛出.</exception>
    /// <exception cref="OperationCanceledException">当 <paramref name="requestOptions"/> 中的取消令牌被取消时抛出.</exception>
    public HexWorldPath? FindPath(
        IHexNavigationSnapshot snapshot,
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint goal,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0,
        HexPathfindingRequestOptions? requestOptions = null)
    {
        return FindPathCore(
            snapshot,
            layout,
            start,
            goal,
            footprint,
            clearanceApothemScale,
            requestOptions);
    }

    /// <inheritdoc />
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
            () => FindPathCore(
                snapshot,
                layout,
                start,
                goal,
                footprint,
                clearanceApothemScale,
                requestOptions),
            requestOptions?.CancellationToken ?? CancellationToken.None));
    }

    private HexWorldPath? FindPathCore(
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

        if (!HexWorldPathAnchorSelector.TryGetAnchor(
                Options,
                snapshot,
                layout,
                start,
                footprint,
                clearanceApothemScale,
                requestOptions,
                out HexagonalCubePoint startAnchor,
                out _)
            || !HexWorldPathAnchorSelector.TryGetAnchor(
                Options,
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

        HexGridPath? gridPath = GridPathfinder.FindPath(
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
            waypoints = HexWorldPathPostProcessor.SmoothWaypoints(
                snapshot,
                layout,
                waypoints,
                footprint,
                clearanceApothemScale,
                Options.CostPolicy);
        }

        if (!HexWorldPathPostProcessor.TryGetPathCost(
                snapshot,
                layout,
                waypoints,
                footprint,
                clearanceApothemScale,
                Options.CostPolicy,
                out double cost))
        {
            return null;
        }

        return new HexWorldPath([.. waypoints], cost, snapshot.Version);
    }

    private static void AddWaypoint(List<HexagonalWorldPoint> waypoints, HexagonalWorldPoint point)
    {
        if (waypoints[^1] != point)
        {
            waypoints.Add(point);
        }
    }
}
