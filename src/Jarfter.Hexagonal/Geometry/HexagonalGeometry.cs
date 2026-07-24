using System.Runtime.CompilerServices;
using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.Geometry;

/// <summary>
/// 提供固定朝向正六边形在连续平面中的通用几何查询.
/// 这些查询不依赖导航地图或寻路算法, 可用于碰撞、可见性和路径后处理等场景.
/// </summary>
public static class HexagonalGeometry
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
    /// 判断线段是否接触或进入指定格心上的正六边形.
    /// 六边形与布局同朝向, 尺寸以布局单位六边形 Apothem 的比例表示.
    /// </summary>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">路径线段的起点.</param>
    /// <param name="end">路径线段的终点.</param>
    /// <param name="hexagonPoint">正六边形所在的格心坐标.</param>
    /// <param name="apothemScale">正六边形相对于单位六边形 Apothem 的非负尺寸比例. 0 表示退化区域.</param>
    /// <returns>当线段接触或进入六边形区域时返回 <see langword="true"/>; 六边形退化时返回 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当尺寸比例或平面坐标无效时抛出.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static bool SegmentIntersectsHexagon(
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalCubePoint hexagonPoint,
        double apothemScale)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinitePoint(start, nameof(start));
        ValidateFinitePoint(end, nameof(end));

        if (!double.IsFinite(apothemScale) || apothemScale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(apothemScale));
        }

        return SegmentIntersectsHexagonUnchecked(
            layout,
            start,
            end,
            hexagonPoint,
            apothemScale);
    }

    /// <summary>
    /// 获取线段依次穿过的主六边形格子集合.
    /// 枚举器不分配托管内存, 每个返回项给出该格子在线段上的参数区间.
    /// </summary>
    /// <param name="layout">定义格心位置、朝向和单位 Apothem 的六边形布局.</param>
    /// <param name="start">线段起点.</param>
    /// <param name="end">线段终点.</param>
    /// <returns>可枚举的线段穿格集合.</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="layout"/> 为 <see langword="null"/> 时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当任一平面坐标包含非有限分量时抛出.</exception>
    public static HexagonalSegmentTraversal TraverseSegment(
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ValidateFinitePoint(start, nameof(start));
        ValidateFinitePoint(end, nameof(end));

        return new HexagonalSegmentTraversal(layout, start, end, layout.Orientation);
    }

    /// <summary>
    /// 获取指定六边形朝向的六条边外法线.
    /// 返回的跨度引用静态数组, 调用方不得将其用于跨线程写入或修改.
    /// </summary>
    /// <param name="orientation">六边形固定朝向.</param>
    /// <returns>按逆时针顺序排列的六条单位边外法线.</returns>
    internal static ReadOnlySpan<HexagonalWorldPoint> GetSideNormals(HexagonalOrientation orientation)
    {
        return orientation == HexagonalOrientation.PointyTop
            ? s_PointyTopSideNormals
            : s_FlatTopSideNormals;
    }

    /// <summary>
    /// 在调用方已验证参数时, 判断线段是否与正六边形相交.
    /// </summary>
    /// <param name="layout">六边形布局.</param>
    /// <param name="start">路径线段起点.</param>
    /// <param name="end">路径线段终点.</param>
    /// <param name="hexagonPoint">正六边形格心坐标.</param>
    /// <param name="apothemScale">正六边形 Apothem 比例.</param>
    /// <returns>当线段接触或进入六边形时返回 <see langword="true"/>.</returns>
    internal static bool SegmentIntersectsHexagonUnchecked(
        HexagonalLayout layout,
        HexagonalWorldPoint start,
        HexagonalWorldPoint end,
        HexagonalCubePoint hexagonPoint,
        double apothemScale)
    {
        if (apothemScale == 0) return false;

        return SegmentIntersectsHexagonUnchecked(
            layout,
            start.X,
            start.Y,
            end.X - start.X,
            end.Y - start.Y,
            GetSideNormals(layout.Orientation),
            hexagonPoint,
            apothemScale);
    }

    /// <summary>
    /// 在调用方已预先计算线段方向和六边形边法线时, 判断线段是否与正六边形相交.
    /// </summary>
    /// <param name="layout">六边形布局.</param>
    /// <param name="startX">线段起点的 X 分量.</param>
    /// <param name="startY">线段起点的 Y 分量.</param>
    /// <param name="deltaX">线段的 X 方向分量.</param>
    /// <param name="deltaY">线段的 Y 方向分量.</param>
    /// <param name="sideNormals">布局朝向对应的六条单位边外法线.</param>
    /// <param name="hexagonPoint">正六边形格心坐标.</param>
    /// <param name="apothemScale">正六边形 Apothem 比例.</param>
    /// <returns>当线段接触或进入六边形时返回 <see langword="true"/>.</returns>
    internal static bool SegmentIntersectsHexagonUnchecked(
        HexagonalLayout layout,
        double startX,
        double startY,
        double deltaX,
        double deltaY,
        ReadOnlySpan<HexagonalWorldPoint> sideNormals,
        HexagonalCubePoint hexagonPoint,
        double apothemScale)
    {
        if (apothemScale == 0) return false;

        HexagonalWorldPoint center = layout.GetCenter(hexagonPoint);
        double apothem = layout.UnitApothem * apothemScale;
        return SegmentIntersectsConvexHexagon(
            startX - center.X,
            startY - center.Y,
            deltaX,
            deltaY,
            apothem,
            sideNormals);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static bool SegmentIntersectsConvexHexagon(
        double startX,
        double startY,
        double deltaX,
        double deltaY,
        double apothem,
        ReadOnlySpan<HexagonalWorldPoint> sideNormals)
    {
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
