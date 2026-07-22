namespace Jarfter.Hexagonal.Direction;

/// <summary>
/// 表示六边形网格中与显示朝向无关的六个拓扑方向.
/// </summary>
public enum HexagonalDirection
{
    /// <summary>
    /// q 分量增加, s 分量减少的方向.
    /// </summary>
    PositiveQ = 0,

    /// <summary>
    /// r 分量减少, q 分量增加的方向.
    /// </summary>
    NegativeR = 1,

    /// <summary>
    /// s 分量增加, r 分量减少的方向.
    /// </summary>
    PositiveS = 2,

    /// <summary>
    /// q 分量减少, s 分量增加的方向.
    /// </summary>
    NegativeQ = 3,

    /// <summary>
    /// r 分量增加, q 分量减少的方向.
    /// </summary>
    PositiveR = 4,

    /// <summary>
    /// s 分量减少, r 分量增加的方向.
    /// </summary>
    NegativeS = 5
}
