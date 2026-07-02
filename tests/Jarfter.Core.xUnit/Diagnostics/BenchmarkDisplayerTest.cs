using System.Text.Json;
using System.Text.Json.Serialization;
using Jarfter.Core.Diagnostics;

namespace Jarfter.Core.xUnit.Diagnostics;

public sealed class BenchmarkDisplayerTest
{
    [Fact]
    public void Display_WithNoMetrics_ShouldWriteEmptyMessage()
    {
        BenchmarkRunResult<int> result = new BenchmarkRunResult<int>(new BenchmarkOption(1), [
            new BenchmarkCaseResult<int>(new MethodWrapper<int>(() => 1), 1, [])
        ]);
        BenchmarkViewOptions options = new BenchmarkViewOptions
        {
            EmptyMessage = "EMPTY",
            UseColor = false,
        };

        using StringWriter writer = new StringWriter();
        Benchmark.Display(result, options, writer, DefaultBenchmarkDisplayer.Instance);

        Assert.Contains("EMPTY", writer.ToString(), StringComparison.Ordinal);
    }

    private static readonly decimal[] s_Samples = [1m, 2m];

    [Fact]
    public void JsonDisplayer_ShouldWriteStructuredJson()
    {
        BenchmarkMetricResult metric = new BenchmarkMetricResult(
            new BenchmarkMetricDescriptor(BenchmarkMetricKind.Time, "Time", "s", BenchmarkFlags.NoTimeTest),
            s_Samples,
            new BenchmarkMetricStatistics(3m, 1.5m, 0.5m, 0.3333m));

        BenchmarkRunResult<int> result = new BenchmarkRunResult<int>(new BenchmarkOption(2) { LoopCount = 3 }, [
            new BenchmarkCaseResult<int>(new MethodWrapper<int>(() => 7, "MyMethod"), 7, [metric]),
        ]);

        using StringWriter writer = new StringWriter();
        Benchmark.Display(result, new BenchmarkViewOptions(), writer, JsonBenchmarkDisplayer.Instance);

        using JsonDocument doc = JsonDocument.Parse(writer.ToString());
        JsonElement root = doc.RootElement;

        Assert.Equal(2u, root.GetProperty("option").GetProperty("iterations").GetUInt32());
        Assert.Equal(3u, root.GetProperty("option").GetProperty("loopCount").GetUInt32());
        Assert.Equal("time", root.GetProperty("cases")[0].GetProperty("metrics")[0].GetProperty("kind").GetString());
        Assert.Equal("MyMethod", root.GetProperty("cases")[0].GetProperty("methodName").GetString());
    }

    [Fact]
    public void JsonDisplayer_WithCustomSerializerOptions_ShouldHonorPropertyNaming()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            WriteIndented = false,
        };
        options.Converters.Add(new JsonStringEnumConverter());

        JsonBenchmarkDisplayer displayer = new JsonBenchmarkDisplayer(options);

        BenchmarkRunResult<int> result = new BenchmarkRunResult<int>(new BenchmarkOption(1), [
            new BenchmarkCaseResult<int>(new MethodWrapper<int>(() => 1, "M"), 1, []),
        ]);

        using StringWriter writer = new StringWriter();
        Benchmark.Display(result, new BenchmarkViewOptions(), writer, displayer);

        using JsonDocument doc = JsonDocument.Parse(writer.ToString());
        JsonElement root = doc.RootElement;
        Assert.True(root.TryGetProperty("Option", out _));
        Assert.True(root.TryGetProperty("Cases", out _));
    }
}
