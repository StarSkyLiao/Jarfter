namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 基准指标类型.
/// </summary>
public enum BenchmarkMetricKind : byte
{
    /// <summary>
    /// 耗时指标.
    /// </summary>
    Time = 1,

    /// <summary>
    /// 内存指标.
    /// </summary>
    Memory = 2,
}
