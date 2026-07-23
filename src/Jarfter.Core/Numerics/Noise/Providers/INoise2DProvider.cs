using Jarfter.Core.Numerics.Noise.Calculators;

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 定义一种能够提供二维噪声能力的接口.
/// </summary>
public interface INoise2DProvider
{
    /// <summary>
    /// 噪声图的种子
    /// </summary>
    int NoiseSeed { get; }

    /// <summary>
    /// 目标点没有被缓存时, 使用的噪声计算方式
    /// </summary>
    INoiseCalculator Calculator { get; }

    /// <summary>
    /// 返回 position 坐标位置的采样噪声值 (0 ~ 1).
    /// </summary>
    /// <param name="localPosition">要采样的二维整数坐标.</param>
    /// <returns>指定坐标的噪声值.</returns>
    double ValueAt((int x, int y) localPosition);

    /// <summary>
    /// 返回指定单精度浮点的噪声值(0 ~ 1).
    /// 该点的值根据包围该点的四个整点进行加权计算得出。
    /// </summary>
    /// <param name="position">要采样的二维单精度浮点坐标.</param>
    /// <returns>指定坐标的插值噪声值.</returns>
    double ValueAt((float x, float y) position) => ValueAt(((double)position.x, position.y));

    /// <summary>
    /// 返回指定双精度浮点的噪声值(0 ~ 1).
    /// 该点的值根据包围该点的四个整点进行加权计算得出。
    /// </summary>
    /// <param name="position">要采样的二维双精度浮点坐标.</param>
    /// <returns>指定坐标的插值噪声值.</returns>
    double ValueAt((double x, double y) position)
    {
        // 计算向下取整的点
        int floorX = (int)Math.Floor(position.x);
        int floorY = (int)Math.Floor(position.y);
        // 计算权重
        double weightX = position.x - floorX;
        double weightY = position.y - floorY;
        // 包裹矩形下边对应点
        double bottomCenter = Lerp(
            ValueAt((floorX, floorY)),
            ValueAt((floorX + 1, floorY)),
            weightX
        );
        // 包裹矩形上边对应点
        double topCenter = Lerp(
            ValueAt((floorX, floorY + 1)),
            ValueAt((floorX + 1, floorY + 1)),
            weightX
        );
        // 计算目标点结果并返回
        return Lerp(bottomCenter, topCenter, weightY);
        static double Lerp(double a, double b, double t) => a + (b - a) * (t < 0 ? 0 : t > 1 ? 1 : t);
    }
}
