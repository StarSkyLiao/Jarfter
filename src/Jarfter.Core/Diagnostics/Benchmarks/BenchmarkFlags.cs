namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示基准测试执行过程中的可选行为标志。
/// </summary>
[Flags]
public enum BenchmarkFlags: uint
{
    /// <summary>
    /// 使用默认测试行为。
    /// </summary>
    None         = 0,
    /// <summary>
    /// 跳过时间开销测试。
    /// </summary>
    NoTimeTest   = 0b1 << 0, // 不需要测试时间开销
    /// <summary>
    /// 跳过内存开销测试。
    /// </summary>
    NoMemoryTest = 0b1 << 1, // 不需要测试内存开销
    
    /// <summary>
    /// 跳过测试前的预热执行。
    /// </summary>
    NoWarm       = 0b1 << 4, // 测试前, 不先进行一次预热
    /// <summary>
    /// 使用并行方式执行内存测量, 时间测量仍保持单线程。
    /// </summary>
    Parallel     = 0b1 << 5, // 内存测试时, 使用并行的方式进行测量. 时间测试仍然保持单线程
    /// <summary>
    /// 使用全部测试用例计算结果, 而不是只考察表现最好的部分用例。
    /// </summary>
    FullTest     = 0b1 << 6, // 默认只会单独考量表现最好的 75% 测试用例, 启用后, 考虑全部用例
    // Simple       = 0b1 << 7, // 简化测试结果, 只展示最核心的数据
    /// <summary>
    /// 测试过程中不主动触发垃圾回收。
    /// </summary>
    NoExplicitGc = 0b1 << 8, // 测试过程中不要主动触发垃圾回收
}
