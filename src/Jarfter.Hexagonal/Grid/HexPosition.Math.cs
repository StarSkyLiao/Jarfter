using System.Runtime.CompilerServices;
using Jarfter.Core.Numerics;

namespace Jarfter.Hexagonal.Grid;

public partial record struct HexagonalGrid<T>
{
    /// <summary>
    /// 获取当前坐标到另一个坐标的六边形距离.
    /// </summary>
    /// <param name="other">另一个六边形坐标.</param>
    /// <returns>两个坐标之间的最短步数.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public T DistanceTo(HexagonalGrid<T> other)
    {
        T q = Q - other.Q;
        T r = R - other.R;
        return (q.Abs() + r.Abs() + (q + r).Abs()) / (T.One + T.One);
    }

    /// <summary>
    /// 将当前坐标绕原点逆时针旋转 60 度.
    /// </summary>
    /// <returns>旋转后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public HexagonalGrid<T> RotateLeft()
    {
        return new HexagonalGrid<T>(Q + R, -Q);
    }

    /// <summary>
    /// 将当前坐标绕原点顺时针旋转 60 度.
    /// </summary>
    /// <returns>旋转后的坐标.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public HexagonalGrid<T> RotateRight()
    {
        return new HexagonalGrid<T>(-R, Q + R);
    }
}
