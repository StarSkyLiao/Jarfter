using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 耗时指标采样器.
/// </summary>
public sealed class TimeBenchmarkMetricCollector : IBenchmarkMetricCollector
{
    private const decimal MaxCalibrationSeconds = 0.001m;
    private const int MaxCalibrationAttempts = 8;

    private TimeBenchmarkMetricCollector() { }

    /// <summary>
    /// 默认单例实例.
    /// </summary>
    public static readonly TimeBenchmarkMetricCollector Instance = new TimeBenchmarkMetricCollector();

    /// <summary>
    /// 指标描述信息.
    /// </summary>
    public BenchmarkMetricDescriptor Descriptor => new BenchmarkMetricDescriptor(
        BenchmarkMetricKind.Time, "Time", "s", BenchmarkFlags.NoTimeTest
    );

    /// <summary>
    /// 执行耗时采样并返回原始样本.
    /// </summary>
    /// <param name="method">待测方法.</param>
    /// <param name="option">测试选项.</param>
    /// <param name="iterations">采样轮次.</param>
    /// <returns>原始样本数组.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public decimal[] Collect<TResult>(Func<TResult> method, BenchmarkOption option, uint iterations)
    {
        int loopCount = (int)option.LoopCount;
        decimal[] eachTest = new decimal[(int)iterations];
        Span<decimal> span = eachTest.AsSpan();
        for (int i = 0; i < eachTest.Length; i++)
        {
            Benchmark.GcCollect(option.Option);
            long baselineTicks = MeasureBaselineTicks<TResult>(loopCount);
            long elapsedTicks = MeasureTicks(method, loopCount);
            span[i] = StopwatchTicksToSeconds(Math.Max(elapsedTicks - baselineTicks, 0));
            Benchmark.GcCollect(option.Option);
        }

        return eachTest;
    }

    internal static BenchmarkOption CalibrateLoopCount<TResult>(BenchmarkOption option, ReadOnlySpan<MethodWrapper<TResult>> methodList)
    {
        decimal targetSeconds = TimeSpanToSeconds(option.TargetTime);
        decimal calibrationTargetSeconds = Math.Min(targetSeconds, MaxCalibrationSeconds);
        decimal minSecondsPerOperation = decimal.MaxValue;

        foreach (MethodWrapper<TResult> wrapper in methodList)
        {
            decimal secondsPerOperation = EstimateSecondsPerOperation(wrapper.Method, option.LoopCount, calibrationTargetSeconds);
            if (secondsPerOperation > 0 && secondsPerOperation < minSecondsPerOperation)
            {
                minSecondsPerOperation = secondsPerOperation;
            }
        }

        if (minSecondsPerOperation == decimal.MaxValue) return option;
        return option with { LoopCount = CalculateLoopCount(targetSeconds, minSecondsPerOperation) };
    }

    private static decimal EstimateSecondsPerOperation<TResult>(Func<TResult> method, uint initialLoopCount, decimal calibrationTargetSeconds)
    {
        int loopCount = (int)initialLoopCount;
        decimal latestSecondsPerOperation = 0;

        for (int attempt = 0; attempt < MaxCalibrationAttempts; attempt++)
        {
            long baselineTicks = MeasureBaselineTicks<TResult>(loopCount);
            long elapsedTicks = MeasureTicks(method, loopCount);
            decimal netSeconds = StopwatchTicksToSeconds(Math.Max(elapsedTicks - baselineTicks, 0));

            if (netSeconds > 0)
            {
                latestSecondsPerOperation = netSeconds / loopCount;
                if (netSeconds >= calibrationTargetSeconds || loopCount == int.MaxValue)
                {
                    return latestSecondsPerOperation;
                }

                int estimatedLoopCount = (int)CalculateLoopCount(calibrationTargetSeconds, latestSecondsPerOperation);
                loopCount = Math.Max(estimatedLoopCount, GrowLoopCount(loopCount));
            }
            else
            {
                loopCount = GrowLoopCount(loopCount);
            }
        }

        return latestSecondsPerOperation;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static long MeasureTicks<TResult>(Func<TResult> method, int loopCount)
    {
        long start = Stopwatch.GetTimestamp();
        for (int index = loopCount; index > 0; index--) _ = method.Invoke();
        return Stopwatch.GetTimestamp() - start;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static long MeasureBaselineTicks<TResult>(int loopCount)
        => MeasureTicks(Baseline<TResult>.Method, loopCount);

    private static uint CalculateLoopCount(decimal targetSeconds, decimal secondsPerOperation)
    {
        decimal loopCount = Math.Ceiling(targetSeconds / secondsPerOperation);
        if (loopCount < 1) return 1;
        if (loopCount > int.MaxValue) return int.MaxValue;
        return (uint)loopCount;
    }

    private static int GrowLoopCount(int loopCount)
        => loopCount >= int.MaxValue / 2 ? int.MaxValue : loopCount * 2;

    private static decimal StopwatchTicksToSeconds(long stopwatchTicks)
        => (decimal)stopwatchTicks / Stopwatch.Frequency;

    private static decimal TimeSpanToSeconds(TimeSpan timeSpan)
        => (decimal)timeSpan.Ticks / TimeSpan.TicksPerSecond;

    private static class Baseline<TResult>
    {
        internal static readonly Func<TResult> Method = static () => default!;
    }
}
