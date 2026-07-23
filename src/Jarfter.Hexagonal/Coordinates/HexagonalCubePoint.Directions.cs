using System.Collections;

namespace Jarfter.Hexagonal.Coordinates;

public readonly partial record struct HexagonalCubePoint
{
    /// <summary>
    /// 表示六边形网格中与显示朝向无关的六个拓扑方向.
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// q 分量增加, s 分量减少的方向.
        /// </summary>
        PositiveQ = 0,

        /// <summary>
        /// r 分量减少, q 分量增加的方向.
        /// </summary>
        NegativeR = 1,

        /// <summary>
        /// s 分量增加, r 分量减少的方向.
        /// </summary>
        PositiveS = 2,

        /// <summary>
        /// q 分量减少, s 分量增加的方向.
        /// </summary>
        NegativeQ = 3,

        /// <summary>
        /// r 分量增加, q 分量减少的方向.
        /// </summary>
        PositiveR = 4,

        /// <summary>
        /// s 分量减少, r 分量增加的方向.
        /// </summary>
        NegativeS = 5
    }

    private static readonly HexagonalCubePoint[] s_Directions =
    [
        new HexagonalCubePoint(1, 0),
        new HexagonalCubePoint(1, -1),
        new HexagonalCubePoint(0, -1),
        new HexagonalCubePoint(-1, 0),
        new HexagonalCubePoint(-1, 1),
        new HexagonalCubePoint(0, 1)
    ];

    /// <summary>
    /// 获取当前坐标的邻居集合.
    /// </summary>
    public NeighborCollection Neighbors => new NeighborCollection(this);

    /// <summary>
    /// 获取指定方向的邻居点坐标.
    /// </summary>
    /// <param name="direction">给定的六边形网格方向.</param>
    /// <exception cref="ArgumentOutOfRangeException">给定的六边形网格方向不是合法的方向.</exception>
    public HexagonalCubePoint NeighborAt(Direction direction)
    {
        int index = (int)direction;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, 5);
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        return this + s_Directions[index];
    }

    /// <summary>
    /// 不检查地获取指定方向的邻居点坐标.
    /// </summary>
    /// <param name="direction">给定的六边形网格方向.</param>
    public HexagonalCubePoint NeighborAtUnchecked(int direction) => this + s_Directions[direction];

    /// <summary>
    /// 六边形网格点所有邻居的集合.
    /// </summary>
    public struct NeighborCollection(HexagonalCubePoint point) : IEnumerator<HexagonalCubePoint>, IReadOnlyCollection<HexagonalCubePoint>
    {
        private int m_Index = -1;

        /// <inheritdoc />
        public bool MoveNext() => ++m_Index < 6;

        /// <inheritdoc />
        public void Reset() => m_Index = -1;

        /// <inheritdoc />
        public void Dispose() => m_Index = -1;

        /// <inheritdoc />
        public HexagonalCubePoint Current => point + s_Directions[m_Index];

        /// <inheritdoc />
        public int Count => 6;

        /// <summary>
        /// 获取无装箱的结构体枚举器.
        /// </summary>
        public NeighborCollection GetEnumerator() => new NeighborCollection(point);

        /// <inheritdoc />
        IEnumerator<HexagonalCubePoint> IEnumerable<HexagonalCubePoint>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        object IEnumerator.Current => Current;
    }

}

