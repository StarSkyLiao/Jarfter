using System.Collections;
using Jarfter.Hexagonal.Direction;

namespace Jarfter.Hexagonal.Grid;

public partial record struct HexagonalGrid<T>
{
    private static readonly HexagonalGrid<T>[] s_Directions =
    [
        new HexagonalGrid<T>(T.One, T.Zero),
        new HexagonalGrid<T>(T.One, -T.One),
        new HexagonalGrid<T>(T.Zero, -T.One),
        new HexagonalGrid<T>(-T.One, T.Zero),
        new HexagonalGrid<T>(-T.One, T.One),
        new HexagonalGrid<T>(T.Zero, T.One)
    ];

    /// <summary>
    /// 获取指定方向对应的单位坐标偏移.
    /// </summary>
    /// <param name="direction">六边形拓扑方向.</param>
    /// <returns>指定方向上的单位坐标偏移.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 direction 不是有效方向时抛出.</exception>
    public static HexagonalGrid<T> Direction(HexagonalDirection direction)
    {
        int index = (int)direction;
        if ((uint)index < 6)
        {
            return s_Directions[index];
        }

        throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.");
    }

    /// <summary>
    /// 获取当前坐标在指定方向上的相邻坐标.
    /// </summary>
    /// <param name="direction">相邻方向.</param>
    /// <returns>指定方向上的相邻坐标.</returns>
    public HexagonalGrid<T> Neighbor(HexagonalDirection direction)
    {
        return this + Direction(direction);
    }

    /// <summary>
    /// 获取当前坐标在指定方向和距离上的坐标.
    /// </summary>
    /// <param name="direction">移动方向.</param>
    /// <param name="distance">移动距离.</param>
    /// <returns>指定方向和距离上的坐标.</returns>
    public HexagonalGrid<T> Neighbor(HexagonalDirection direction, T distance)
    {
        return this + Direction(direction) * distance;
    }

    /// <summary>
    /// 获取当前坐标在指定方向和距离上的坐标.
    /// </summary>
    /// <param name="direction">移动方向.</param>
    /// <param name="distance">移动距离.</param>
    /// <returns>指定方向和距离上的坐标.</returns>
    public HexagonalGrid<T> Neighbor(HexagonalDirection direction, int distance)
    {
        return Neighbor(direction, T.CreateChecked(distance));
    }

    /// <summary>
    /// 枚举当前坐标周围的六个相邻坐标.
    /// </summary>
    /// <returns>以逆时针方向排列的六个相邻坐标.</returns>
    public NeighborEnumerable Neighbors()
    {
        return new NeighborEnumerable(this);
    }

    /// <summary>
    /// 将当前坐标周围的六个相邻坐标复制到目标缓冲区.
    /// </summary>
    /// <param name="destination">用于接收相邻坐标的目标缓冲区.</param>
    /// <exception cref="ArgumentException">当 destination 的长度小于 6 时抛出.</exception>
    public void CopyNeighborsTo(Span<HexagonalGrid<T>> destination)
    {
        if (destination.Length < 6)
        {
            throw new ArgumentException("Destination must contain at least 6 elements.", nameof(destination));
        }

        T one = T.One;

        destination[0] = new HexagonalGrid<T>(Q + one, R);
        destination[1] = new HexagonalGrid<T>(Q + one, R - one);
        destination[2] = new HexagonalGrid<T>(Q, R - one);
        destination[3] = new HexagonalGrid<T>(Q - one, R);
        destination[4] = new HexagonalGrid<T>(Q - one, R + one);
        destination[5] = new HexagonalGrid<T>(Q, R + one);
    }

    /// <summary>
    /// 判断指定坐标是否与当前坐标相邻.
    /// </summary>
    /// <param name="other">要判断的另一个坐标.</param>
    /// <returns>当两个坐标的六边形距离为 1 时返回 true, 否则返回 false.</returns>
    public bool IsNeighborOf(HexagonalGrid<T> other)
    {
        return DistanceTo(other) == T.One;
    }

    /// <summary>
    /// 获取从当前坐标到指定坐标的直线拓扑方向.
    /// </summary>
    /// <param name="other">目标坐标.</param>
    /// <returns>从当前坐标指向目标坐标的方向.</returns>
    /// <exception cref="ArgumentException">当目标坐标与当前坐标相同, 或目标坐标不在六个直线方向上时抛出.</exception>
    public HexagonalDirection DirectionTo(HexagonalGrid<T> other)
    {
        if (TryGetDirectionTo(other, out HexagonalDirection direction))
        {
            return direction;
        }

        throw new ArgumentException(
            "The target cell is not aligned with this cell in a hex direction.",
            nameof(other));
    }

