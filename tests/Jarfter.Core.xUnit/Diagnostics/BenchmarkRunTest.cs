using Jarfter.Core.Diagnostics;

namespace Jarfter.Core.xUnit.Diagnostics;

public static class BenchmarkRunTest
{
    public static void Run()
    {
        // ReSharper disable once RedundantExplicitParamsArrayCreation
        Benchmark.RunQuickTest(new BenchmarkOption(50) { LoopCount = 10 }, [
            new MethodWrapper<int>(SumByForeachArray),
            new MethodWrapper<int>(SumByForeachList),
            new MethodWrapper<int>(SumByForeachSpan),
            new MethodWrapper<int>(SumByForArray),
            new MethodWrapper<int>(SumByForList),
            new MethodWrapper<int>(SumByForSpan),
            new MethodWrapper<int>(SumBySumArray),
            new MethodWrapper<int>(SumBySumList),
            new MethodWrapper<int>(SumBySumStaticArray),
            new MethodWrapper<int>(SumBySumStaticList),
        ]);
    }

    private static int SumByForArray()
    {
        int[] array = [1, 2, 3, 4, 5];
        int sum = 0;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int index = 0; index < array.Length; index++) sum += array[index];
        return sum;
    }

    private static int SumByForList()
    {
        List<int> list = [1, 2, 3, 4, 5];
        int sum = 0;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int index = 0; index < list.Count; index++) sum += list[index];
        return sum;
    }

    private static int SumByForSpan()
    {
        Span<int> list = [1, 2, 3, 4, 5];
        int sum = 0;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (int index = 0; index < list.Length; index++) sum += list[index];
        return sum;
    }

    private static int SumByForeachArray()
    {
        int[] array = [1, 2, 3, 4, 5];
        int sum = 0;
        foreach (int t in array) sum += t;
        return sum;
    }

    private static int SumByForeachList()
    {
        List<int> list = [1, 2, 3, 4, 5];
        int sum = 0;
        foreach (int t in list) sum += t;
        return sum;
    }

    private static int SumByForeachSpan()
    {
        Span<int> list = [1, 2, 3, 4, 5];
        int sum = 0;
        foreach (int t in list) sum += t;
        return sum;
    }

    private static int SumBySumArray()
    {
        int[] array = [1, 2, 3, 4, 5];
        return array.Sum();
    }

    private static int SumBySumList()
    {
        List<int> list = [1, 2, 3, 4, 5];
        return list.Sum();
    }

    private static readonly int[] s_Array = [1, 2, 3, 4, 5];
    private static readonly List<int> s_List = [1, 2, 3, 4, 5];
    private static int SumBySumStaticArray()
    {
        return s_Array.Sum();
    }

    private static int SumBySumStaticList()
    {
        return s_List.Sum();
    }

}
