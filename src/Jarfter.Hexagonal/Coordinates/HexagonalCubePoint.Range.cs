using System.Collections;

namespace Jarfter.Hexagonal.Coordinates;

public readonly partial record struct HexagonalCubePoint
{
    /// <summary>
    /// 获取指定范围内的所有六边形网格点, 包含自身.
    /// </summary>
    public RangeCollection RangeIn(int radius) => new RangeCollection(this, radius);

    /// <summary>
    /// 六边形网格点指定半径范围内所有点坐标的集合.
    /// 包含与中心点距离小于或等于 <see cref="Radius"/> 的所有点.
    /// 注意: 总是会返回自身.
    /// </summary>
    public struct RangeCollection : IEnumerator<HexagonalCubePoint>, IReadOnlyCollection<HexagonalCubePoint>
    {
        private readonly HexagonalCubePoint m_Center;

        // -1 表示尚未开始枚举;
        //  0 表示当前位于中心点;
        // >0 表示当前位于对应的环.
        private int m_Ring;

        // 当前环的边索引, 范围为 [0, 5].
        private int m_Side;

        // 当前边已经返回的点数, 范围为 [0, m_Ring).
        private int m_Step;

        private HexagonalCubePoint m_Current;

        /// <summary>
        /// 初始化指定中心点和半径的范围集合.
        /// </summary>
        public RangeCollection(HexagonalCubePoint center, int radius)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(radius);

            m_Center = center;

            Radius = radius;
            Count = checked(1 + 3 * radius * (radius + 1));

            m_Ring = -1;
            m_Side = 0;
            m_Step = 0;
            m_Current = default;
        }

        /// <summary>
        /// 获取范围半径.
        /// </summary>
        public int Radius { get; }

        /// <inheritdoc />
        public int Count { get; }

        /// <inheritdoc />
        public HexagonalCubePoint Current
        {
            get
            {
                if (m_Ring >= 0 && m_Ring <= Radius) return m_Current;
                throw new InvalidOperationException("枚举器尚未开始枚举, 或者枚举已经结束.");
            }
        }

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            switch (m_Ring)
            {
                // 第一次枚举时返回中心点.
                case < 0:
                {
                    m_Ring = 0;
                    m_Current = m_Center;
                    return true;
                }
                // 中心点之后进入第一圈.
                case 0 when Radius == 0:
                {
                    Finish();
                    return false;
                }
                case 0:
                {
                    StartRing(1);
                    return true;
                }
            }

            // 沿当前边移动一个网格单位.
            m_Current += s_Directions[m_Side];

            m_Step++;

            if (m_Step != m_Ring) return true;
            // 当前边已经遍历完毕, 进入下一条边.
            m_Step = 0;
            m_Side++;

            // 六条边全部遍历完毕.
            if (m_Side != 6) return true;
            int nextRing = m_Ring + 1;

            if (nextRing > Radius)
            {
                Finish();
                return false;
            }

            StartRing(nextRing);

            return true;
        }

        /// <summary>
        /// 开始枚举指定环.
        /// </summary>
        private void StartRing(int ring)
        {
            m_Ring = ring;
            m_Side = 0;
            m_Step = 0;

            // 每个环从左下角 (-ring, ring) 开始.
            HexagonalCubePoint cornerDirection = s_Directions[4];

            m_Current = new HexagonalCubePoint(
                m_Center.Q + cornerDirection.Q * ring,
                m_Center.R + cornerDirection.R * ring
            );
        }

        private void Finish()
        {
            m_Ring = Radius + 1;
            m_Side = 0;
            m_Step = 0;
            m_Current = default;
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_Ring = -1;
            m_Side = 0;
            m_Step = 0;
            m_Current = default;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <summary>
        /// 获取无装箱的结构体枚举器.
        /// </summary>
        public RangeCollection GetEnumerator() => this;

        IEnumerator<HexagonalCubePoint> IEnumerable<HexagonalCubePoint>.GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => this;
    }
}
