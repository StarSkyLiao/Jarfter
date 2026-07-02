namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示指标统计结果.
/// </summary>
/// <param name="Sum">总和.</param>
/// <param name="Mean">均值.</param>
/// <param name="StdDev">总体标准差.</param>
/// <param name="Rsd">相对标准差.</param>
public readonly record struct BenchmarkMetricStatistics(decimal Sum, decimal Mean, decimal StdDev, decimal Rsd);
