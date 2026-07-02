namespace Jarfter.Core.Diagnostics;

[Flags]
public enum BenchmarkFlags: uint
{
    None         = 0,
    NoTimeTest   = 0b1 << 0, // 不需要测试时间开销
    NoMemoryTest = 0b1 << 1, // 不需要测试内存开销
    
    NoWarm       = 0b1 << 4, // 测试前, 不先进行一次预热
    Parallel     = 0b1 << 5, // 内存测试时, 使用并行的方式进行测量. 时间测试仍然保持单线程
    FullTest     = 0b1 << 6, // 默认只会单独考量表现最好的 75% 测试用例, 启用后, 考虑全部用例
    // Simple       = 0b1 << 7, // 简化测试结果, 只展示最核心的数据
    NoExplicitGc = 0b1 << 8, // 测试过程中不要主动触发垃圾回收
}
