namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 表示默认六边形寻路地图中的单元数据.
/// <see cref="TraversalMultiplier"/> 为进入该单元的移动代价倍率; <see cref="ObstacleApothemScale"/> 大于 0 时, 该单元表示不可通行的正六边形障碍物.
/// </summary>
/// <param name="TraversalMultiplier">进入可通行单元的移动代价倍率.</param>
/// <param name="ObstacleApothemScale">障碍物相对单元半径的内切圆半径缩放比例.</param>
public readonly record struct HexNavigationCell(double TraversalMultiplier = 1, double ObstacleApothemScale = 0)
{
    /// <summary>
    /// 创建移动代价倍率为 1 且不包含障碍物的默认单元.
    /// </summary>
    public HexNavigationCell() : this(1)
    {
    }
}
