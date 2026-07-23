using System.Diagnostics;
using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Geometry;

/// <summary>
/// 定义轴向六边形坐标与二维连续平面坐标之间的布局关系.
/// 单位六边形使用 Apothem 作为尺寸基准, 所有格子和足迹均共享同一固定朝向.
/// </summary>
public sealed class HexagonalLayout
{
    private static readonly double s_Sqrt3 = Math.Sqrt(3);

    /// <summary>
    /// 使用指定朝向、单位 Apothem 和原点初始化六边形布局.
    /// </summary>
    /// <param name="orientation">单位六边形的固定朝向.</param>
    /// <param name="unitApothem">单位六边形的 Apothem, 必须为有限正数.</param>
    /// <param name="origin">轴向原点在二维平面中的格心坐标.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="orientation"/> 或 <paramref name="unitApothem"/> 无效时抛出.</exception>
    public HexagonalLayout(HexagonalOrientation orientation, double unitApothem, HexagonalWorldPoint origin = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)orientation, (int)HexagonalOrientation.FlatTop);
        ArgumentOutOfRangeException.ThrowIfNegative((int)orientation);

        if (!double.IsFinite(unitApothem) || unitApothem <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitApothem));
        }

        if (!double.IsFinite(origin.X) || !double.IsFinite(origin.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(origin));
        }

        Orientation = orientation;
        UnitApothem = unitApothem;
        Origin = origin;
    }

    /// <summary>
    /// 获取单位六边形的固定朝向.
    /// </summary>
    public HexagonalOrientation Orientation { get; }

    /// <summary>
    /// 获取单位六边形的 Apothem.
    /// </summary>
    public double UnitApothem { get; }

    /// <summary>
    /// 获取轴向原点在二维平面中的格心坐标.
    /// </summary>
    public HexagonalWorldPoint Origin { get; }

    /// <summary>
    /// 获取指定轴向坐标对应的格心位置.
    /// </summary>
    /// <param name="point">轴向坐标.</param>
    /// <returns>指定轴向坐标的格心位置.</returns>
    public HexagonalWorldPoint GetCenter(HexagonalCubePoint point)
    {
        double q = point.Q;
        double r = point.R;

        return Orientation switch
        {
            HexagonalOrientation.PointyTop => new HexagonalWorldPoint(
                Origin.X + UnitApothem * (2 * q + r),
                Origin.Y + s_Sqrt3 * UnitApothem * r
            ),
            HexagonalOrientation.FlatTop => new HexagonalWorldPoint(
                Origin.X + s_Sqrt3 * UnitApothem * q,
                Origin.Y + UnitApothem * (q + 2 * r)
            ),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// 获取最接近指定平面坐标的轴向格子.
    /// 位于多个格子等距边界上的坐标会按标准立方坐标舍入规则选择其中一个格子.
    /// </summary>
    /// <param name="point">要转换的连续平面坐标.</param>
    /// <returns>最接近指定平面坐标的轴向坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="point"/> 包含非有限分量时抛出.</exception>
    public HexagonalCubePoint GetNearestPoint(HexagonalWorldPoint point)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(point));
        }

        double x = point.X - Origin.X;
        double y = point.Y - Origin.Y;
        double q;
        double r;

        switch (Orientation)
        {
            case HexagonalOrientation.PointyTop:
                r = y / (s_Sqrt3 * UnitApothem);
                q = (x / UnitApothem - r) / 2;
                break;
            case HexagonalOrientation.FlatTop:
                q = x / (s_Sqrt3 * UnitApothem);
                r = (y / UnitApothem - q) / 2;
                break;
            default:
                throw new UnreachableException();
        }

        return RoundCube(q, r);
    }

    /// <summary>
    /// 获取指定格子上、指定足迹尺寸的一个顶点位置.
    /// 顶点按逆时针顺序编号, 编号 0 位于 X 轴正方向最接近的顶点.
    /// </summary>
    /// <param name="point">轴向坐标.</param>
    /// <param name="footprint">与布局朝向一致的六边形足迹.</param>
    /// <param name="vertexIndex">顶点索引, 范围为 [0, 5].</param>
    /// <returns>指定顶点的平面坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="vertexIndex"/> 不是合法顶点索引时抛出.</exception>
    public HexagonalWorldPoint GetVertex(HexagonalCubePoint point, HexagonalFootprint footprint, int vertexIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(vertexIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertexIndex, 5);

        HexagonalWorldPoint center = GetCenter(point);
        double circumradius = 2 * UnitApothem * footprint.ApothemScale / s_Sqrt3;
        double angle = (Orientation == HexagonalOrientation.PointyTop ? 30 : 0) + 60 * vertexIndex;
        double radians = Math.PI * angle / 180;

        return new HexagonalWorldPoint(
            center.X + circumradius * Math.Cos(radians),
            center.Y + circumradius * Math.Sin(radians)
        );
    }

    /// <summary>
    /// 获取指定足迹在当前布局中的实际 Apothem.
    /// </summary>
    /// <param name="footprint">要计算的六边形足迹.</param>
    /// <returns>足迹在当前布局中的实际 Apothem.</returns>
    public double GetApothem(HexagonalFootprint footprint) => UnitApothem * footprint.ApothemScale;

    private static HexagonalCubePoint RoundCube(double q, double r)
    {
        double s = -q - r;
        int roundedQ = checked((int)Math.Round(q));
        int roundedR = checked((int)Math.Round(r));
        int roundedS = checked((int)Math.Round(s));
        double qDifference = Math.Abs(roundedQ - q);
        double rDifference = Math.Abs(roundedR - r);
        double sDifference = Math.Abs(roundedS - s);

        // 只修正舍入误差最大的分量, 以保持 q + r + s = 0.
        if (qDifference > rDifference && qDifference > sDifference)
        {
            roundedQ = -roundedR - roundedS;
        }
        else if (rDifference > sDifference)
        {
            roundedR = -roundedQ - roundedS;
        }

        return new HexagonalCubePoint(roundedQ, roundedR);
    }
}
