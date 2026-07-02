namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示基准测试运行选项.
/// </summary>
/// <param name="Iterations">采样轮次.</param>
/// <param name="Option">运行标志.</param>
public record struct BenchmarkOption(uint Iterations, BenchmarkFlags Option = BenchmarkFlags.None)
{
    /// <summary>
    /// 每轮采样中重复执行待测方法的次数.
    /// </summary>
    public uint LoopCount { get; init; } = 1;

    /// <summary>
    /// 自动校准 LoopCount 时使用的目标单轮采样时长. 值小于或等于零时禁用自动校准.
    /// </summary>
    public TimeSpan TargetTime { get; init; } = TimeSpan.Zero;
}
