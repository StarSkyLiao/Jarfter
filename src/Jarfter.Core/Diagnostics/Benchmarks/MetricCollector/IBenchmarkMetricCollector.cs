namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 定义单项指标采样器接口.
/// </summary>
public interface IBenchmarkMetricCollector
{
    /// <summary>
    /// 指标描述信息.
    /// </summary>
    public BenchmarkMetricDescriptor Descriptor { get; }

    /// <summary>
    /// 执行采样并返回原始样本.
    /// </summary>
    /// <param name="method">待测方法.</param>
    /// <param name="option">测试选项.</param>
    /// <param name="iterations">采样轮次.</param>
    /// <returns>原始样本数组.</returns>
    public decimal[] Collect<TResult>(Func<TResult> method, BenchmarkOption option, uint iterations);
}
