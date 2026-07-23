using Point = (int x, int y);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 表示在任意坐标均返回同一值的二维噪声提供器.
/// 常量值必须位于 [0, 1] 区间, 可用于测试、调试和滤镜组合的基准输入.
/// </summary>
/// <param name="value">要返回的常量噪声值.</param>
public sealed class NoiseConstant2D(double value) : INoise2DProvider
{
    /// <summary>
    /// 获取常量噪声值.
    /// </summary>
    public double Value { get; } = ValidateValue(value);

    /// <inheritdoc />
    public int NoiseSeed => 0;

    /// <inheritdoc />
    public double ValueAt(Point localPosition) => Value;

    private static double ValidateValue(double value)
    {
        if (!double.IsFinite(value) || value is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(value), "常量噪声值必须位于 [0, 1] 区间.");

        return value;
    }
}
