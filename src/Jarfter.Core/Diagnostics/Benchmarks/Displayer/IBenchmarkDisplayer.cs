namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 定义基准结果展示器接口.
/// </summary>
public interface IBenchmarkDisplayer
{
    /// <summary>
    /// 将结果写入目标文本写入器.
    /// </summary>
    /// <param name="viewOptions">展示选项.</param>
    /// <param name="benchmarkOption">基准测试运行选项.</param>
    /// <param name="textWriter">文本写入器.</param>
    /// <param name="result">基准结果.</param>
    public void Write<TResult>(BenchmarkViewOptions viewOptions, BenchmarkOption benchmarkOption, TextWriter textWriter, BenchmarkRunResult<TResult> result);
}
