namespace Jarfter.Hexagonal.Geometry;

/// <summary>
/// 表示与布局保持相同朝向的正六边形足迹.
/// 足迹尺寸以布局单位六边形 Apothem 的比例表示, 不允许退化为点.
/// </summary>
public readonly record struct HexagonalFootprint
{
    /// <summary>
    /// 使用指定的 Apothem 比例初始化六边形足迹.
    /// </summary>
    /// <param name="apothemScale">相对于布局单位六边形 Apothem 的正比例.</param>
    /// <exception cref="ArgumentOutOfRangeException">当比例不是有限正数时抛出.</exception>
    public HexagonalFootprint(double apothemScale)
    {
        if (!double.IsFinite(apothemScale) || apothemScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(apothemScale));
        }

        ApothemScale = apothemScale;
    }

    /// <summary>
    /// 获取相对于布局单位六边形 Apothem 的尺寸比例.
    /// </summary>
    public double ApothemScale { get; }

    /// <summary>
    /// 获取与单位六边形相同大小的足迹.
    /// </summary>
    public static HexagonalFootprint Unit => new HexagonalFootprint(1);
}
