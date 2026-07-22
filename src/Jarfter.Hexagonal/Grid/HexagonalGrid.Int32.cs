using Jarfter.Hexagonal.Direction;

namespace Jarfter.Hexagonal.Grid;

/// <summary>
/// 提供整数六边形坐标的专用操作.
/// </summary>
public static class HexagonalGridInt32Extensions
{
    /// <param name="cell">当前坐标.</param>
    extension(HexagonalGrid<int> cell)
    {
        /// <summary>
        /// 尝试获取当前坐标在指定方向上的相邻坐标.
        /// 该方法被隐藏, 因为首个参数容易被错误传递.
        /// </summary>
        /// <param name="direction">相邻方向的整形结果.</param>
        /// <param name="neighbor">指定方向上的相邻坐标.</param>
        /// <returns>当方向有效且结果没有超出 int 坐标范围时返回 true, 否则返回 false.</returns>
        internal bool TryGetNeighbor(int direction, out HexagonalGrid<int> neighbor)
        {
            return cell.TryGetNeighbor((HexagonalDirection)direction, 1, out neighbor);
        }

        /// <summary>
        /// 尝试获取当前坐标在指定方向上的相邻坐标.
        /// </summary>
        /// <param name="direction">相邻方向.</param>
        /// <param name="neighbor">指定方向上的相邻坐标.</param>
        /// <returns>当方向有效且结果没有超出 int 坐标范围时返回 true, 否则返回 false.</returns>
        public bool TryGetNeighbor(HexagonalDirection direction, out HexagonalGrid<int> neighbor)
        {
            return cell.TryGetNeighbor(direction, 1, out neighbor);
        }

        /// <summary>
        /// 尝试获取当前坐标在指定方向和距离上的坐标.
        /// </summary>
        /// <param name="direction">移动方向.</param>
        /// <param name="distance">移动距离.</param>
        /// <param name="neighbor">指定方向和距离上的坐标.</param>
        /// <returns>当方向有效且结果没有超出 int 坐标范围时返回 true, 否则返回 false.</returns>
        public bool TryGetNeighbor(HexagonalDirection direction, int distance, out HexagonalGrid<int> neighbor)
        {
            if (!TryGetDirectionOffset(direction, out int qOffset, out int rOffset))
            {
                neighbor = default;
                return false;
            }

            long q = cell.Q + (long)qOffset * distance;
            long r = cell.R + (long)rOffset * distance;
            if (q < int.MinValue || q > int.MaxValue || r < int.MinValue || r > int.MaxValue)
            {
                neighbor = default;
                return false;
            }

            neighbor = new HexagonalGrid<int>((int)q, (int)r);
            return true;
        }

        /// <summary>
        /// 尝试获取当前坐标到另一个坐标的六边形距离.
        /// </summary>
        /// <param name="other">另一个六边形坐标.</param>
        /// <param name="distance">两个坐标之间的最短步数.</param>
        /// <returns>当距离可由 int 表示时返回 true, 否则返回 false.</returns>
        public bool TryDistanceTo(HexagonalGrid<int> other, out int distance)
        {
            long q = (long)cell.Q - other.Q;
            long r = (long)cell.R - other.R;
            long distanceAsLong = (Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2;
            if (distanceAsLong > int.MaxValue)
            {
                distance = default;
                return false;
            }

            distance = (int)distanceAsLong;
            return true;
        }

        /// <summary>
        /// 尝试将当前坐标绕原点逆时针旋转 60 度.
        /// </summary>
        /// <param name="rotated">旋转后的坐标.</param>
        /// <returns>当旋转结果没有超出 int 坐标范围时返回 true, 否则返回 false.</returns>
        public bool TryRotateLeft(out HexagonalGrid<int> rotated)
        {
            long q = (long)cell.Q + cell.R;
            long r = -(long)cell.Q;
            if (q < int.MinValue || q > int.MaxValue || r < int.MinValue || r > int.MaxValue)
            {
                rotated = default;
                return false;
            }

            rotated = new HexagonalGrid<int>((int)q, (int)r);
            return true;
        }

        /// <summary>
        /// 尝试将当前坐标绕原点顺时针旋转 60 度.
        /// </summary>
        /// <param name="rotated">旋转后的坐标.</param>
        /// <returns>当旋转结果没有超出 int 坐标范围时返回 true, 否则返回 false.</returns>
        public bool TryRotateRight(out HexagonalGrid<int> rotated)
        {
            long r = (long)cell.Q + cell.R;
            long q = -(long)cell.R;
            if (q < int.MinValue || q > int.MaxValue || r < int.MinValue || r > int.MaxValue)
            {
                rotated = default;
                return false;
            }

            rotated = new HexagonalGrid<int>((int)q, (int)r);
            return true;
        }
    }

    private static bool TryGetDirectionOffset(
        HexagonalDirection direction,
        out int qOffset,
        out int rOffset)
    {
        switch (direction)
        {
            case HexagonalDirection.PositiveQ:
                qOffset = 1;
                rOffset = 0;
                return true;
            case HexagonalDirection.NegativeR:
                qOffset = 1;
                rOffset = -1;
                return true;
            case HexagonalDirection.PositiveS:
                qOffset = 0;
                rOffset = -1;
                return true;
            case HexagonalDirection.NegativeQ:
                qOffset = -1;
                rOffset = 0;
                return true;
            case HexagonalDirection.PositiveR:
                qOffset = -1;
                rOffset = 1;
                return true;
            case HexagonalDirection.NegativeS:
                qOffset = 0;
                rOffset = 1;
                return true;
            default:
                qOffset = 0;
                rOffset = 0;
                return false;
        }
    }
}
