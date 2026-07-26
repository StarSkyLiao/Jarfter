using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Grids;

/// <summary>
/// 定义以几何六边形坐标索引的二维地图查询接口.
/// 该接口用于分数坐标的几何采样, 不保证查询位置对应离散网格单元中心.
/// </summary>
/// <typeparam name="T">地图单元存储的值类型.</typeparam>
public interface IHexGrid2D<T>
{
    /// <summary>
    /// 获取地图中包含的元素数量.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 获取或设置指定几何坐标处的地图元素.
    /// </summary>
    /// <param name="position">几何地图坐标.</param>
    /// <returns>指定坐标上的值.</returns>
    T this[HexCubePoint position] { get; }

    /// <summary>
    /// 判断地图中是否存在指定几何坐标.
    /// </summary>
    /// <param name="position">要判断的几何地图坐标.</param>
    /// <returns>当地图中存在指定坐标时返回 true, 否则返回 false.</returns>
    bool Contains(HexCubePoint position);

    /// <summary>
    /// 尝试获取指定几何坐标上的值.
    /// </summary>
    /// <param name="position">几何地图坐标.</param>
    /// <param name="value">获取到的单元值.</param>
    /// <returns>当地图中存在指定坐标时返回 true, 否则返回 false.</returns>
    bool TryGetValue(HexCubePoint position, out T? value);

    /// <summary>
    /// 尝试获取指定几何坐标上的值.
    /// </summary>
    /// <param name="position">几何地图坐标.</param>
    /// <param name="defaultValue">获取失败时的默认值.</param>
    /// <returns>当地图中存在指定坐标时返回值, 否则返回默认值.</returns>
    T? GetValueOrDefault(HexCubePoint position, T? defaultValue = default);
}
