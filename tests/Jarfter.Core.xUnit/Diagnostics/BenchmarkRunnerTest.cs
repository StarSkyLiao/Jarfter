using Jarfter.Core.Diagnostics;

namespace Jarfter.Core.xUnit.Diagnostics;

public sealed class BenchmarkRunnerTest
{
    [Fact]
    public void DefaultRunner_WithoutFullTest_ShouldTrimSlowestQuarterAndNormalizeByLoop()
    {
        BenchmarkMetricDescriptor descriptor = new(BenchmarkMetricKind.Time, "FakeTime", "u", BenchmarkFlags.NoTimeTest);
        DefaultBenchmarkRunner runner = new DefaultBenchmarkRunner(
        [
            new FixedCollector(descriptor, [8m, 4m, 12m, 16m]),
        ]);

        BenchmarkOption option = new BenchmarkOption(4) { LoopCount = 2 };
        BenchmarkRunResult<int> result = Benchmark.Run(runner, option, [new MethodWrapper<int>(() => 123)]);

        BenchmarkCaseResult<int> @case = Assert.Single(result.Cases);
        BenchmarkMetricResult metric = Assert.Single(@case.Metrics);

        Assert.Equal([2m, 4m, 6m], metric.Samples.ToArray());
        Assert.Equal(12m, metric.Statistics.Sum);
        Assert.Equal(4m, metric.Statistics.Mean);
        Assert.InRange(metric.Statistics.StdDev, 1.6329m, 1.6331m);
        Assert.InRange(metric.Statistics.Rsd, 0.4082m, 0.4083m);
    }

    [Fact]
    public void DefaultRunner_WithFullTest_ShouldKeepAllSamples()
    {
        BenchmarkMetricDescriptor descriptor = new(BenchmarkMetricKind.Time, "FakeTime", "u", BenchmarkFlags.NoTimeTest);
        DefaultBenchmarkRunner runner = new DefaultBenchmarkRunner(
        [
            new FixedCollector(descriptor, [8m, 4m, 12m, 16m]),
        ]);

        BenchmarkOption option = new BenchmarkOption(4, BenchmarkFlags.FullTest) { LoopCount = 2 };
        BenchmarkRunResult<int> result = Benchmark.Run(runner, option, [new MethodWrapper<int>(() => 123)]);

        BenchmarkMetricResult metric = Assert.Single(Assert.Single(result.Cases).Metrics);
        Assert.Equal([4m, 2m, 6m, 8m], metric.Samples.ToArray());
    }

    [Fact]
    public void DefaultRunner_WithTargetTime_ShouldCalibrateLoopCount()
    {
        DefaultBenchmarkRunner runner = new DefaultBenchmarkRunner([TimeBenchmarkMetricCollector.Instance]);
        BenchmarkOption option = new BenchmarkOption(1, BenchmarkFlags.FullTest | BenchmarkFlags.NoWarm | BenchmarkFlags.NoExplicitGc)
        {
            LoopCount = 1,
            TargetTime = TimeSpan.FromMilliseconds(1),
        };

        BenchmarkRunResult<int> result = Benchmark.Run(runner, option, [new MethodWrapper<int>(SpinMethod)]);

        BenchmarkMetricResult metric = Assert.Single(Assert.Single(result.Cases).Metrics);
        Assert.True(result.Option.LoopCount > 1);
        Assert.Single(metric.Samples.ToArray());
        Assert.True(metric.Statistics.Mean >= 0);
    }

    [Fact]
    public void DefaultRunner_WithTargetTimeAndNoTimeTest_ShouldKeepConfiguredLoopCount()
    {
        BenchmarkOption option = new BenchmarkOption(1, BenchmarkFlags.NoTimeTest | BenchmarkFlags.NoWarm | BenchmarkFlags.NoExplicitGc)
        {
            LoopCount = 3,
            TargetTime = TimeSpan.FromMilliseconds(1),
        };

        BenchmarkRunResult<int> result = Benchmark.Run(option, [new MethodWrapper<int>(() => 1)]);

        Assert.Equal(3u, result.Option.LoopCount);
    }

    [Fact]
    public void DefaultRunner_ShouldHonorMetricDisableFlags()
    {
        BenchmarkMetricDescriptor timeDescriptor = new(BenchmarkMetricKind.Time, "Time", "u", BenchmarkFlags.NoTimeTest);
        BenchmarkMetricDescriptor memoryDescriptor = new(BenchmarkMetricKind.Memory, "Memory", "u", BenchmarkFlags.NoMemoryTest);
        DefaultBenchmarkRunner runner = new DefaultBenchmarkRunner(
        [
            new FixedCollector(timeDescriptor, [1m]),
            new FixedCollector(memoryDescriptor, [2m]),
        ]);

        BenchmarkOption option = new BenchmarkOption(1, BenchmarkFlags.NoMemoryTest);
        BenchmarkRunResult<int> result = Benchmark.Run(runner, option, [new MethodWrapper<int>(() => 1)]);

        BenchmarkCaseResult<int> @case = Assert.Single(result.Cases);
        BenchmarkMetricResult metric = Assert.Single(@case.Metrics);
        Assert.Equal(BenchmarkMetricKind.Time, metric.Descriptor.Kind);
    }

    [Fact]
    public void DefaultRunner_WithEmptyCollectors_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => new DefaultBenchmarkRunner([]));
    }

    private static int SpinMethod()
    {
        Thread.SpinWait(100);
        return 1;
    }

    private sealed class FixedCollector(BenchmarkMetricDescriptor descriptor, decimal[] samples) : IBenchmarkMetricCollector
    {
        public BenchmarkMetricDescriptor Descriptor { get; } = descriptor;

        public decimal[] Collect<TResult>(Func<TResult> method, BenchmarkOption option, uint iterations)
        {
            Assert.Equal((uint)samples.Length, iterations);
            return samples.ToArray();
        }
    }
}
