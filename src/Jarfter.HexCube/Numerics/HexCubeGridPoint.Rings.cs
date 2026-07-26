using System.Collections;

namespace Jarfter.HexCube.Numerics;

public readonly partial record struct HexCubeGridPoint
{
    /// <summary>
    /// 获取指定半径上所有整数网格坐标的集合.
    /// </summary>
    /// <param name="radius">环的六边形半径.</param>
    /// <returns>指定半径上的坐标集合.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 <paramref name="radius"/> 小于 0 时抛出.</exception>
    public RingCollection RingAt(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
        return new RingCollection(this, radius);
    }

    /// <summary>
    /// 六边形网格点指定半径上所有整数坐标的集合.
    /// 半径为 0 时只包含当前坐标.
    /// </summary>
    public struct RingCollection(HexCubeGridPoint point, int radius) : IEnumerator<HexCubeGridPoint>, IReadOnlyCollection<HexCubeGridPoint>
    {
        private int m_Index = -1;

        /// <inheritdoc />
        public bool MoveNext() => ++m_Index < Count;

        /// <inheritdoc />
        public void Reset() => m_Index = -1;

        /// <inheritdoc />
        public void Dispose() => m_Index = -1;

        /// <inheritdoc />
        public HexCubeGridPoint Current
        {
            get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)m_Index, (uint)Count);
                if (radius == 0) return point;
                (int side, int step) = Math.DivRem(m_Index, radius);
                HexCubeGridPoint corner = s_Directions[(side + 4) % 6];
                return point + corner * radius + s_Directions[side] * step;
            }
        }

        /// <inheritdoc />
        public int Count { get; } = radius == 0 ? 1 : checked(6 * radius);

        /// <summary>
        /// 获取无装箱的结构体枚举器.
        /// </summary>
        /// <returns>环坐标集合的结构体枚举器.</returns>
        public RingCollection GetEnumerator() => this;

        /// <inheritdoc />
        IEnumerator<HexCubeGridPoint> IEnumerable<HexCubeGridPoint>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        object IEnumerator.Current => Current;
    }
}
