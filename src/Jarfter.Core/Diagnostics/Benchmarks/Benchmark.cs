namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 提供基准测试的运行与展示入口.
/// </summary>
public static class Benchmark
{
    private static readonly DefaultBenchmarkRunner s_DefaultRunner = DefaultBenchmarkRunner.Instance;
    private static readonly IBenchmarkDisplayer s_DefaultDisplayer = DefaultBenchmarkDisplayer.Instance;

    /// <summary>
    /// 运行基准测试并返回结构化结果.
    /// </summary>
    /// <param name="option">测试选项.</param>
    /// <param name="methodList">待测试的方法列表.</param>
    /// <returns>结构化基准测试结果.</returns>
    public static BenchmarkRunResult<TResult> Run<TResult>(BenchmarkOption option, params ReadOnlySpan<MethodWrapper<TResult>> methodList)
        => s_DefaultRunner.Run(ref option, methodList);

    /// <summary>
    /// 使用指定运行器执行基准测试并返回结构化结果.
    /// </summary>
    /// <param name="runner">运行器实现.</param>
    /// <param name="option">测试选项.</param>
    /// <param name="methodList">待测试的方法列表.</param>
    /// <returns>结构化基准测试结果.</returns>
    public static BenchmarkRunResult<TResult> Run<TResult>(IBenchmarkRunner runner, BenchmarkOption option, params ReadOnlySpan<MethodWrapper<TResult>> methodList)
    {
        ArgumentNullException.ThrowIfNull(runner);
        return runner.Run(ref option, methodList);
    }

    /// <summary>
    /// 快速入口: 运行测试并输出到控制台.
    /// </summary>
    /// <param name="option">测试选项.</param>
    /// <param name="methodList">待测试的方法列表.</param>
    public static void RunQuickTest<TResult>(BenchmarkOption option, params ReadOnlySpan<MethodWrapper<TResult>> methodList)
    {
        BenchmarkRunResult<TResult> result = Run(option, methodList);
        Display(result, new BenchmarkViewOptions(), Console.Out, s_DefaultDisplayer);
    }

    /// <summary>
    /// 将结构化结果输出到指定文本写入器.
    /// </summary>
    /// <param name="result">基准测试结果.</param>
    /// <param name="viewOptions">展示选项.</param>
    /// <param name="textWriter">目标输出写入器.</param>
    /// <param name="displayer">展示器实现.</param>
    public static void Display<TResult>(BenchmarkRunResult<TResult> result, BenchmarkViewOptions? viewOptions = null, TextWriter? textWriter = null, IBenchmarkDisplayer? displayer = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        BenchmarkViewOptions options = viewOptions ?? new BenchmarkViewOptions();
        TextWriter writer = textWriter ?? Console.Out;
        IBenchmarkDisplayer output = displayer ?? s_DefaultDisplayer;
        output.Write(options, result.Option, writer, result);
    }

    internal static void GcCollect(BenchmarkFlags option)
    {
        if ((option & BenchmarkFlags.NoExplicitGc) != 0) return;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
