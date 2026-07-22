using Jarfter.Hexagonal.Direction;

namespace Jarfter.Hexagonal.Grid;

public partial record struct HexagonalGrid<T>
{
    /// <summary>
    /// 枚举以当前坐标为中心的指定半径环.
    /// </summary>
    /// <param name="radius">环半径. 0 表示仅返回当前坐标.</param>
    /// <returns>指定半径环上的坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    public IEnumerable<HexagonalGrid<T>> Ring(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        if (radius == 0)
        {
            yield return this;
            yield break;
        }

        HexagonalGrid<T> current = Neighbor(HexagonalDirection.PositiveR, radius);
        for (int directionIndex = 0; directionIndex < 6; directionIndex++)
        {
            HexagonalGrid<T> directionOffset = Direction((HexagonalDirection)directionIndex);
            for (int step = 0; step < radius; step++)
            {
                yield return current;
                current += directionOffset;
            }
        }
    }

    /// <summary>
    /// 枚举以当前坐标为中心, 从内到外直到指定半径的螺旋区域.
    /// </summary>
    /// <param name="radius">最大半径. 0 表示仅返回当前坐标.</param>
    /// <returns>从当前坐标开始, 按半径递增排列的坐标.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    public IEnumerable<HexagonalGrid<T>> Spiral(int radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(radius);

        yield return this;

        for (int currentRadius = 1; currentRadius <= radius; currentRadius++)
        {
            HexagonalGrid<T> current = Neighbor(HexagonalDirection.PositiveR, currentRadius);
            for (int directionIndex = 0; directionIndex < 6; directionIndex++)
            {
                HexagonalGrid<T> directionOffset = Direction((HexagonalDirection)directionIndex);
                for (int step = 0; step < currentRadius; step++)
                {
                    yield return current;
                    current += directionOffset;
                }
            }
        }
    }

    /// <summary>
    /// 将以当前坐标为中心的指定半径环复制到目标缓冲区.
    /// </summary>
    /// <param name="radius">环半径. 0 表示仅写入当前坐标.</param>
    /// <param name="destination">用于接收坐标的目标缓冲区.</param>
    /// <returns>写入目标缓冲区的坐标数量.</returns>
    /// <exception cref="ArgumentException">当 destination 长度不足以容纳指定半径环时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    public int CopyRingTo(int radius, Span<HexagonalGrid<T>> destination)
    {
        int count = HexagonalGridMetrics.CountInRing(radius);
        if (destination.Length < count)
        {
            throw new ArgumentException("Destination is too small for the specified ring.", nameof(destination));
        }

        if (radius == 0)
        {
            destination[0] = this;
            return count;
        }

        int index = 0;
        HexagonalGrid<T> current = Neighbor(HexagonalDirection.PositiveR, radius);
        for (int directionIndex = 0; directionIndex < 6; directionIndex++)
        {
            HexagonalGrid<T> directionOffset = Direction((HexagonalDirection)directionIndex);
            for (int step = 0; step < radius; step++)
            {
                destination[index] = current;
                index++;
                current += directionOffset;
            }
        }

        return count;
    }

    /// <summary>
    /// 将以当前坐标为中心, 半径不超过指定值的区域复制到目标缓冲区.
    /// </summary>
    /// <param name="radius">最大半径. 0 表示仅写入当前坐标.</param>
    /// <param name="destination">用于接收坐标的目标缓冲区.</param>
    /// <returns>写入目标缓冲区的坐标数量.</returns>
    /// <exception cref="ArgumentException">当 destination 长度不足以容纳指定范围时抛出.</exception>
    /// <exception cref="ArgumentOutOfRangeException">当 radius 小于 0 时抛出.</exception>
    public int CopyRangeTo(int radius, Span<HexagonalGrid<T>> destination)
    {
        int count = HexagonalGridMetrics.CountInRange(radius);
        if (destination.Length < count)
        {
            throw new ArgumentException("Destination is too small for the specified range.", nameof(destination));
        }

        int index = 0;
        destination[index] = this;
        index++;

        for (int currentRadius = 1; currentRadius <= radius; currentRadius++)
        {
            HexagonalGrid<T> current = Neighbor(HexagonalDirection.PositiveR, currentRadius);
            for (int directionIndex = 0; directionIndex < 6; directionIndex++)
            {
                HexagonalGrid<T> directionOffset = Direction((HexagonalDirection)directionIndex);
                for (int step = 0; step < currentRadius; step++)
                {
                    destination[index] = current;
                    index++;
                    current += directionOffset;
                }
            }
        }

        return count;
    }
}
