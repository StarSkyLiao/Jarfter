namespace Jarfter.Hexagonal.Geometry;

/// <summary>
/// 表示六边形布局所在二维平面中的连续坐标.
/// 此类型不隐含格子归属, 可用于移动对象的位置、路径航点和布局原点.
/// </summary>
/// <param name="X">平面坐标的 X 分量.</param>
/// <param name="Y">平面坐标的 Y 分量.</param>
public readonly record struct HexagonalWorldPoint(double X, double Y)
{
    /// <summary>
    /// 获取坐标原点.
    /// </summary>
    public static HexagonalWorldPoint Zero => new HexagonalWorldPoint(0, 0);

    /// <summary>
    /// 获取当前坐标到另一坐标的欧氏距离.
    /// </summary>
    /// <param name="other">另一个平面坐标.</param>
    /// <returns>两个坐标之间的欧氏距离.</returns>
    public double DistanceTo(HexagonalWorldPoint other)
    {
        double x = X - other.X;
        double y = Y - other.Y;
        return Math.Sqrt(x * x + y * y);
    }
}
