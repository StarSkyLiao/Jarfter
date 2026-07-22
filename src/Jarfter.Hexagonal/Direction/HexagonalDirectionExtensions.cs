namespace Jarfter.Hexagonal.Direction;

/// <summary>
/// 提供六边形方向的基础拓扑操作.
/// </summary>
public static class HexagonalDirectionExtensions
{
    private const int DirectionCount = 6;

    /// <summary>
    /// 获取所有方向, 顺序为围绕原点逆时针排列.
    /// </summary>
    public static ReadOnlySpan<HexagonalDirection> All =>
    [
        HexagonalDirection.PositiveQ,
        HexagonalDirection.NegativeR,
        HexagonalDirection.PositiveS,
        HexagonalDirection.NegativeQ,
        HexagonalDirection.PositiveR,
        HexagonalDirection.NegativeS
    ];

    /// <param name="direction">罗盘方向.</param>
    extension(HexagonalCompassDirection direction)
    {
        /// <summary>
        /// 根据显示朝向获取罗盘方向对应的拓扑方向.
        /// </summary>
        /// <param name="orientation">六边形显示朝向.</param>
        /// <returns>指定罗盘方向在当前显示朝向下对应的拓扑方向.</returns>
        /// <exception cref="ArgumentException">当罗盘方向无法落在指定显示朝向的六个边方向上时抛出.</exception>
        /// <exception cref="ArgumentOutOfRangeException">当 direction 或 orientation 不是有效值时抛出.</exception>
        public HexagonalDirection ToHexagonalDirection(HexagonalOrientation orientation)
        {
            if (direction.TryToHexagonalDirection(orientation, out HexagonalDirection hexagonalDirection))
            {
                return hexagonalDirection;
            }

            throw new ArgumentException(
                "The compass direction is not available for the specified hex orientation.",
                nameof(direction));
        }

