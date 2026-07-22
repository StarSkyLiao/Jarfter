using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Map;

/// <summary>
/// 定义可遍历的只读六边形地图查询接口.
/// 该接口在 <see cref="IHexagonalMap{T}"/> 的坐标查询能力之上提供全部单元的枚举,
/// 适用于寻路, 序列化和通用地图算法.
/// </summary>
/// <typeparam name="T">地图单元存储的值类型.</typeparam>
public interface IReadOnlyHexagonalMap<T> : IHexagonalMap<T>, IEnumerable<KeyValuePair<HexagonalGrid<int>, T>>
{
}
