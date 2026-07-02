using Jarfter.Core.Collections.Generic;
using Jarfter.Core.Diagnostics;

namespace Jarfter.Core.xUnit.Collections.Generic;

public static class SpanListRunTest
{
    private const int ElementCount = 128;
    private static readonly List<int> s_ReusedPopulateList = new List<int>(ElementCount);
    private static readonly List<int> s_ReusedInsertList = new List<int>(ElementCount);

    public static void Run()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(50) { LoopCount = 2000 }, CreateMethods());
    }

    private static int PopulateAndSumSpanList()
    {
        Span<int> buffer = stackalloc int[ElementCount];
        SpanList<int> list = new SpanList<int>(buffer);

        for (int i = 0; i < ElementCount; i++) list.Add(i);

        int sum = 0;
        foreach (int item in list) sum += item;
        return sum;
    }

    private static int PopulateAndSumList()
    {
        List<int> list = new List<int>(ElementCount);
        for (int i = 0; i < ElementCount; i++) list.Add(i);

        int sum = 0;
        foreach (int item in list) sum += item;
        return sum;
    }

    private static int PopulateAndSumReusedList()
    {
        List<int> list = s_ReusedPopulateList;
        list.Clear();

        for (int i = 0; i < ElementCount; i++) list.Add(i);

        int sum = 0;
        foreach (int item in list) sum += item;
        return sum;
    }

    private static int InsertAndRemoveSpanList()
    {
        Span<int> buffer = stackalloc int[ElementCount];
        SpanList<int> list = new SpanList<int>(buffer);

        for (int i = 0; i < ElementCount / 2; i++) list.Add(i);
        for (int i = 0; i < ElementCount / 4; i++) list.Insert(0, i);
        for (int i = 0; i < ElementCount / 4; i++) list.RemoveAt(list.Count - 1);

        int sum = 0;
        foreach (int item in list) sum += item;
        return sum;
    }

    private static int InsertAndRemoveList()
    {
        List<int> list = new List<int>(ElementCount);

        for (int i = 0; i < ElementCount / 2; i++) list.Add(i);
        for (int i = 0; i < ElementCount / 4; i++) list.Insert(0, i);
        for (int i = 0; i < ElementCount / 4; i++) list.RemoveAt(list.Count - 1);

        int sum = 0;
        foreach (int item in list) sum += item;
        return sum;
    }

    private static int InsertAndRemoveReusedList()
    {
        List<int> list = s_ReusedInsertList;
        list.Clear();

        for (int i = 0; i < ElementCount / 2; i++) list.Add(i);
        for (int i = 0; i < ElementCount / 4; i++) list.Insert(0, i);
        for (int i = 0; i < ElementCount / 4; i++) list.RemoveAt(list.Count - 1);

        int sum = 0;
        foreach (int item in list) sum += item;
        return sum;
    }

    private static MethodWrapper<int>[] CreateMethods() =>
    [
        new MethodWrapper<int>(PopulateAndSumSpanList),
        new MethodWrapper<int>(PopulateAndSumList),
        new MethodWrapper<int>(PopulateAndSumReusedList),
        new MethodWrapper<int>(InsertAndRemoveSpanList),
        new MethodWrapper<int>(InsertAndRemoveList),
        new MethodWrapper<int>(InsertAndRemoveReusedList),
    ];
}
