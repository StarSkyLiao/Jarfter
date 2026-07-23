using System.Collections;

namespace Jarfter.Hexagonal.Coordinates;

public readonly partial record struct HexagonalCubePoint
{
    /// <summary>
    /// 获取指定半径上所有点坐标的集合.
    /// </summary>
    public RingCollection RingAt(int radius) => new RingCollection(this, radius);

    /// <summary>
    /// 六边形网格点指定半径上所有点坐标的集合.
    /// 注意: 半径为 0 时并不会返回自身.
    /// </summary>
    public struct RingCollection(HexagonalCubePoint point, int radius) : IEnumerator<HexagonalCubePoint>, IReadOnlyCollection<HexagonalCubePoint>
    {
        private int m_Index = -1;

        /// <inheritdoc />
        public bool MoveNext() => ++m_Index < Count;

        /// <inheritdoc />
        public void Reset() => m_Index = -1;

        /// <inheritdoc />
        public void Dispose() => m_Index = -1;

        /// <inheritdoc />
        public HexagonalCubePoint Current
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)m_Index, (uint)Count);
                if (radius == 0) return point;
                (int side, int step) = Math.DivRem(m_Index, radius);
                HexagonalCubePoint corner = s_Directions[(side + 4) % 6];
                return point + corner * radius + s_Directions[side] * step;
            }
        }

        /// <inheritdoc />
        public int Count { get; } = radius == 0 ? 1 : 6 * radius;

        /// <summary>
        /// 获取无装箱的结构体枚举器.
        /// </summary>
        public RingCollection GetEnumerator() => new RingCollection(point, radius);

        /// <inheritdoc />
        IEnumerator<HexagonalCubePoint> IEnumerable<HexagonalCubePoint>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        object IEnumerator.Current => Current;
    }

}

