using System.Collections;
using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Map;

/// <summary>
/// 表示以连续数组存储的固定轴向矩形六边形地图.
/// </summary>
/// <typeparam name="T">地图单元存储的值类型.</typeparam>
public sealed class DenseAxialHexagonalMap<T> : IReadOnlyHexagonalMap<T>
{
    private const int NeighborCount = 6;
    private readonly T[] m_Cells;

    /// <summary>
    /// 创建固定轴向矩形范围的稠密六边形地图.
    /// </summary>
    /// <param name="minQ">最小 q 坐标.</param>
    /// <param name="minR">最小 r 坐标.</param>
    /// <param name="width">q 方向单元数量.</param>
    /// <param name="height">r 方向单元数量.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 width 或 height 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当 width * height 超出 int 范围时抛出.</exception>
    public DenseAxialHexagonalMap(int minQ, int minR, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);
        ValidateBounds(minQ, width);
        ValidateBounds(minR, height);

        MinQ = minQ;
        MinR = minR;
        Width = width;
        Height = height;
        m_Cells = new T[checked(width * height)];
    }

    /// <summary>
    /// 创建固定轴向矩形范围的稠密六边形地图.
    /// </summary>
    /// <param name="origin">最小 q 和最小 r 组成的起点坐标.</param>
    /// <param name="width">q 方向单元数量.</param>
    /// <param name="height">r 方向单元数量.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 width 或 height 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当 width * height 超出 int 范围时抛出.</exception>
    public DenseAxialHexagonalMap(HexagonalGrid<int> origin, int width, int height)
        : this(origin.Q, origin.R, width, height)
    {
    }

    /// <summary>
    /// 获取最小 q 坐标.
    /// </summary>
    public int MinQ { get; }

    /// <summary>
    /// 获取最小 r 坐标.
    /// </summary>
    public int MinR { get; }

    /// <summary>
    /// 获取 q 方向单元数量.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// 获取 r 方向单元数量.
    /// </summary>
    public int Height { get; }

    /// <inheritdoc />
    public int Count => m_Cells.Length;

    /// <inheritdoc />
    public bool IsEmpty => m_Cells.Length == 0;

    /// <summary>
    /// 获取地图中存在的全部坐标.
    /// </summary>
    public IEnumerable<HexagonalGrid<int>> Positions
    {
        get
        {
            for (int r = 0; r < Height; r++)
            {
                for (int q = 0; q < Width; q++)
                {
                    yield return new HexagonalGrid<int>(MinQ + q, MinR + r);
                }
            }
        }
    }

    /// <summary>
    /// 获取地图中存在的全部值.
    /// </summary>
    public IEnumerable<T> Values => m_Cells;

    /// <summary>
    /// 获取或设置指定坐标上的值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <returns>指定坐标上的值.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 position 不在地图范围内时抛出.</exception>
    public T this[HexagonalGrid<int> position]
    {
        get => m_Cells[GetIndexOrThrow(position)];
        set => m_Cells[GetIndexOrThrow(position)] = value;
    }

    /// <inheritdoc />
    public bool Contains(HexagonalGrid<int> position)
    {
        long q = (long)position.Q - MinQ;
        long r = (long)position.R - MinR;
        return (ulong)q < (uint)Width && (ulong)r < (uint)Height;
    }

    /// <inheritdoc />
    public bool TryGetValue(HexagonalGrid<int> position, out T value)
    {
        if (TryGetIndex(position, out int index))
        {
            value = m_Cells[index];
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// 设置指定坐标上的值.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="value">单元值.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 position 不在地图范围内时抛出.</exception>
    public void Set(HexagonalGrid<int> position, T value)
    {
        m_Cells[GetIndexOrThrow(position)] = value;
    }

    /// <summary>
    /// 将所有单元设置为指定值.
    /// </summary>
    /// <param name="value">要填充的单元值.</param>
    public void Fill(T value)
    {
        Array.Fill(m_Cells, value);
    }

    /// <summary>
    /// 获取连续存储的单元值.
    /// </summary>
    /// <returns>表示底层连续数组的 span.</returns>
    public Span<T> AsSpan()
    {
        return m_Cells;
    }

    /// <summary>
    /// 获取连续存储的只读单元值.
    /// </summary>
    /// <returns>表示底层连续数组的只读 span.</returns>
    public ReadOnlySpan<T> AsReadOnlySpan()
    {
        return m_Cells;
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
            if (position.TryGetNeighbor(directionIndex, out HexagonalGrid<int> neighbor) && Contains(neighbor))
            {
                yield return neighbor;
            }
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
            if (!TryGetIndex(neighbor, out int index)) continue;
            yield return new KeyValuePair<HexagonalGrid<int>, T>(neighbor, m_Cells[index]);
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
            if (!TryGetIndex(neighbor, out int index)) continue;
            destination[count] = new KeyValuePair<HexagonalGrid<int>, T>(neighbor, m_Cells[index]);
            count++;
        }

        return count;
    }

    /// <summary>
    /// 尝试获取指定坐标对应的连续数组索引.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="index">连续数组索引.</param>
    /// <returns>当指定坐标在地图范围内时返回 true, 否则返回 false.</returns>
    public bool TryGetIndex(HexagonalGrid<int> position, out int index)
    {
        long q = (long)position.Q - MinQ;
        long r = (long)position.R - MinR;
        if ((ulong)q < (uint)Width && (ulong)r < (uint)Height)
        {
            index = (int)r * Width + (int)q;
            return true;
        }

        index = 0;
        return false;
    }

    /// <summary>
    /// 返回用于无分配遍历地图单元的枚举器.
    /// </summary>
    /// <returns>地图单元枚举器.</returns>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<KeyValuePair<HexagonalGrid<int>, T>> IEnumerable<KeyValuePair<HexagonalGrid<int>, T>>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private int GetIndexOrThrow(HexagonalGrid<int> position)
    {
        if (TryGetIndex(position, out int index))
        {
            return index;
        }

        throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the dense map bounds.");
    }

    private static void ValidateBounds(int min, int length)
    {
        if (length == 0) return;

        _ = checked(min + length - 1);
    }

    /// <summary>
    /// 以行优先顺序遍历稠密轴向地图单元的枚举器.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<HexagonalGrid<int>, T>>
    {
        private readonly DenseAxialHexagonalMap<T> m_Map;
        private int m_Index;

        /// <summary>
        /// 使用指定地图创建枚举器.
        /// </summary>
        /// <param name="map">要遍历的地图.</param>
        public Enumerator(DenseAxialHexagonalMap<T> map)
        {
            m_Map = map;
            m_Index = -1;
        }

        /// <inheritdoc />
        public KeyValuePair<HexagonalGrid<int>, T> Current
        {
            get
            {
                if ((uint)m_Index >= (uint)m_Map.m_Cells.Length)
                {
                    throw new InvalidOperationException("Enumerator is not positioned on a map cell.");
                }

                int q = m_Index % m_Map.Width;
                int r = m_Index / m_Map.Width;
                HexagonalGrid<int> position = new HexagonalGrid<int>(m_Map.MinQ + q, m_Map.MinR + r);
                return new KeyValuePair<HexagonalGrid<int>, T>(position, m_Map.m_Cells[m_Index]);
            }
        }

        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            int index = m_Index + 1;
            if ((uint)index >= (uint)m_Map.m_Cells.Length)
            {
                m_Index = m_Map.m_Cells.Length;
                return false;
            }

            m_Index = index;
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_Index = -1;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
