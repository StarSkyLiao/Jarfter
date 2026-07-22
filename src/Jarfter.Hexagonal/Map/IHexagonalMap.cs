using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Map;

/// <summary>
/// 定义以整数六边形坐标索引的地图查询接口.
/// </summary>
/// <typeparam name="T">地图单元存储的值类型.</typeparam>
public interface IHexagonalMap<T>
{
    /// <summary>
    /// 获取地图中的单元数量.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 获取地图是否为空.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// 获取指定坐标上的值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <returns>指定坐标上的值.</returns>
    T this[HexagonalGrid<int> position] { get; }

    /// <summary>
    /// 判断地图中是否存在指定坐标.
    /// </summary>
    /// <param name="position">要判断的地图坐标.</param>
    /// <returns>当地图中存在指定坐标时返回 true, 否则返回 false.</returns>
    bool Contains(HexagonalGrid<int> position);

    /// <summary>
    /// 尝试获取指定坐标上的值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="value">获取到的单元值.</param>
    /// <returns>当地图中存在指定坐标时返回 true, 否则返回 false.</returns>
    bool TryGetValue(HexagonalGrid<int> position, out T value);

    /// <summary>
    /// 将指定坐标周围存在于地图中的相邻坐标复制到目标缓冲区.
    /// </summary>
    /// <param name="position">中心坐标.</param>
    /// <param name="destination">用于接收相邻坐标的目标缓冲区.</param>
    /// <returns>写入目标缓冲区的坐标数量.</returns>
    int CopyNeighborsTo(HexagonalGrid<int> position, Span<HexagonalGrid<int>> destination);

    /// <summary>
    /// 将指定坐标周围存在于地图中的相邻单元复制到目标缓冲区.
    /// </summary>
    /// <param name="position">中心坐标.</param>
    /// <param name="destination">用于接收相邻单元的目标缓冲区.</param>
    /// <returns>写入目标缓冲区的单元数量.</returns>
    int CopyNeighborCellsTo(
        HexagonalGrid<int> position,
        Span<KeyValuePair<HexagonalGrid<int>, T>> destination);
}
