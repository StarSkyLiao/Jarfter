using System.Collections;
using System.Numerics;
using Jarfter.Hexagonal.Direction;
using Jarfter.Hexagonal.Grid;

namespace Jarfter.Hexagonal.Layout;

/// <summary>
/// 表示六边形坐标与二维空间之间的布局转换.
/// </summary>
public readonly record struct HexagonalLayout
{
    private const double Sqrt3 = 1.7320508075688772;
    private const double Sqrt3Over2 = Sqrt3 / 2;
    private const int CornerCount = 6;

    /// <summary>
    /// 创建六边形布局.
    /// </summary>
    /// <param name="orientation">六边形显示朝向.</param>
    /// <param name="size">六边形半径尺寸. X 和 Y 可不同, 用于非等比缩放.</param>
    /// <param name="origin">布局原点.</param>
    /// <exception cref="ArgumentOutOfRangeException">当 orientation 无效, 或 size 的任一分量不为有限正数时抛出.</exception>
    public HexagonalLayout(HexagonalOrientation orientation, HexagonalPoint size, HexagonalPoint origin)
    {
        if (!Enum.IsDefined(orientation))
        {
            throw new ArgumentOutOfRangeException(nameof(orientation), orientation, "Unknown hex orientation.");
        }

        if (!double.IsFinite(size.X) || !double.IsFinite(size.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Hex size components must be finite.");
        }

        if (size.X <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Hex size X must be greater than 0.");
        }

        if (size.Y <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Hex size Y must be greater than 0.");
        }

        if (!double.IsFinite(origin.X) || !double.IsFinite(origin.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(origin), origin, "Layout origin components must be finite.");
        }

        Orientation = orientation;
        Size = size;
        Origin = origin;
    }

    /// <summary>
    /// 获取六边形显示朝向.
    /// </summary>
    public HexagonalOrientation Orientation { get; }

    /// <summary>
    /// 获取六边形半径尺寸.
    /// </summary>
    public HexagonalPoint Size { get; }

    /// <summary>
    /// 获取布局原点.
    /// </summary>
    public HexagonalPoint Origin { get; }

    /// <summary>
    /// 创建尖顶朝上的等比布局.
    /// </summary>
    /// <param name="radius">六边形半径.</param>
    /// <param name="origin">布局原点.</param>
    /// <returns>尖顶朝上的布局.</returns>
    public static HexagonalLayout CreatePointyTop(double radius, HexagonalPoint origin)
    {
        return new HexagonalLayout(HexagonalOrientation.PointyTop, new HexagonalPoint(radius, radius), origin);
    }

    /// <summary>
    /// 创建平边朝上的等比布局.
    /// </summary>
    /// <param name="radius">六边形半径.</param>
    /// <param name="origin">布局原点.</param>
    /// <returns>平边朝上的布局.</returns>
    public static HexagonalLayout CreateFlatTop(double radius, HexagonalPoint origin)
    {
        return new HexagonalLayout(HexagonalOrientation.FlatTop, new HexagonalPoint(radius, radius), origin);
    }

    /// <summary>
    /// 将六边形坐标转换为二维中心点.
    /// </summary>
    /// <typeparam name="TCoordinate">坐标分量的数值类型.</typeparam>
    /// <param name="cell">六边形坐标.</param>
    /// <returns>对应的二维中心点.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 Orientation 不是有效值时抛出.</exception>
    public HexagonalPoint ToPoint<TCoordinate>(HexagonalGrid<TCoordinate> cell)
        where TCoordinate : INumber<TCoordinate>
    {
        double q = double.CreateChecked(cell.Q);
        double r = double.CreateChecked(cell.R);

        return Orientation switch
        {
            HexagonalOrientation.PointyTop => new HexagonalPoint(
                Origin.X + Size.X * (Sqrt3 * q + Sqrt3Over2 * r),
                Origin.Y + Size.Y * (1.5 * r)),
            HexagonalOrientation.FlatTop => new HexagonalPoint(
                Origin.X + Size.X * (1.5 * q),
                Origin.Y + Size.Y * (Sqrt3Over2 * q + Sqrt3 * r)),
            _ => throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown hex orientation.")
        };
    }

    /// <summary>
    /// 将二维点转换为分数六边形坐标.
    /// </summary>
    /// <param name="point">二维点.</param>
    /// <returns>对应的分数六边形坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 Orientation 不是有效值时抛出.</exception>
    public HexagonalGrid<double> ToFractionalGrid(HexagonalPoint point)
    {
        double x = (point.X - Origin.X) / Size.X;
        double y = (point.Y - Origin.Y) / Size.Y;

        return Orientation switch
        {
            HexagonalOrientation.PointyTop => new HexagonalGrid<double>(
                Sqrt3 / 3 * x - y / 3,
                2.0 / 3 * y),
            HexagonalOrientation.FlatTop => new HexagonalGrid<double>(
                2.0 / 3 * x,
                -x / 3 + Sqrt3 / 3 * y),
            _ => throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown hex orientation.")
        };
    }

    /// <summary>
    /// 将二维点转换为最近的整数六边形坐标.
    /// </summary>
    /// <param name="point">二维点.</param>
    /// <returns>距离二维点最近的整数六边形坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 point 的任一分量不是有限数时抛出.</exception>
    /// <exception cref="OverflowException">当最近的整数六边形坐标超出 int 范围时抛出.</exception>
    public HexagonalGrid<int> ToRoundedGrid(HexagonalPoint point)
    {
        if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(point), point, "Point components must be finite.");
        }

        return RoundToInt32(ToFractionalGrid(point));
    }

    /// <summary>
    /// 获取指定顶点相对六边形中心点的偏移.
    /// </summary>
    /// <param name="cornerIndex">顶点索引, 有效范围为 0 到 5.</param>
    /// <returns>指定顶点相对中心点的偏移.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 cornerIndex 不在 0 到 5 范围内时抛出.</exception>
    public HexagonalPoint GetCornerOffset(int cornerIndex)
    {
        if ((uint)cornerIndex >= CornerCount)
        {
            throw new ArgumentOutOfRangeException(nameof(cornerIndex), cornerIndex, "Corner index must be between 0 and 5.");
        }

        HexagonalPoint unitOffset = Orientation switch
        {
            HexagonalOrientation.PointyTop => GetPointyTopUnitCornerOffset(cornerIndex),
            HexagonalOrientation.FlatTop => GetFlatTopUnitCornerOffset(cornerIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(Orientation), Orientation, "Unknown hex orientation.")
        };
        return new HexagonalPoint(Size.X * unitOffset.X, Size.Y * unitOffset.Y);
    }

    /// <summary>
    /// 将指定中心点对应的六个顶点复制到目标缓冲区.
    /// </summary>
    /// <param name="center">六边形中心点.</param>
    /// <param name="destination">用于接收六个顶点的目标缓冲区.</param>
    /// <exception cref="ArgumentException">当 destination 的长度小于 6 时抛出.</exception>
    public void CopyCornersTo(HexagonalPoint center, Span<HexagonalPoint> destination)
    {
        if (destination.Length < CornerCount)
        {
            throw new ArgumentException("Destination must contain at least 6 elements.", nameof(destination));
        }

        for (int index = 0; index < CornerCount; index++)
        {
            destination[index] = center + GetCornerOffset(index);
        }
    }

    /// <summary>
    /// 将指定六边形坐标对应的六个顶点复制到目标缓冲区.
    /// </summary>
    /// <typeparam name="TCoordinate">坐标分量的数值类型.</typeparam>
    /// <param name="cell">六边形坐标.</param>
    /// <param name="destination">用于接收六个顶点的目标缓冲区.</param>
    /// <exception cref="ArgumentException">当 destination 的长度小于 6 时抛出.</exception>
    public void CopyCornersTo<TCoordinate>(
        HexagonalGrid<TCoordinate> cell,
        Span<HexagonalPoint> destination)
        where TCoordinate : INumber<TCoordinate>
    {
        CopyCornersTo(ToPoint(cell), destination);
    }

    /// <summary>
    /// 枚举指定六边形坐标的六个顶点.
    /// </summary>
    /// <typeparam name="TCoordinate">坐标分量的数值类型.</typeparam>
    /// <param name="cell">六边形坐标.</param>
    /// <returns>指定六边形坐标的六个顶点.</returns>
    public CornerEnumerable<TCoordinate> Corners<TCoordinate>(HexagonalGrid<TCoordinate> cell)
        where TCoordinate : INumber<TCoordinate>
    {
        return new CornerEnumerable<TCoordinate>(this, cell);
    }

    private static HexagonalGrid<int> RoundToInt32(HexagonalGrid<double> cell)
    {
        double q = cell.Q;
        double r = cell.R;
        double s = cell.S;

        int roundedQ = checked((int)Math.Round(q, MidpointRounding.AwayFromZero));
        int roundedR = checked((int)Math.Round(r, MidpointRounding.AwayFromZero));
        int roundedS = checked((int)Math.Round(s, MidpointRounding.AwayFromZero));

        double qDifference = Math.Abs(roundedQ - q);
        double rDifference = Math.Abs(roundedR - r);
        double sDifference = Math.Abs(roundedS - s);

        if (qDifference > rDifference && qDifference > sDifference)
        {
            roundedQ = checked(-roundedR - roundedS);
        }
        else if (rDifference > sDifference)
        {
            roundedR = checked(-roundedQ - roundedS);
        }

        return new HexagonalGrid<int>(roundedQ, roundedR);
    }

    private static HexagonalPoint GetPointyTopUnitCornerOffset(int cornerIndex)
    {
        return cornerIndex switch
        {
            0 => new HexagonalPoint(Sqrt3Over2, 0.5),
            1 => new HexagonalPoint(0, 1),
            2 => new HexagonalPoint(-Sqrt3Over2, 0.5),
            3 => new HexagonalPoint(-Sqrt3Over2, -0.5),
            4 => new HexagonalPoint(0, -1),
            5 => new HexagonalPoint(Sqrt3Over2, -0.5),
            _ => throw new ArgumentOutOfRangeException(nameof(cornerIndex), cornerIndex, "Corner index must be between 0 and 5.")
        };
    }

    private static HexagonalPoint GetFlatTopUnitCornerOffset(int cornerIndex)
    {
        return cornerIndex switch
        {
            0 => new HexagonalPoint(1, 0),
            1 => new HexagonalPoint(0.5, Sqrt3Over2),
            2 => new HexagonalPoint(-0.5, Sqrt3Over2),
            3 => new HexagonalPoint(-1, 0),
            4 => new HexagonalPoint(-0.5, -Sqrt3Over2),
            5 => new HexagonalPoint(0.5, -Sqrt3Over2),
            _ => throw new ArgumentOutOfRangeException(nameof(cornerIndex), cornerIndex, "Corner index must be between 0 and 5.")
        };
    }

    /// <summary>
    /// 表示指定六边形坐标六个顶点的无分配可枚举集合.
    /// 直接使用 <c>foreach</c> 遍历时不会创建枚举器对象.
    /// </summary>
    /// <typeparam name="TCoordinate">六边形坐标分量的数值类型.</typeparam>
    public readonly struct CornerEnumerable<TCoordinate> : IEnumerable<HexagonalPoint>
        where TCoordinate : INumber<TCoordinate>
    {
        private readonly HexagonalLayout m_Layout;
        private readonly HexagonalGrid<TCoordinate> m_Cell;

        /// <summary>
        /// 使用指定布局和坐标创建顶点集合.
        /// </summary>
        /// <param name="layout">六边形布局.</param>
        /// <param name="cell">六边形坐标.</param>
        internal CornerEnumerable(HexagonalLayout layout, HexagonalGrid<TCoordinate> cell)
        {
            m_Layout = layout;
            m_Cell = cell;
        }

        /// <summary>
        /// 返回用于无分配遍历六个顶点的枚举器.
        /// </summary>
        /// <returns>六边形顶点枚举器.</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(m_Layout, m_Cell);
        }

        IEnumerator<HexagonalPoint> IEnumerable<HexagonalPoint>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// 按顶点索引顺序遍历六边形顶点的枚举器.
        /// </summary>
        public struct Enumerator : IEnumerator<HexagonalPoint>
        {
            private readonly HexagonalLayout m_Layout;
            private readonly HexagonalGrid<TCoordinate> m_Cell;
            private HexagonalPoint m_Center;
            private int m_Index;

            /// <summary>
            /// 使用指定布局和坐标创建枚举器.
            /// </summary>
            /// <param name="layout">六边形布局.</param>
            /// <param name="cell">六边形坐标.</param>
            internal Enumerator(HexagonalLayout layout, HexagonalGrid<TCoordinate> cell)
            {
                m_Layout = layout;
                m_Cell = cell;
                m_Center = default;
                m_Index = -1;
            }

            /// <inheritdoc />
            public HexagonalPoint Current
            {
                get
                {
                    if ((uint)m_Index >= CornerCount)
                    {
                        throw new InvalidOperationException("Enumerator is not positioned on a corner.");
                    }

                    return m_Center + m_Layout.GetCornerOffset(m_Index);
                }
            }

            object IEnumerator.Current => Current;

            /// <inheritdoc />
            public bool MoveNext()
            {
                int index = m_Index + 1;
                if ((uint)index >= CornerCount)
                {
                    m_Index = CornerCount;
                    return false;
                }

                if (index == 0)
                {
                    m_Center = m_Layout.ToPoint(m_Cell);
                }

                m_Index = index;
                return true;
            }

            /// <inheritdoc />
            public void Reset()
            {
                m_Center = default;
                m_Index = -1;
            }

            /// <inheritdoc />
            public void Dispose()
            {
            }
        }
    }
}
