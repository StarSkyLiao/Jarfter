namespace Jarfter.Hexagonal.Pathfinding.Navigation;

/// <summary>
/// 表示单个六边形格子的静态导航属性.
/// 地形倍率作用于穿过该格子的路径长度, 障碍尺寸以单位六边形 Apothem 的比例表示.
/// </summary>
public readonly record struct HexNavigationCell
{
    /// <summary>
    /// 使用指定的地形倍率和障碍 Apothem 比例初始化导航格子.
    /// </summary>
    /// <param name="traversalMultiplier">穿过格子的移动倍率, 必须为不小于 1 的有限数.</param>
    /// <param name="obstacleApothemScale">格心障碍的 Apothem 比例. 0 表示不存在障碍.</param>
    /// <exception cref="ArgumentOutOfRangeException">当任一参数不满足其取值范围时抛出.</exception>
    public HexNavigationCell(double traversalMultiplier = 1, double obstacleApothemScale = 0)
    {
        if (!double.IsFinite(traversalMultiplier) || traversalMultiplier < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(traversalMultiplier));
        }

        if (!double.IsFinite(obstacleApothemScale) || obstacleApothemScale < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(obstacleApothemScale));
        }

        TraversalMultiplier = traversalMultiplier;
        ObstacleApothemScale = obstacleApothemScale;
    }

    /// <summary>
    /// 获取穿过格子的移动倍率.
    /// <see langword="default"/> 值表示无障碍且移动倍率为 1 的普通格子, 可直接用作稠密地图的初始值.
    /// </summary>
    public double TraversalMultiplier => field == 0 ? 1 : field;

    /// <summary>
    /// 获取格心障碍相对于单位六边形 Apothem 的尺寸比例. 0 表示不存在障碍.
    /// </summary>
    public double ObstacleApothemScale { get; }

    /// <summary>
    /// 获取格子是否包含需要参与导航碰撞的障碍.
    /// </summary>
    public bool HasObstacle => ObstacleApothemScale > 0;

    /// <summary>
    /// 获取无障碍且移动倍率为 1 的普通格子.
    /// </summary>
    public static HexNavigationCell Default => new HexNavigationCell();
}