        /// <summary>
        /// 尝试根据显示朝向获取罗盘方向对应的拓扑方向.
        /// </summary>
        /// <param name="orientation">六边形显示朝向.</param>
        /// <param name="hexagonalDirection">转换得到的拓扑方向.</param>
        /// <returns>当罗盘方向能落在指定显示朝向的六个边方向上时返回 true, 否则返回 false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 direction 或 orientation 不是有效值时抛出.</exception>
        public bool TryToHexagonalDirection(HexagonalOrientation orientation,
            out HexagonalDirection hexagonalDirection)
        {
            switch (orientation)
            {
                case HexagonalOrientation.PointyTop:
                    return TryGetPointyTopDirection(direction, out hexagonalDirection);
                case HexagonalOrientation.FlatTop:
                    return TryGetFlatTopDirection(direction, out hexagonalDirection);
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), orientation, "Unknown hex orientation.");
            }
        }
    }

    /// <param name="direction">当前方向.</param>
    extension(HexagonalDirection direction)
    {
        /// <summary>
        /// 获取与当前方向相反的方向.
        /// </summary>
        /// <returns>与当前方向相差 180 度的方向.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 direction 不是有效方向时抛出.</exception>
        public HexagonalDirection Opposite()
        {
            int index = (int)direction;
            if ((uint)index < DirectionCount)
            {
                return (HexagonalDirection)((index + 3) % DirectionCount);
            }

            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.");
        }

        /// <summary>
        /// 获取当前方向逆时针旋转 60 度后的方向.
        /// </summary>
        /// <returns>逆时针旋转后的方向.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 direction 不是有效方向时抛出.</exception>
        public HexagonalDirection RotateLeft()
        {
            int index = (int)direction;
            if ((uint)index < DirectionCount)
            {
                return (HexagonalDirection)((index + 1) % DirectionCount);
            }

            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.");
        }

        /// <summary>
        /// 获取当前方向顺时针旋转 60 度后的方向.
        /// </summary>
        /// <returns>顺时针旋转后的方向.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 direction 不是有效方向时抛出.</exception>
        public HexagonalDirection RotateRight()
        {
            int index = (int)direction;
            if ((uint)index < DirectionCount)
            {
                return (HexagonalDirection)((index + DirectionCount - 1) % DirectionCount);
            }

            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.");
        }

        /// <summary>
        /// 根据显示朝向获取当前拓扑方向对应的罗盘方向.
        /// </summary>
        /// <param name="orientation">六边形显示朝向.</param>
        /// <returns>当前拓扑方向在指定显示朝向下对应的罗盘方向.</returns>
        /// <exception cref="ArgumentOutOfRangeException">当 direction 或 orientation 不是有效值时抛出.</exception>
        public HexagonalCompassDirection ToCompassDirection(HexagonalOrientation orientation)
        {
            return orientation switch
            {
                HexagonalOrientation.PointyTop => direction switch
                {
                    HexagonalDirection.PositiveQ => HexagonalCompassDirection.East,
                    HexagonalDirection.NegativeR => HexagonalCompassDirection.NorthEast,
                    HexagonalDirection.PositiveS => HexagonalCompassDirection.NorthWest,
                    HexagonalDirection.NegativeQ => HexagonalCompassDirection.West,
                    HexagonalDirection.PositiveR => HexagonalCompassDirection.SouthWest,
                    HexagonalDirection.NegativeS => HexagonalCompassDirection.SouthEast,
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.")
                },
                HexagonalOrientation.FlatTop => direction switch
                {
                    HexagonalDirection.PositiveQ => HexagonalCompassDirection.SouthEast,
                    HexagonalDirection.NegativeR => HexagonalCompassDirection.NorthEast,
                    HexagonalDirection.PositiveS => HexagonalCompassDirection.North,
                    HexagonalDirection.NegativeQ => HexagonalCompassDirection.NorthWest,
                    HexagonalDirection.PositiveR => HexagonalCompassDirection.SouthWest,
                    HexagonalDirection.NegativeS => HexagonalCompassDirection.South,
                    _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown hex direction.")
                },
                _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, "Unknown hex orientation.")
            };
        }
    }

    private static bool TryGetPointyTopDirection(
        HexagonalCompassDirection direction,
        out HexagonalDirection hexagonalDirection)
    {
        switch (direction)
        {
            case HexagonalCompassDirection.East:
                hexagonalDirection = HexagonalDirection.PositiveQ;
                return true;
            case HexagonalCompassDirection.NorthEast:
                hexagonalDirection = HexagonalDirection.NegativeR;
                return true;
            case HexagonalCompassDirection.NorthWest:
                hexagonalDirection = HexagonalDirection.PositiveS;
                return true;
            case HexagonalCompassDirection.West:
                hexagonalDirection = HexagonalDirection.NegativeQ;
                return true;
            case HexagonalCompassDirection.SouthWest:
                hexagonalDirection = HexagonalDirection.PositiveR;
                return true;
            case HexagonalCompassDirection.SouthEast:
                hexagonalDirection = HexagonalDirection.NegativeS;
                return true;
            case HexagonalCompassDirection.North:
            case HexagonalCompassDirection.South:
                hexagonalDirection = default;
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown compass direction.");
        }
    }

    private static bool TryGetFlatTopDirection(
        HexagonalCompassDirection direction,
        out HexagonalDirection hexagonalDirection)
    {
        switch (direction)
        {
            case HexagonalCompassDirection.SouthEast:
                hexagonalDirection = HexagonalDirection.PositiveQ;
                return true;
            case HexagonalCompassDirection.NorthEast:
                hexagonalDirection = HexagonalDirection.NegativeR;
                return true;
            case HexagonalCompassDirection.North:
                hexagonalDirection = HexagonalDirection.PositiveS;
                return true;
            case HexagonalCompassDirection.NorthWest:
                hexagonalDirection = HexagonalDirection.NegativeQ;
                return true;
            case HexagonalCompassDirection.SouthWest:
                hexagonalDirection = HexagonalDirection.PositiveR;
                return true;
            case HexagonalCompassDirection.South:
                hexagonalDirection = HexagonalDirection.NegativeS;
                return true;
            case HexagonalCompassDirection.East:
            case HexagonalCompassDirection.West:
                hexagonalDirection = default;
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, "Unknown compass direction.");
        }
    }
}
