using System.Runtime.CompilerServices;
using Jarfter.Hexagonal.Coordinates;
using Jarfter.Hexagonal.Geometry;

namespace Jarfter.Hexagonal.Pathfinding.Geometry;

/// <summary>
/// 提供六边形导航所需的连续平面几何查询.
/// 所有障碍与移动对象均假定为和布局同朝向的正六边形.
/// </summary>
public static class HexNavigationGeometry
{
    private static readonly double s_Sqrt3 = Math.Sqrt(3);
    private static readonly HexagonalWorldPoint[] s_PointyTopSideNormals =
    [
        new HexagonalWorldPoint(1, 0),
        new HexagonalWorldPoint(0.5, s_Sqrt3 / 2),
        new HexagonalWorldPoint(-0.5, s_Sqrt3 / 2),
        new HexagonalWorldPoint(-1, 0),
        new HexagonalWorldPoint(-0.5, -s_Sqrt3 / 2),
        new HexagonalWorldPoint(0.5, -s_Sqrt3 / 2)
    ];
    private static readonly HexagonalWorldPoint[] s_FlatTopSideNormals =
    [
        new HexagonalWorldPoint(s_Sqrt3 / 2, 0.5),
        new HexagonalWorldPoint(0, 1),
        new HexagonalWorldPoint(-s_Sqrt3 / 2, 0.5),
        new HexagonalWorldPoint(-s_Sqrt3 / 2, -0.5),
        new HexagonalWorldPoint(0, -1),
        new HexagonalWorldPoint(s_Sqrt3 / 2, -0.5)
    ];

    /// <summary>
    /// 判断路径线段是否接触或进入指定格心障碍按单位足迹和安全边距膨胀后的区域.
    /// 障碍尺寸、单位足迹和安全边距均以布局单位六边形 Apothem 的比例表示.
    /// </summary>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">路径线段的起点.</param>
    /// <param name="end">路径线段的终点.</param>
    /// <param name="obstaclePoint">障碍所在的格心坐标.</param>
    /// <param name="obstacleApothemScale">障碍相对于单位六边形 Apothem 的尺寸比例. 0 表示不存在障碍.</param>
    /// <param name="footprint">移动对象的固定朝向六边形足迹.</param>
    /// <param name="clearanceApothemScale">额外安全边距相对于单位 Apothem 的非负比例.</param>
    /// <returns>当线段接触或进入膨胀后障碍区域时返回 <see langword="true"/>; 不存在障碍时返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当尺寸比例或平面坐标无效时抛出.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool SegmentIntersectsInflatedHexagon(
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalCubePoint obstaclePoint,
        double obstacleApothemScale,
        HexagonalFootprint footprint,
        double clearanceApothemScale = 0)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinitePoint(start, nameof(start));
        ValidateFinitePoint(end, nameof(end));

        if (!double.IsFinite(obstacleApothemScale) || obstacleApothemScale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(obstacleApothemScale));
        }

        if (!double.IsFinite(clearanceApothemScale) || clearanceApothemScale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clearanceApothemScale));
        }

        if (obstacleApothemScale == 0) return false;

        HexagonalWorldPoint center = layout.GetCenter(obstaclePoint);
        double apothem = layout.UnitApothem * (
            obstacleApothemScale + footprint.ApothemScale + clearanceApothemScale);
        ReadOnlySpan<HexagonalWorldPoint> sideNormals = layout.Orientation == HexagonalOrientation.PointyTop
            ? s_PointyTopSideNormals
            : s_FlatTopSideNormals;

        return SegmentIntersectsConvexHexagon(start, end, center, apothem, sideNormals);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool SegmentIntersectsConvexHexagon(
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalWorldPoint center,
        double apothem,
        ReadOnlySpan<HexagonalWorldPoint> sideNormals)
    {
        double startX = start.X - center.X;
        double startY = start.Y - center.Y;
        double deltaX = end.X - start.X;
        double deltaY = end.Y - start.Y;
        double minimumT = 0;
        double maximumT = 1;

        // 对六个凸多边形半平面裁剪线段. 包含边界可避免角色从零宽缝隙穿过.
        foreach (HexagonalWorldPoint normal in sideNormals)
        {
            double startProjection = startX * normal.X + startY * normal.Y;
            double deltaProjection = deltaX * normal.X + deltaY * normal.Y;
            double boundaryDifference = apothem - startProjection;

            if (deltaProjection > 0)
            {
                maximumT = Math.Min(maximumT, boundaryDifference / deltaProjection);
            }
            else if (deltaProjection < 0)
            {
                minimumT = Math.Max(minimumT, boundaryDifference / deltaProjection);
            }
            else if (boundaryDifference < 0)
            {
                return false;
            }

            if (minimumT > maximumT) return false;
        }

        return true;
    }

    private static void ValidateFinitePoint(HexagonalWorldPoint point, string parameterName)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
