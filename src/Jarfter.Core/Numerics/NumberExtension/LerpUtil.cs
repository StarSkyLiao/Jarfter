namespace Jarfter.Core.Numerics;

/// <summary>
/// 提供单精度和双精度浮点数的插值方法.
/// <para>当 <c>progress</c> 位于 [0, 1] 时, 结果位于起止值之间; 超出该范围时会执行外推.</para>
/// </summary>
public static class LerpUtil
{
    /// <summary>
    /// 返回两个单精度浮点数之间的线性插值结果.
    /// </summary>
    /// <param name="start">插值的起始值.</param>
    /// <param name="end">插值的结束值.</param>
    /// <param name="progress">插值进度.</param>
    /// <returns>根据 <paramref name="progress"/> 计算得到的线性插值结果.</returns>
    public static float Linear(float start, float end, float progress)
        => start + (end - start) * progress;

    /// <summary>
    /// 返回两个单精度浮点数之间经三次多项式平滑后的插值结果.
    /// </summary>
    /// <param name="start">插值的起始值.</param>
    /// <param name="end">插值的结束值.</param>
    /// <param name="progress">插值进度.</param>
    /// <returns>根据 <paramref name="progress"/> 计算得到的平滑插值结果.</returns>
    public static float Smooth(float start, float end, float progress)
        => start + (end - start) * (progress * progress * (3 - 2 * progress));

    /// <summary>
    /// 返回两个单精度浮点数之间经五次多项式平滑后的插值结果.
    /// </summary>
    /// <param name="start">插值的起始值.</param>
    /// <param name="end">插值的结束值.</param>
    /// <param name="progress">插值进度.</param>
    /// <returns>根据 <paramref name="progress"/> 计算得到的平滑插值结果.</returns>
    public static float Quintic(float start, float end, float progress)
        => start + (end - start) * (progress * progress * progress *
            (6 * progress * progress - 15 * progress + 10));

    /// <summary>
    /// 返回两个双精度浮点数之间的线性插值结果.
    /// </summary>
    /// <param name="start">插值的起始值.</param>
    /// <param name="end">插值的结束值.</param>
    /// <param name="progress">插值进度.</param>
    /// <returns>根据 <paramref name="progress"/> 计算得到的线性插值结果.</returns>
    public static double Linear(double start, double end, double progress)
        => start + (end - start) * progress;

    /// <summary>
    /// 返回两个双精度浮点数之间经三次多项式平滑后的插值结果.
    /// </summary>
    /// <param name="start">插值的起始值.</param>
    /// <param name="end">插值的结束值.</param>
    /// <param name="progress">插值进度.</param>
    /// <returns>根据 <paramref name="progress"/> 计算得到的平滑插值结果.</returns>
    public static double Smooth(double start, double end, double progress)
        => start + (end - start) * (progress * progress * (3 - 2 * progress));

    /// <summary>
    /// 返回两个双精度浮点数之间经五次多项式平滑后的插值结果.
    /// </summary>
    /// <param name="start">插值的起始值.</param>
    /// <param name="end">插值的结束值.</param>
    /// <param name="progress">插值进度.</param>
    /// <returns>根据 <paramref name="progress"/> 计算得到的平滑插值结果.</returns>
    public static double Quintic(double start, double end, double progress)
        => start + (end - start) * (progress * progress * progress *
            (6 * progress * progress - 15 * progress + 10));
}
