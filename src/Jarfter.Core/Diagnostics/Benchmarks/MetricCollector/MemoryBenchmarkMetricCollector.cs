namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 内存指标采样器.
/// </summary>
public sealed class MemoryBenchmarkMetricCollector : IBenchmarkMetricCollector
{
    private MemoryBenchmarkMetricCollector() { }

    /// <summary>
    /// 默认单例实例.
    /// </summary>
    public static readonly MemoryBenchmarkMetricCollector Instance = new MemoryBenchmarkMetricCollector();

    /// <summary>
    /// 指标描述信息.
    /// </summary>
    public BenchmarkMetricDescriptor Descriptor => new BenchmarkMetricDescriptor(
        BenchmarkMetricKind.Memory, "Memory", "B", BenchmarkFlags.NoMemoryTest
    );

    /// <summary>
    /// 执行内存采样并返回原始样本.
    /// </summary>
    /// <param name="method">待测方法.</param>
    /// <param name="option">测试选项.</param>
    /// <param name="iterations">采样轮次.</param>
    /// <returns>原始样本数组.</returns>
    public decimal[] Collect<TResult>(Func<TResult> method, BenchmarkOption option, uint iterations)
    {
        decimal[] eachTest = new decimal[(int)iterations];
        if ((option.Option & BenchmarkFlags.Parallel) > 0)
        {
            Parallel.For(0, eachTest.Length, i =>
            {
                Benchmark.GcCollect(option.Option);
                long memoryStart = GC.GetAllocatedBytesForCurrentThread();
                for (int index = (int)option.LoopCount; index > 0; index--) _ = method.Invoke();
                long memoryEnd = GC.GetAllocatedBytesForCurrentThread();
                eachTest[i] = memoryEnd - memoryStart;
                Benchmark.GcCollect(option.Option);
            });
        }
        else
        {
            for (int i = 0; i < eachTest.Length; i++)
            {
                Benchmark.GcCollect(option.Option);
                long memoryStart = GC.GetAllocatedBytesForCurrentThread();
                for (int index = (int)option.LoopCount; index > 0; index--) _ = method.Invoke();
                long memoryEnd = GC.GetAllocatedBytesForCurrentThread();
                eachTest[i] = memoryEnd - memoryStart;
                Benchmark.GcCollect(option.Option);
            }
        }

        return eachTest;
    }
}
