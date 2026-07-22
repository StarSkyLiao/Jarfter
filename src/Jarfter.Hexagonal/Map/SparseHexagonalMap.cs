using System.Collections;
using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Map;

/// <summary>
/// 表示以字典存储的稀疏六边形地图.
/// </summary>
/// <typeparam name="T">地图单元存储的值类型.</typeparam>
public sealed class SparseHexagonalMap<T> : IReadOnlyHexagonalMap<T>
{
    private const int NeighborCount = 6;
    private readonly Dictionary<HexagonalGrid<int>, T> m_Cells;

    /// <summary>
    /// 创建空的稀疏六边形地图.
    /// </summary>
    public SparseHexagonalMap()
    {
        m_Cells = new Dictionary<HexagonalGrid<int>, T>();
    }

    /// <summary>
    /// 创建具有指定初始容量的稀疏六边形地图.
    /// </summary>
    /// <param name="capacity">初始容量.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 capacity 小于 0 时抛出.</exception>
    public SparseHexagonalMap(int capacity)
    {
        m_Cells = new Dictionary<HexagonalGrid<int>, T>(capacity);
    }

    /// <summary>
    /// 使用指定单元创建稀疏六边形地图.
    /// </summary>
    /// <param name="cells">初始地图单元.</param>
    /// <exception cref="ArgumentNullException">当 cells 为 null 时抛出.</exception>
    public SparseHexagonalMap(IEnumerable<KeyValuePair<HexagonalGrid<int>, T>> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        m_Cells = new Dictionary<HexagonalGrid<int>, T>(cells);
    }

    /// <inheritdoc />
    public int Count => m_Cells.Count;

    /// <inheritdoc />
    public bool IsEmpty => m_Cells.Count == 0;

    /// <summary>
    /// 获取地图中存在的全部坐标.
    /// </summary>
    public IEnumerable<HexagonalGrid<int>> Positions => m_Cells.Keys;

    /// <summary>
    /// 获取地图中存在的全部值.
    /// </summary>
    public IEnumerable<T> Values => m_Cells.Values;

    /// <summary>
    /// 获取或设置指定坐标上的值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <returns>指定坐标上的值.</returns>
    public T this[HexagonalGrid<int> position]
    {
        get => m_Cells[position];
        set => m_Cells[position] = value;
    }

    /// <summary>
    /// 尝试向地图添加指定坐标和值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="value">单元值.</param>
    /// <returns>添加成功时返回 true, 当指定坐标已存在时返回 false.</returns>
    public bool TryAdd(HexagonalGrid<int> position, T value)
    {
        return m_Cells.TryAdd(position, value);
    }

    /// <summary>
    /// 向地图添加指定坐标和值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="value">单元值.</param>
    /// <exception cref="ArgumentException">当指定坐标已存在时抛出.</exception>
    public void Add(HexagonalGrid<int> position, T value)
    {
        m_Cells.Add(position, value);
    }

    /// <summary>
    /// 设置指定坐标上的值, 不存在时创建该坐标.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="value">单元值.</param>
    public void Set(HexagonalGrid<int> position, T value)
    {
        m_Cells[position] = value;
    }

    /// <summary>
    /// 移除指定坐标.
    /// </summary>
    /// <param name="position">要移除的地图坐标.</param>
    /// <returns>移除成功时返回 true, 当指定坐标不存在时返回 false.</returns>
    public bool Remove(HexagonalGrid<int> position)
    {
        return m_Cells.Remove(position);
    }

    /// <summary>
    /// 清空地图.
    /// </summary>
    public void Clear()
    {
        m_Cells.Clear();
    }

    /// <summary>
    /// 确保字典容量至少可容纳指定数量的单元.
    /// </summary>
    /// <param name="capacity">目标容量.</param>
    /// <returns>当前字典容量.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 capacity 小于 0 时抛出.</exception>
    public int EnsureCapacity(int capacity)
    {
        return m_Cells.EnsureCapacity(capacity);
    }

    /// <summary>
    /// 将字典容量裁剪为当前单元数量.
    /// </summary>
    public void TrimExcess()
    {
        m_Cells.TrimExcess();
    }

    /// <inheritdoc />
    public bool Contains(HexagonalGrid<int> position)
    {
        return m_Cells.ContainsKey(position);
    }

    /// <inheritdoc />
    public bool TryGetValue(HexagonalGrid<int> position, out T value)
    {
        return m_Cells.TryGetValue(position, out value!);
    }

    /// <summary>
    /// 枚举指定坐标周围存在于地图中的相邻坐标.
    /// </summary>
    /// <param name="position">中心坐标.</param>
    /// <returns>地图中存在的相邻坐标.</returns>
    public IEnumerable<HexagonalGrid<int>> GetNeighbors(HexagonalGrid<int> position)
    {
        for (int directionIndex = 0; directionIndex < NeighborCount; directionIndex++)
        {
            if (!position.TryGetNeighbor(directionIndex, out HexagonalGrid<int> neighbor)) continue;
            if (!Contains(neighbor)) continue;
            yield return neighbor;
        }
    }

    /// <summary>
    /// 枚举指定坐标周围存在于地图中的相邻单元.
    /// </summary>
    /// <param name="position">中心坐标.</param>
    /// <returns>地图中存在的相邻单元.</returns>
    public IEnumerable<KeyValuePair<HexagonalGrid<int>, T>> GetNeighborCells(HexagonalGrid<int> position)
    {
        for (int directionIndex = 0; directionIndex < NeighborCount; directionIndex++)
        {
            if (!position.TryGetNeighbor(directionIndex, out HexagonalGrid<int> neighbor)) continue;
            if (!m_Cells.TryGetValue(neighbor, out T? value)) continue;
            yield return new KeyValuePair<HexagonalGrid<int>, T>(neighbor, value);
        }
    }

    /// <inheritdoc />
    public int CopyNeighborsTo(HexagonalGrid<int> position, Span<HexagonalGrid<int>> destination)
    {
        if (destination.Length < NeighborCount)
        {
            throw new ArgumentException("Destination must contain at least 6 elements.", nameof(destination));
        }

        int count = 0;
        for (int directionIndex = 0; directionIndex < NeighborCount; directionIndex++)
        {
            if (!position.TryGetNeighbor(directionIndex, out HexagonalGrid<int> neighbor)) continue;
            if (!Contains(neighbor)) continue;
            destination[count] = neighbor;
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public int CopyNeighborCellsTo(
        HexagonalGrid<int> position,
        Span<KeyValuePair<HexagonalGrid<int>, T>> destination)
    {
        if (destination.Length < NeighborCount)
        {
            throw new ArgumentException("Destination must contain at least 6 elements.", nameof(destination));
        }

        int count = 0;
        for (int directionIndex = 0; directionIndex < NeighborCount; directionIndex++)
        {
            if (!position.TryGetNeighbor(directionIndex, out HexagonalGrid<int> neighbor)) continue;
            if (!m_Cells.TryGetValue(neighbor, out T? value)) continue;
            destination[count] = new KeyValuePair<HexagonalGrid<int>, T>(neighbor, value);
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public Dictionary<HexagonalGrid<int>, T>.Enumerator GetEnumerator()
    {
        return m_Cells.GetEnumerator();
    }

    IEnumerator<KeyValuePair<HexagonalGrid<int>, T>> IEnumerable<KeyValuePair<HexagonalGrid<int>, T>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
