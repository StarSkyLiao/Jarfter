namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 默认统计计算器实现.
/// </summary>
public sealed class DefaultBenchmarkStatisticsCalculator : IBenchmarkStatisticsCalculator
{
    private DefaultBenchmarkStatisticsCalculator() { }

    /// <summary>
    /// 默认单例实例.
    /// </summary>
    public static readonly DefaultBenchmarkStatisticsCalculator Instance = new DefaultBenchmarkStatisticsCalculator();

    /// <summary>
    /// 根据样本计算统计信息.
    /// </summary>
    /// <param name="samples">样本序列.</param>
    /// <returns>统计结果.</returns>
    public BenchmarkMetricStatistics Calculate(ReadOnlySpan<decimal> samples)
    {
        if (samples.IsEmpty) return default;

        decimal sum = 0;
        decimal mean = 0;
        decimal m2 = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            decimal x = samples[i];
            sum += x;

            decimal delta = x - mean;
            mean += delta / (i + 1);
            decimal delta2 = x - mean;
            m2 += delta * delta2;
        }

        decimal stdDev = (decimal)Math.Sqrt((double)(m2 / samples.Length));
        decimal rsd = mean == 0 ? 0 : Math.Abs(stdDev / mean);
        return new BenchmarkMetricStatistics(sum, mean, stdDev, rsd);
    }
}
