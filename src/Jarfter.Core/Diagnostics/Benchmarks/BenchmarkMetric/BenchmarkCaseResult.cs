namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示单个测试用例结果.
/// </summary>
/// <typeparam name="TResult">方法返回值类型.</typeparam>
/// <param name="Method">方法包装器.</param>
/// <param name="Return">方法返回值.</param>
/// <param name="Metrics">指标结果列表.</param>
public sealed record BenchmarkCaseResult<TResult>(MethodWrapper<TResult> Method, TResult Return, IReadOnlyList<BenchmarkMetricResult> Metrics)
{
    /// <summary>
    /// 方法显示名称.
    /// </summary>
    public string MethodName => Method.MethodName ?? "<unknown>";

    /// <summary>
    /// 按指标类型获取指标结果.
    /// </summary>
    /// <param name="kind">指标类型.</param>
    /// <returns>匹配结果, 不存在则返回 <c>null</c>.</returns>
    public BenchmarkMetricResult? GetMetric(BenchmarkMetricKind kind)
    {
        for (int i = 0; i < Metrics.Count; i++)
        {
            BenchmarkMetricResult metric = Metrics[i];
            if (metric.Descriptor.Kind == kind) return metric;
        }

        return null;
    }
}
