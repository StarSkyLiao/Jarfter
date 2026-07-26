namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 表示地图变化后剩余连续路径的有效性校验结果.
/// 调用方应仅在结果为 <see cref="Invalid"/> 或 <see cref="Unknown"/> 时决定是否重新规划.
/// </summary>
public enum ContinuousPathValidationResult
{
    /// <summary>
    /// 自路径规划后发生的地图变化未阻断待移动的剩余路径.
    /// </summary>
    Valid,

    /// <summary>
    /// 当前地图中的障碍物已阻断待移动的剩余路径.
    /// </summary>
    Invalid,

    /// <summary>
    /// 无法获取完整的变更历史或路径参数不适用, 应由调用方保守地重新规划.
    /// </summary>
    Unknown
}
