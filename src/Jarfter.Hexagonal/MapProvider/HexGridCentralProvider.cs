using Jarfter.Hexagonal.Coordinates;

namespace Jarfter.Hexagonal.MapProvider;

/// <summary>
/// 一个以中心点为原点, 由六边形网格单元组成的有限区域.
/// </summary>
/// <typeparam name="TElement">
/// 网格单元中存储的元素类型.
/// </typeparam>
/// <param name="radius">
/// 网格区域半径, 即距离中心点的最大六边形距离.
/// </param>
public partial class HexGridCentralProvider<TElement>(int radius) : IHexGridProvider<TElement>
{
    /// <summary>
    /// 获取该六边形网格的半径.
    /// 半径表示中心单元到最外围单元的最大六边形距离.
    /// 半径为 0 时, 网格仅包含中心单元.
    /// </summary>
    public int Radius { get; } = radius >= 0 ? radius : throw new ArgumentOutOfRangeException(nameof(radius));

    /// <inheritdoc />
    public int Count { get; } = 1 + 3 * radius + 3 * radius * radius;

    /// <summary>
    /// 存储所有网格单元元素的连续数组.
    /// 元素顺序由六边形坐标索引规则确定.
    /// </summary>
    internal readonly TElement[] InternalElements = new TElement[1 + 3 * radius + 3 * radius * radius];

    /// <summary>
    /// 获取网格中所有元素的只读集合.
    /// </summary>
    public ReadOnlySpan<TElement> Elements => InternalElements;

    /// <inheritdoc />
    public TElement this[HexagonalCubePoint cubePoint]
    {
        get => InternalElements[ToIndex(cubePoint)];
        set => InternalElements[ToIndex(cubePoint)] = value;
    }

    /// <inheritdoc />
    public bool Contains(HexagonalCubePoint position)
    {
        int index = ToIndex(position);
        return index >= 0 && index < Count;
    }

    /// <inheritdoc />
    public bool TryGetValue(HexagonalCubePoint position, out TElement? value)
    {
        int index = ToIndex(position);
        if (index < 0 || index >= Count)
        {
            value = default;
            return false;
        }

        value = InternalElements[index];
        return true;
    }

    /// <inheritdoc />
    public TElement? GetValueOrDefault(HexagonalCubePoint position, TElement? defaultValue = default)
    {
        int index = ToIndex(position);
        if (index >= 0 && index < Count) return InternalElements[index];
        return defaultValue;
    }
}
