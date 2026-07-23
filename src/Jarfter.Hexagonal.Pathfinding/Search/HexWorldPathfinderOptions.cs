namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 定义 <see cref="HexWorldPathfinder"/> 的装配选项.
/// </summary>
public sealed class HexWorldPathfinderOptions
{
    /// <summary>
    /// 初始化 <see cref="HexWorldPathfinderOptions"/> 的新实例.
    /// </summary>
    public HexWorldPathfinderOptions()
    {
    }

    /// <summary>
    /// 获取或初始化从连续端点寻找可见格心锚点时的最大六边形距离.
    /// 值越大越能从局部阻塞中恢复, 但会提高每个端点的固定枚举开销.
    /// </summary>
    public int AnchorSearchRadius { get; init; } = 1;
}
