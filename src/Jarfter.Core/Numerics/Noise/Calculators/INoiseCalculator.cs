namespace Jarfter.Core.Numerics.Noise.Calculators;

/// <summary>
/// 定义根据种子和二维坐标计算噪声值的能力.
/// </summary>
public interface INoiseCalculator
{
    /// <summary>
    /// 根据种子和点坐标计算噪声值
    /// </summary>
    /// <param name="localSeed">用于计算当前噪声值的种子.</param>
    /// <param name="point">要计算的二维整数坐标.</param>
    /// <returns>计算得到的噪声值.</returns>
    double Calculate(int localSeed, (int x, int y) point);
}
