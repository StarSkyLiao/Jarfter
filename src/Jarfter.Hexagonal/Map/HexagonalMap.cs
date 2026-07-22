using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Map;

/// <summary>
/// 提供六边形地图的创建入口.
/// </summary>
public static class HexagonalMap
{
    /// <summary>
    /// 创建空的稀疏六边形地图.
    /// </summary>
    /// <typeparam name="T">地图单元存储的值类型.</typeparam>
    /// <returns>空的稀疏六边形地图.</returns>
    public static SparseHexagonalMap<T> Sparse<T>()
    {
        return new SparseHexagonalMap<T>();
    }

    /// <summary>
    /// 创建具有指定初始容量的稀疏六边形地图.
    /// </summary>
    /// <typeparam name="T">地图单元存储的值类型.</typeparam>
    /// <param name="capacity">初始容量.</param>
    /// <returns>具有指定初始容量的稀疏六边形地图.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 capacity 小于 0 时抛出.</exception>
    public static SparseHexagonalMap<T> Sparse<T>(int capacity)
    {
        return new SparseHexagonalMap<T>(capacity);
    }

    /// <summary>
    /// 创建固定轴向矩形范围的稠密六边形地图.
    /// </summary>
    /// <typeparam name="T">地图单元存储的值类型.</typeparam>
    /// <param name="minQ">最小 q 坐标.</param>
    /// <param name="minR">最小 r 坐标.</param>
    /// <param name="width">q 方向单元数量.</param>
    /// <param name="height">r 方向单元数量.</param>
    /// <returns>固定轴向矩形范围的稠密六边形地图.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 width 或 height 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当 width * height 超出 int 范围时抛出.</exception>
    public static DenseAxialHexagonalMap<T> DenseAxial<T>(int minQ, int minR, int width, int height)
    {
        return new DenseAxialHexagonalMap<T>(minQ, minR, width, height);
    }

    /// <summary>
    /// 创建固定轴向矩形范围的稠密六边形地图.
    /// </summary>
    /// <typeparam name="T">地图单元存储的值类型.</typeparam>
    /// <param name="origin">最小 q 和最小 r 组成的左上起点坐标.</param>
    /// <param name="width">q 方向单元数量.</param>
    /// <param name="height">r 方向单元数量.</param>
    /// <returns>固定轴向矩形范围的稠密六边形地图.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 width 或 height 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当 width * height 超出 int 范围时抛出.</exception>
    public static DenseAxialHexagonalMap<T> DenseAxial<T>(HexagonalGrid<int> origin, int width, int height)
    {
        return new DenseAxialHexagonalMap<T>(origin, width, height);
    }

    /// <summary>
    /// 创建以原点为中心的固定半径正六边形地图.
    /// </summary>
    /// <typeparam name="T">地图单元存储的值类型.</typeparam>
    /// <param name="radius">地图半径.</param>
    /// <returns>固定半径正六边形地图.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当单元数量超出 int 范围时抛出.</exception>
    public static DenseHexagonalRadiusMap<T> DenseRadius<T>(int radius)
    {
        return new DenseHexagonalRadiusMap<T>(radius);
    }

    /// <summary>
    /// 创建以指定坐标为中心的固定半径正六边形地图.
    /// </summary>
    /// <typeparam name="T">地图单元存储的值类型.</typeparam>
    /// <param name="center">中心坐标.</param>
    /// <param name="radius">地图半径.</param>
    /// <returns>固定半径正六边形地图.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当单元数量或坐标范围超出 int 范围时抛出.</exception>
    public static DenseHexagonalRadiusMap<T> DenseRadius<T>(HexagonalGrid<int> center, int radius)
    {
        return new DenseHexagonalRadiusMap<T>(center, radius);
    }
}
