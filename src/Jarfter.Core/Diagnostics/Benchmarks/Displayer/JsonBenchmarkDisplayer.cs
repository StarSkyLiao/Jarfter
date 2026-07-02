using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 将基准结果序列化为 JSON 的展示器.
/// </summary>
public sealed class JsonBenchmarkDisplayer : IBenchmarkDisplayer
{
    /// <summary>
    /// 默认单例实例.
    /// </summary>
    public static readonly JsonBenchmarkDisplayer Instance = new JsonBenchmarkDisplayer();

    /// <summary>
    /// 当前展示器使用的序列化配置.
    /// </summary>
    public JsonSerializerOptions SerializerOptions { get; }

    /// <summary>
    /// 创建 JSON 展示器.
    /// </summary>
    /// <param name="serializerOptions">序列化配置. 为空时使用默认配置.</param>
    public JsonBenchmarkDisplayer(JsonSerializerOptions? serializerOptions = null)
    {
        SerializerOptions = serializerOptions ?? CreateDefaultSerializerOptions();
    }

    /// <summary>
    /// 将结果写入目标文本写入器.
    /// </summary>
    /// <param name="viewOptions">展示选项. JSON 展示器不使用该选项, 仅用于保持接口一致.</param>
    /// <param name="benchmarkOption">基准测试运行选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    /// <param name="result">基准结果.</param>
    public void Write<TResult>(BenchmarkViewOptions viewOptions, BenchmarkOption benchmarkOption, TextWriter textWriter, BenchmarkRunResult<TResult> result)
    {
        ArgumentNullException.ThrowIfNull(textWriter);
        ArgumentNullException.ThrowIfNull(result);

        JsonPayload<TResult> payload = CreatePayload(benchmarkOption, result);
        textWriter.Write(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    private static JsonSerializerOptions CreateDefaultSerializerOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private static JsonPayload<TResult> CreatePayload<TResult>(BenchmarkOption benchmarkOption, BenchmarkRunResult<TResult> result)
    {
        JsonCase<TResult>[] cases = new JsonCase<TResult>[result.Cases.Count];
        for (int caseIndex = 0; caseIndex < result.Cases.Count; caseIndex++)
        {
            BenchmarkCaseResult<TResult> caseResult = result.Cases[caseIndex];
            JsonMetric[] metrics = new JsonMetric[caseResult.Metrics.Count];
            for (int metricIndex = 0; metricIndex < caseResult.Metrics.Count; metricIndex++)
            {
                BenchmarkMetricResult metric = caseResult.Metrics[metricIndex];
                metrics[metricIndex] = new JsonMetric(
                    metric.Descriptor.Kind,
                    metric.Descriptor.Name,
                    metric.Descriptor.Unit,
                    metric.Samples.ToArray(),
                    new JsonStatistics(
                        metric.Statistics.Sum,
                        metric.Statistics.Mean,
                        metric.Statistics.StdDev,
                        metric.Statistics.Rsd
                    )
                );
            }

            cases[caseIndex] = new JsonCase<TResult>(caseResult.MethodName, caseResult.Return, metrics);
        }

        return new JsonPayload<TResult>(
            new JsonOption(benchmarkOption.Iterations, benchmarkOption.LoopCount, benchmarkOption.TargetTime, benchmarkOption.Option),
            cases);
    }

    private sealed record JsonPayload<TResult>(JsonOption Option, JsonCase<TResult>[] Cases);

    private sealed record JsonOption(uint Iterations, uint LoopCount, TimeSpan TargetTime, BenchmarkFlags Flags);

    private sealed record JsonCase<TResult>(string MethodName, TResult Return, JsonMetric[] Metrics);

    private sealed record JsonMetric(BenchmarkMetricKind Kind, string Name, string Unit, decimal[] Samples, JsonStatistics Statistics);

    private sealed record JsonStatistics(decimal Sum, decimal Mean, decimal StdDev, decimal Rsd);
}
