namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 定义基准测试运行器接口.
/// </summary>
public interface IBenchmarkRunner
{
    /// <summary>
    /// 运行基准测试并返回结构化结果.
    /// </summary>
    /// <param name="option">测试选项.</param>
    /// <param name="methodList">待测试的方法列表.</param>
    /// <returns>结构化结果.</returns>
    public BenchmarkRunResult<TResult> Run<TResult>(ref BenchmarkOption option, ReadOnlySpan<MethodWrapper<TResult>> methodList);
}
