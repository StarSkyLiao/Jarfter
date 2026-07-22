using System.Collections;
using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Map;

/// <summary>
/// 表示以连续数组存储的固定半径正六边形地图.
/// </summary>
/// <typeparam name="T">地图单元存储的值类型.</typeparam>
public sealed class DenseHexagonalRadiusMap<T> : IReadOnlyHexagonalMap<T>
{
    private const int NeighborCount = 6;
    private readonly T[] m_Cells;
    private readonly int[] m_RowQStarts;
    private readonly int[] m_RowLengths;
    private readonly int[] m_RowStarts;

    /// <summary>
    /// 创建以原点为中心的固定半径正六边形地图.
    /// </summary>
    /// <param name="radius">地图半径.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当单元数量超出 int 范围时抛出.</exception>
    public DenseHexagonalRadiusMap(int radius)
        : this(HexagonalGrid<int>.Zero, radius)
    {
    }

    /// <summary>
    /// 创建以指定坐标为中心的固定半径正六边形地图.
    /// </summary>
    /// <param name="center">中心坐标.</param>
    /// <param name="radius">地图半径.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    /// <exception cref="OverflowException">当单元数量或坐标范围超出 int 范围时抛出.</exception>
    public DenseHexagonalRadiusMap(HexagonalGrid<int> center, int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        ValidateBounds(center, radius);

        Center = center;
        Radius = radius;

        int rowCount = checked(radius * 2 + 1);
        m_RowQStarts = new int[rowCount];
        m_RowLengths = new int[rowCount];
        m_RowStarts = new int[rowCount];

        int count = 0;
        for (int row = 0; row < rowCount; row++)
        {
            int dr = row - radius;
            int qStart = Math.Max(-radius, -radius - dr);
            int qEnd = Math.Min(radius, radius - dr);
            int rowLength = qEnd - qStart + 1;

            m_RowQStarts[row] = qStart;
            m_RowLengths[row] = rowLength;
            m_RowStarts[row] = count;
            count = checked(count + rowLength);
        }

        m_Cells = new T[count];
    }

    /// <summary>
    /// 获取地图中心坐标.
    /// </summary>
    public HexagonalGrid<int> Center { get; }

    /// <summary>
    /// 获取地图半径.
    /// </summary>
    public int Radius { get; }

    /// <inheritdoc />
    public int Count => m_Cells.Length;

    /// <inheritdoc />
    public bool IsEmpty => m_Cells.Length == 0;

    /// <summary>
    /// 获取地图中全部坐标.
    /// </summary>
    public IEnumerable<HexagonalGrid<int>> Positions
    {
        get
        {
            for (int row = 0; row < m_RowStarts.Length; row++)
            {
                int dr = row - Radius;
                int rowLength = m_RowLengths[row];
                int qStart = m_RowQStarts[row];

                for (int column = 0; column < rowLength; column++)
                {
                    yield return new HexagonalGrid<int>(
                        Center.Q + qStart + column,
                        Center.R + dr);
                }
            }
        }
    }

    /// <summary>
    /// 获取地图中全部单元值.
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
        return TryGetIndex(position, out _);
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

    /// <summary>
    /// 尝试获取指定坐标对应的连续数组索引.
    /// </summary>
    /// <param name="position">地图坐标.</param>
    /// <param name="index">连续数组索引.</param>
    /// <returns>当指定坐标在地图范围内时返回 true, 否则返回 false.</returns>
    public bool TryGetIndex(HexagonalGrid<int> position, out int index)
    {
        long dq = (long)position.Q - Center.Q;
        long dr = (long)position.R - Center.R;
        long row = dr + Radius;

        if ((ulong)row >= (uint)m_RowStarts.Length)
        {
            index = 0;
            return false;
        }

        int rowIndex = (int)row;
        long column = dq - m_RowQStarts[rowIndex];
        if ((ulong)column < (uint)m_RowLengths[rowIndex])
        {
            index = m_RowStarts[rowIndex] + (int)column;
            return true;
        }

        index = 0;
        return false;
    }

    private int GetIndexOrThrow(HexagonalGrid<int> position)
    {
        if (TryGetIndex(position, out int index))
        {
            return index;
        }

        throw new ArgumentOutOfRangeException(nameof(position), position, "Position is outside the radius map bounds.");
    }

    private static void ValidateBounds(HexagonalGrid<int> center, int radius)
    {
        _ = checked(center.Q + radius);
        _ = checked(center.Q - radius);
        _ = checked(center.R + radius);
        _ = checked(center.R - radius);
    }

    /// <summary>
    /// 按行优先顺序遍历稠密半径地图单元的枚举器.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<HexagonalGrid<int>, T>>
    {
        private readonly DenseHexagonalRadiusMap<T> m_Map;
        private int m_Row;
        private int m_Column;
        private int m_Index;

        /// <summary>
        /// 使用指定地图创建枚举器.
        /// </summary>
        /// <param name="map">要遍历的地图.</param>
        public Enumerator(DenseHexagonalRadiusMap<T> map)
        {
            m_Map = map;
            m_Row = 0;
            m_Column = -1;
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

                HexagonalGrid<int> position = new HexagonalGrid<int>(
                    m_Map.Center.Q + m_Map.m_RowQStarts[m_Row] + m_Column,
                    m_Map.Center.R + m_Row - m_Map.Radius
                );
                return new KeyValuePair<HexagonalGrid<int>, T>(position, m_Map.m_Cells[m_Index]);
            }
        }

        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if ((uint)m_Row >= (uint)m_Map.m_RowStarts.Length)
            {
                return false;
            }

            int column = m_Column + 1;
            if (column < m_Map.m_RowLengths[m_Row])
            {
                m_Column = column;
                m_Index = m_Map.m_RowStarts[m_Row] + column;
                return true;
            }

            int row = m_Row + 1;
            if ((uint)row >= (uint)m_Map.m_RowStarts.Length)
            {
                m_Row = m_Map.m_RowStarts.Length;
                m_Index = m_Map.m_Cells.Length;
                return false;
            }

            m_Row = row;
            m_Column = 0;
            m_Index = m_Map.m_RowStarts[row];
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_Row = 0;
            m_Column = -1;
            m_Index = -1;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
