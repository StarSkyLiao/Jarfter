namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示一次基准测试运行结果.
/// </summary>
/// <typeparam name="TResult">方法返回值类型.</typeparam>
/// <param name="Option">运行选项.</param>
/// <param name="Cases">各测试用例结果.</param>
public sealed record BenchmarkRunResult<TResult>(BenchmarkOption Option, IReadOnlyList<BenchmarkCaseResult<TResult>> Cases);
