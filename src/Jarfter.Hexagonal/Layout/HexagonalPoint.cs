namespace Jarfter.Hexagonal.Layout;

/// <summary>
/// 表示六边形布局中的二维点.
/// </summary>
/// <param name="X">水平坐标.</param>
/// <param name="Y">垂直坐标.</param>
public readonly record struct HexagonalPoint(double X, double Y)
{
    /// <summary>
    /// 获取原点.
    /// </summary>
    public static HexagonalPoint Zero => new HexagonalPoint(0, 0);

    /// <summary>
    /// 将两个点按分量相加.
    /// </summary>
    /// <param name="left">左侧点.</param>
    /// <param name="right">右侧点.</param>
    /// <returns>按分量相加后的点.</returns>
    public static HexagonalPoint operator +(HexagonalPoint left, HexagonalPoint right)
    {
        return new HexagonalPoint(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// 将两个点按分量相减.
    /// </summary>
    /// <param name="left">左侧点.</param>
    /// <param name="right">右侧点.</param>
    /// <returns>按分量相减后的点.</returns>
    public static HexagonalPoint operator -(HexagonalPoint left, HexagonalPoint right)
    {
        return new HexagonalPoint(left.X - right.X, left.Y - right.Y);
    }
}