    /// <summary>
    /// 尝试获取从当前坐标到指定坐标的直线拓扑方向.
    /// </summary>
    /// <param name="other">目标坐标.</param>
    /// <param name="direction">从当前坐标指向目标坐标的方向.</param>
    /// <returns>当目标坐标在当前坐标的六个直线方向上时返回 true, 否则返回 false.</returns>
    public bool TryGetDirectionTo(HexagonalGrid<T> other, out HexagonalDirection direction)
    {
        HexagonalGrid<T> delta = other - this;

        if (delta.R == T.Zero && delta.Q > T.Zero)
        {
            direction = HexagonalDirection.PositiveQ;
            return true;
        }

        if (delta.Q + delta.R == T.Zero && delta.Q > T.Zero)
        {
            direction = HexagonalDirection.NegativeR;
            return true;
        }

        if (delta.Q == T.Zero && delta.R < T.Zero)
        {
            direction = HexagonalDirection.PositiveS;
            return true;
        }

        if (delta.R == T.Zero && delta.Q < T.Zero)
        {
            direction = HexagonalDirection.NegativeQ;
            return true;
        }

        if (delta.Q + delta.R == T.Zero && delta.Q < T.Zero)
        {
            direction = HexagonalDirection.PositiveR;
            return true;
        }

        if (delta.Q == T.Zero && delta.R > T.Zero)
        {
            direction = HexagonalDirection.NegativeS;
            return true;
        }

        direction = default;
        return false;
    }

    /// <summary>
    /// 表示当前坐标周围六个相邻坐标的无分配可枚举集合.
    /// 直接使用 <c>foreach</c> 遍历时不会创建枚举器对象.
    /// </summary>
    public readonly struct NeighborEnumerable : IEnumerable<HexagonalGrid<T>>
    {
        private readonly HexagonalGrid<T> m_Cell;

        /// <summary>
        /// 使用指定中心坐标创建相邻坐标集合.
        /// </summary>
        /// <param name="cell">中心坐标.</param>
        internal NeighborEnumerable(HexagonalGrid<T> cell)
        {
            m_Cell = cell;
        }

        /// <summary>
        /// 返回用于无分配遍历相邻坐标的枚举器.
        /// </summary>
        /// <returns>相邻坐标枚举器.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(m_Cell);
        }

        IEnumerator<HexagonalGrid<T>> IEnumerable<HexagonalGrid<T>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 以逆时针方向遍历六个相邻坐标的枚举器.
        /// </summary>
        public struct Enumerator : IEnumerator<HexagonalGrid<T>>
        {
            private readonly HexagonalGrid<T> m_Cell;
            private readonly T m_One;
            private int m_Index;

            /// <summary>
            /// 使用指定中心坐标创建枚举器.
            /// </summary>
            /// <param name="cell">中心坐标.</param>
            internal Enumerator(HexagonalGrid<T> cell)
            {
                m_Cell = cell;
                m_One = T.One;
                m_Index = -1;
            }

            /// <inheritdoc />
            public HexagonalGrid<T> Current
            {
                get
                {
                    if ((uint)m_Index >= 6)
                    {
                        throw new InvalidOperationException("Enumerator is not positioned on a neighbor.");
                    }

                    return m_Index switch
                    {
                        0 => new HexagonalGrid<T>(m_Cell.Q + m_One, m_Cell.R),
                        1 => new HexagonalGrid<T>(m_Cell.Q + m_One, m_Cell.R - m_One),
                        2 => new HexagonalGrid<T>(m_Cell.Q, m_Cell.R - m_One),
                        3 => new HexagonalGrid<T>(m_Cell.Q - m_One, m_Cell.R),
                        4 => new HexagonalGrid<T>(m_Cell.Q - m_One, m_Cell.R + m_One),
                        _ => new HexagonalGrid<T>(m_Cell.Q, m_Cell.R + m_One)
                    };
                }
            }

            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public bool MoveNext()
            {
                int index = m_Index + 1;
                if ((uint)index >= 6)
                {
                    m_Index = 6;
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
}
