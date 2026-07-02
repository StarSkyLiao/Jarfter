namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示指标描述信息.
/// </summary>
/// <param name="Kind">指标类型.</param>
/// <param name="Name">指标名称.</param>
/// <param name="Unit">指标单位.</param>
/// <param name="DisableFlag">禁用该指标对应的标记位.</param>
public readonly record struct BenchmarkMetricDescriptor(BenchmarkMetricKind Kind, string Name, string Unit, BenchmarkFlags DisableFlag);
