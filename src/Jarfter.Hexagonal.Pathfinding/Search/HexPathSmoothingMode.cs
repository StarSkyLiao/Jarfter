namespace Jarfter.Hexagonal.Pathfinding.Search;

/// <summary>
/// 指定连续路径接入格心路径后的航点平滑方式.
/// </summary>
public enum HexPathSmoothingMode
{
    /// <summary>
    /// 保留格心路径查找器返回的全部航点.
    /// </summary>
    None,

    /// <summary>
    /// 贪心移除具有直接可通行视线的中间航点.
    /// </summary>
    LineOfSight
}
