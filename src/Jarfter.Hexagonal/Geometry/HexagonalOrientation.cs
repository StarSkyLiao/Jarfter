namespace Jarfter.Hexagonal.Geometry;

/// <summary>
/// 表示正六边形在二维平面中的固定朝向.
/// 坐标系使用 X 轴向右、Y 轴向上.
/// </summary>
public enum HexagonalOrientation
{
    /// <summary>
    /// 六边形的一个顶点位于 Y 轴正方向.
    /// </summary>
    PointyTop,

    /// <summary>
    /// 六边形的一条边与 Y 轴平行.
    /// </summary>
    FlatTop
}
