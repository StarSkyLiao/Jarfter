namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 定义统计计算器接口.
/// </summary>
public interface IBenchmarkStatisticsCalculator
{
    /// <summary>
    /// 根据样本计算统计信息.
    /// </summary>
    /// <param name="samples">样本序列.</param>
    /// <returns>统计结果.</returns>
    public BenchmarkMetricStatistics Calculate(ReadOnlySpan<decimal> samples);
}
