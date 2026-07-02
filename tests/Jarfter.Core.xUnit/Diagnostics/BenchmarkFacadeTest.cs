using Jarfter.Core.Diagnostics;

namespace Jarfter.Core.xUnit.Diagnostics;

public sealed class BenchmarkFacadeTest
{
    [Fact]
    public void Run_WithZeroIterations_ShouldThrow()
    {
        BenchmarkOption option = new BenchmarkOption(0);
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Benchmark.Run(option, new MethodWrapper<int>(() => 1)));
        Assert.Equal("option.Iterations", exception.ParamName);
    }

    [Fact]
    public void Run_WithCustomRunner_ShouldUseProvidedRunner()
    {
        FakeRunner runner = new FakeRunner();
        BenchmarkOption option = new BenchmarkOption(2);
        MethodWrapper<int>[] methods = [new MethodWrapper<int>(() => 1), new MethodWrapper<int>(() => 2)];

        BenchmarkRunResult<int> result = Benchmark.Run(runner, option, methods);

        Assert.True(runner.Called);
        Assert.Equal(option, runner.LastOption);
        Assert.Equal(methods.Length, runner.LastMethodCount);
        Assert.Equal(option, result.Option);
    }

    private sealed class FakeRunner : IBenchmarkRunner
    {
        public bool Called { get; private set; }

        public BenchmarkOption LastOption { get; private set; }

        public int LastMethodCount { get; private set; }

        public BenchmarkRunResult<TResult> Run<TResult>(ref BenchmarkOption option, ReadOnlySpan<MethodWrapper<TResult>> methodList)
        {
            Called = true;
            LastOption = option;
            LastMethodCount = methodList.Length;
            return new BenchmarkRunResult<TResult>(option, []);
        }
    }
}
