using Jarfter.Core.Numerics;

namespace Jarfter.Hexagonal.Coordinates;

/// <summary>
/// 使用二维数组存储的六边形地图.
/// </summary>
/// <param name="radius">该六边形地图的半径.</param>
public class HexagonalMap<TElement>(int radius)
{
    /// <summary>
    /// 该六边形地图的半径, 等于所有点距离中心最远的距离.
    /// 半径为 0 的六边形地图只有一个中心点.
    /// </summary>
    public int Radius { get; } = radius >= 0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));

    /// <summary>
    /// 地图中所有点的总数.
    /// </summary>
    public int Count { get; } = 1 + 3 * radius + 3 * radius * radius;

    /// <summary>
    /// 包含的所有元素.
    /// </summary>
    private TElement[] m_Elements = new TElement[1 + 3 * radius + 3 * radius * radius];




}
