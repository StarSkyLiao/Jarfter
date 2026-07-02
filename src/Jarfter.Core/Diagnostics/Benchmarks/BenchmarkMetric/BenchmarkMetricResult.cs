namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示单个指标的采样与统计结果.
/// </summary>
/// <param name="Descriptor">指标描述.</param>
/// <param name="Samples">采样结果.</param>
/// <param name="Statistics">统计数据.</param>
public readonly record struct BenchmarkMetricResult(BenchmarkMetricDescriptor Descriptor, ReadOnlyMemory<decimal> Samples, BenchmarkMetricStatistics Statistics);
