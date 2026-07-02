using System.Runtime.CompilerServices;

namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 默认基准测试运行器实现.
/// </summary>
public sealed class DefaultBenchmarkRunner : IBenchmarkRunner
{
    private readonly IBenchmarkMetricCollector[] m_Collectors;
    private readonly IBenchmarkStatisticsCalculator m_StatisticsCalculator;
    private static IBenchmarkMetricCollector[] DefaultCollectors => field ??=
    [
        TimeBenchmarkMetricCollector.Instance, MemoryBenchmarkMetricCollector.Instance
    ];
    
    /// <summary>
    /// 使用指定采样器和统计器创建运行器.
    /// </summary>
    /// <param name="collectors">指标采样器集合, 为空时使用默认采样器.</param>
    /// <param name="statisticsCalculator">统计计算器, 为空时使用默认实现.</param>
    public DefaultBenchmarkRunner(IEnumerable<IBenchmarkMetricCollector>? collectors = null, IBenchmarkStatisticsCalculator? statisticsCalculator = null)
    {
        m_Collectors = collectors is null ? DefaultCollectors : [..collectors];

        if (m_Collectors.Length == 0) throw new ArgumentException("At least one metric collector is required.", nameof(collectors));

        m_StatisticsCalculator = statisticsCalculator ?? DefaultBenchmarkStatisticsCalculator.Instance;
    }

    /// <summary>
    /// 默认单例实例.
    /// </summary>
    public static readonly DefaultBenchmarkRunner Instance = new DefaultBenchmarkRunner();

    /// <summary>
    /// 运行基准测试并返回结构化结果.
    /// </summary>
    /// <param name="option">测试选项.</param>
    /// <param name="methodList">待测试的方法列表.</param>
    /// <returns>结构化结果.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public BenchmarkRunResult<TResult> Run<TResult>(ref BenchmarkOption option, ReadOnlySpan<MethodWrapper<TResult>> methodList)
    {
        option = ValidateOption(option);
        BenchmarkOption optionCopy = option;
        IBenchmarkMetricCollector[] enabledCollectors =
        [
            ..m_Collectors.Where(collector => (optionCopy.Option & collector.Descriptor.DisableFlag) == 0)
        ];
        if (enabledCollectors.Length == 0)
        {
            return new BenchmarkRunResult<TResult>(option, []);
        }

        Benchmark.GcCollect(option.Option);
        if ((option.Option & BenchmarkFlags.NoWarm) == 0)
        {
            WarmupMethods(option, methodList);
        }
        option = CalibrateTimeLoopCount(option, methodList, enabledCollectors);

        List<BenchmarkCaseResult<TResult>> cases = new List<BenchmarkCaseResult<TResult>>(methodList.Length);
        foreach (MethodWrapper<TResult> method in methodList)
        {
            List<BenchmarkMetricResult> metrics = new List<BenchmarkMetricResult>(enabledCollectors.Length);

            foreach (IBenchmarkMetricCollector collector in enabledCollectors)
            {
                decimal[] rawSamples = collector.Collect(method.Method, option, option.Iterations);
                decimal[] selectedSamples = SelectSamples(rawSamples, option.Option);
                NormalizePerLoop(selectedSamples, option.LoopCount);

                BenchmarkMetricStatistics statistics = m_StatisticsCalculator.Calculate(selectedSamples);
                metrics.Add(new BenchmarkMetricResult(collector.Descriptor, selectedSamples, statistics));
            }

            cases.Add(new BenchmarkCaseResult<TResult>(method, method.Execute(), metrics.ToArray()));
        }

        return new BenchmarkRunResult<TResult>(option, cases.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void WarmupMethods<TResult>(BenchmarkOption option, ReadOnlySpan<MethodWrapper<TResult>> methodList)
    {
        if ((option.Option & BenchmarkFlags.Parallel) > 0)
        {
            foreach (MethodWrapper<TResult> item in methodList)
            {
                Parallel.For(0, option.Iterations, _ =>
                {
                    for (int j = 0; j < option.LoopCount; j++)
                    {
                        item.Execute();
                    }
                    Benchmark.GcCollect(option.Option);
                });
                Benchmark.GcCollect(option.Option);
            }
        }
        else
        {
            foreach (MethodWrapper<TResult> item in methodList)
            {
                for (int i = 0; i < option.Iterations; i++)
                {
                    for (int j = 0; j < option.LoopCount; j++)
                    {
                        item.Execute();
                    }
                    Benchmark.GcCollect(option.Option);
                }
            }
        }

        Benchmark.GcCollect(option.Option);
    }

    private static BenchmarkOption CalibrateTimeLoopCount<TResult>(BenchmarkOption option, ReadOnlySpan<MethodWrapper<TResult>> methodList, IBenchmarkMetricCollector[] enabledCollectors)
    {
        if (option.TargetTime <= TimeSpan.Zero || methodList.IsEmpty) return option;

        foreach (IBenchmarkMetricCollector item in enabledCollectors)
        {
            if (item.Descriptor.Kind == BenchmarkMetricKind.Time)
            {
                return TimeBenchmarkMetricCollector.CalibrateLoopCount(option, methodList);
            }
        }

        return option;
    }

    private static BenchmarkOption ValidateOption(BenchmarkOption option)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(option.Iterations, 1u);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(option.Iterations, (uint)int.MaxValue);
        if (option.LoopCount < 1) option = option with { LoopCount = 1 };
        ArgumentOutOfRangeException.ThrowIfGreaterThan(option.LoopCount, (uint)int.MaxValue);
        ArgumentOutOfRangeException.ThrowIfLessThan(option.TargetTime, TimeSpan.Zero);
        return option;
    }

    private static decimal[] SelectSamples(decimal[] samples, BenchmarkFlags flags)
    {
        if ((flags & BenchmarkFlags.FullTest) != 0 || samples.Length <= 1) return samples;

        int takeCount = Math.Max(samples.Length - (samples.Length >> 2), 1);
        Array.Sort(samples);
        if (takeCount == samples.Length) return samples;

        decimal[] selected = new decimal[takeCount];
        Array.Copy(samples, selected, takeCount);
        return selected;
    }

    private static void NormalizePerLoop(decimal[] samples, uint loopCount)
    {
        if (loopCount <= 1) return;

        decimal divisor = loopCount;
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] /= divisor;
        }
    }
}
