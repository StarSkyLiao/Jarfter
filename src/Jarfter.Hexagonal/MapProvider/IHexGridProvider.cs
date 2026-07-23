using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.MapProvider;

/// <summary>
/// 定义以整数六边形坐标索引的地图查询接口.
/// </summary>
/// <typeparam name="T">地图单元存储的值类型.</typeparam>
public interface IHexGridProvider<T>
{
    /// <summary>
    /// 获取网格中包含的单元数量.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 获取或设置指定六边形坐标处的网格元素.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <returns>指定坐标上的值.</returns>
    T this[HexagonalCubePoint position] { get; }

    /// <summary>
    /// 判断地图中是否存在指定坐标.
    /// </summary>
    /// <param name="position">要判断的地图坐标.</param>
    /// <returns>当地图中存在指定坐标时返回 true, 否则返回 false.</returns>
    bool Contains(HexagonalCubePoint position);

    /// <summary>
    /// 尝试获取指定坐标上的值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="value">获取到的单元值.</param>
    /// <returns>当地图中存在指定坐标时返回 true, 否则返回 false.</returns>
    bool TryGetValue(HexagonalCubePoint position, out T? value);

    /// <summary>
    /// 尝试获取指定坐标上的值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="defaultValue">获取失败时的默认值.</param>
    /// <returns>当地图中存在指定坐标时返回值, 否则返回默认值.</returns>
    T? GetValueOrDefault(HexagonalCubePoint position, T? defaultValue = default);
}
