using Jarfter.HexCube.Numerics;

namespace Jarfter.HexCube.Pathfinding.Continuous;

/// <summary>
/// 定义连续 NavMesh 的有限六边形工作边界.
/// 边界用于限制网格构建范围, 不会改变 <see cref="IContinuousNavigationSnapshot"/> 对障碍物和高代价区域的原始定义.
/// </summary>
public sealed class ContinuousNavigationBounds
{
    /// <summary>
    /// 使用指定的连续六边形创建导航边界.
    /// </summary>
    /// <param name="shape">完全包围可导航区域的六边形.</param>
    /// <exception cref="ArgumentOutOfRangeException">当边界坐标或半径不合法, 或半径不为正数时抛出.</exception>
    public ContinuousNavigationBounds(HexCubeArea2D shape)
    {
        HexCubePoint position = shape.Position;

        if (!double.IsFinite(position.Q) || !double.IsFinite(position.R) ||
            !(shape.RadiusScale > 0) || !double.IsFinite(shape.RadiusScale))
        {
            throw new ArgumentOutOfRangeException(nameof(shape), shape, "Navigation boundary position must be finite, and radius must be finite and positive.");
        }

        Shape = shape;
    }

    /// <summary>
    /// 获取完全包围 NavMesh 的六边形边界.
    /// </summary>
    public HexCubeArea2D Shape { get; }

    /// <summary>
    /// 判断指定位置是否位于导航边界内或边界上.
    /// </summary>
    /// <param name="position">要判断的连续位置.</param>
    /// <returns>位置位于边界内或边界上时返回 true, 否则返回 false.</returns>
    public bool Contains(HexCubePoint position) => Shape.Contains(position);

    /// <summary>
    /// 判断指定线段是否完全位于导航边界内或边界上.
    /// 六边形边界为凸集, 因此两个端点均在边界内时整条线段也在边界内.
    /// </summary>
    /// <param name="line">要判断的连续线段.</param>
    /// <returns>线段完全位于边界内或边界上时返回 true, 否则返回 false.</returns>
    public bool Contains(HexCubeLine2D line) => Contains(line.Start) && Contains(line.End);
}
