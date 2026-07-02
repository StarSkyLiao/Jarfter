using System.Text;

namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 默认基准结果展示器.
/// </summary>
public sealed class DefaultBenchmarkDisplayer : IBenchmarkDisplayer
{
    private DefaultBenchmarkDisplayer() { }

    /// <summary>
    /// 默认单例实例.
    /// </summary>
    public static readonly DefaultBenchmarkDisplayer Instance = new DefaultBenchmarkDisplayer();

    /// <summary>
    /// 将结果写入目标文本写入器.
    /// </summary>
    /// <param name="viewOptions">展示选项.</param>
    /// <param name="benchmarkOption">基准测试运行选项.</param>
    /// <param name="textWriter">文本写入器.</param>
    /// <param name="result">基准结果.</param>
    public void Write<TResult>(BenchmarkViewOptions viewOptions, BenchmarkOption benchmarkOption, TextWriter textWriter, BenchmarkRunResult<TResult> result)
    {
        ArgumentNullException.ThrowIfNull(textWriter);
        ArgumentNullException.ThrowIfNull(result);

        textWriter.WriteLine($"CLR Version: {Environment.Version}");
        textWriter.WriteLine($"CoreLib: {typeof(object).Assembly.FullName}");
        if (benchmarkOption.Iterations > 0)
        {
            textWriter.WriteLine($"BenchmarkOption: {benchmarkOption}");
        }

        List<BenchmarkMetricKind> metricOrder = CollectMetricOrder(result);
        if (metricOrder.Count == 0)
        {
            textWriter.WriteLine(viewOptions.EmptyMessage);
            return;
        }

        int methodWidth = CalculateMethodWidth(viewOptions, result);
        for (int metricGroupIndex = 0; metricGroupIndex < metricOrder.Count; metricGroupIndex++)
        {
            BenchmarkMetricKind metricKind = metricOrder[metricGroupIndex];
            WriteHeader(viewOptions, textWriter, methodWidth);

            for (int caseIndex = 0; caseIndex < result.Cases.Count; caseIndex++)
            {
                BenchmarkCaseResult<TResult> caseResult = result.Cases[caseIndex];
                BenchmarkMetricResult? metric = caseResult.GetMetric(metricKind);
                if (metric is null) continue;

                WriteRow(viewOptions, textWriter, methodWidth, caseResult, metric.Value, caseIndex);
            }

            if (metricGroupIndex < metricOrder.Count - 1) textWriter.WriteLine();
        }

        textWriter.WriteLine();
    }

    private static List<BenchmarkMetricKind> CollectMetricOrder<TResult>(BenchmarkRunResult<TResult> result)
    {
        List<BenchmarkMetricKind> order = new List<BenchmarkMetricKind>();
        foreach (BenchmarkCaseResult<TResult> item in result.Cases)
        {
            IReadOnlyList<BenchmarkMetricResult> metrics = item.Metrics;
            foreach (BenchmarkMetricResult metric in metrics)
            {
                BenchmarkMetricKind kind = metric.Descriptor.Kind;
                if (order.Contains(kind)) continue;
                order.Add(kind);
            }
        }

        return order;
    }

    private static int CalculateMethodWidth<TResult>(BenchmarkViewOptions options, BenchmarkRunResult<TResult> result)
    {
        int maxLength = options.MethodHeader.Length;
        foreach (BenchmarkCaseResult<TResult> item in result.Cases)
        {
            int length = item.MethodName.Length;
            if (length > maxLength) maxLength = length;
        }

        return maxLength + 2;
    }

    private static void WriteHeader(BenchmarkViewOptions options, TextWriter writer, int methodWidth)
    {
        string header = new StringBuilder()
            .Append(options.MethodHeader.PadRight(methodWidth - 4))
            .Append($"{options.TypeHeader,-6}")
            .Append($"{options.MeanHeader,-13}")
            .Append($"{options.SumHeader,-13}")
            .Append($"{options.StdDevHeader,-12}")
            .Append($"{options.RsdHeader,-7}")
            .Append($"      {options.ReturnHeader,-9}")
            .ToString();

        WriteWithColor(options, writer, ConsoleColor.Cyan, static (w, text) => w.WriteLine(text), header);
    }

    private static void WriteRow<TResult>(BenchmarkViewOptions options, TextWriter writer, int methodWidth, BenchmarkCaseResult<TResult> caseResult, BenchmarkMetricResult metric, int colorIndex)
    {
        string line = new StringBuilder()
            .Append((caseResult.Method.MethodName ?? options.UnknownMethodName).PadRight(methodWidth))
            .Append($"{metric.Descriptor.Name,-10}")
            .Append($"{$"{metric.Statistics.Mean.Fmt("F2")}{metric.Descriptor.Unit}",-15}")
            .Append($"{$"{metric.Statistics.Sum.Fmt("F2")}{metric.Descriptor.Unit}",-15}")
            .Append($"{$"{metric.Statistics.StdDev.Fmt("F2")}{metric.Descriptor.Unit}",-15}")
            .Append($"{metric.Statistics.Rsd,-12:P2}")
            .Append($"      {caseResult.Return,-12}")
            .ToString();

        ConsoleColor color = (ConsoleColor)(colorIndex % 15 + 1);
        WriteWithColor(options, writer, color, static (w, text) => w.WriteLine(text), line);
    }

    private static void WriteWithColor<TState>(BenchmarkViewOptions options, TextWriter writer, ConsoleColor color, Action<TextWriter, TState> action, TState state)
    {
        if (!options.UseColor || !IsConsoleWriter(writer))
        {
            action(writer, state);
            return;
        }

        ConsoleColor defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        action(writer, state);
        Console.ForegroundColor = defaultColor;
    }

    private static bool IsConsoleWriter(TextWriter writer)
    {
        return ReferenceEquals(writer, Console.Out) || ReferenceEquals(writer, Console.Error);
    }
}
