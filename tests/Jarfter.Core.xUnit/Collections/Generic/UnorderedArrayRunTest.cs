using System.Runtime.InteropServices;
using Jarfter.Core.Collections.Generic;
using Jarfter.Core.Diagnostics;
// ReSharper disable RedundantExplicitParamsArrayCreation

namespace Jarfter.Core.xUnit.Collections.Generic;

public static class UnorderedArrayRunTest
{
    private const int ElementCount = 512;
    private static readonly int[] s_SourceItems = CreateSourceItems();
    private static readonly int[] s_RemoveValues = CreateShuffledSourceItems();
    private static readonly List<int> s_ReusedList = new List<int>(ElementCount);
    private static readonly UnorderedArray<int> s_ReusedUnorderedArray = UnorderedArray.Create<int>(capacity: ElementCount);
    private static readonly UnorderedArray<int> s_ReusedUniqueIndexedUnorderedArray = UnorderedArray.CreateUnique<int>(capacity: ElementCount);

    public static void Run()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(50, BenchmarkFlags.NoMemoryTest) { TargetTime = TimeSpan.FromMicroseconds(100) }, [
            new MethodWrapper<int>(PopulateAndSumList),
            new MethodWrapper<int>(PopulateAndSumUnorderedArray),
            new MethodWrapper<int>(PopulateAndSumUniqueIndexedUnorderedArray),
        ]);
        Benchmark.RunQuickTest(new BenchmarkOption(50, BenchmarkFlags.NoMemoryTest) { TargetTime = TimeSpan.FromMicroseconds(100) }, [
            new MethodWrapper<int>(RefillAndSumList),
            new MethodWrapper<int>(RefillAndSumUnorderedArray),
            new MethodWrapper<int>(RefillAndSumUniqueIndexedUnorderedArray),
        ]);
        Benchmark.RunQuickTest(new BenchmarkOption(50, BenchmarkFlags.NoMemoryTest) { TargetTime = TimeSpan.FromMicroseconds(100) }, [
            new MethodWrapper<int>(RemoveByValueList),
            new MethodWrapper<int>(RemoveByValueUnorderedArray),
            new MethodWrapper<int>(RemoveByValueUniqueIndexedUnorderedArray),
        ]);
        Benchmark.RunQuickTest(new BenchmarkOption(50, BenchmarkFlags.NoMemoryTest) { TargetTime = TimeSpan.FromMicroseconds(100) }, [
            new MethodWrapper<int>(RemoveMiddleByIndexList),
            new MethodWrapper<int>(RemoveMiddleByIndexUnorderedArray),
            new MethodWrapper<int>(RemoveMiddleByIndexUniqueIndexedUnorderedArray),
        ]);
        Benchmark.RunQuickTest(new BenchmarkOption(50, BenchmarkFlags.NoMemoryTest) { TargetTime = TimeSpan.FromMicroseconds(100) }, [
            new MethodWrapper<int>(ForeachList),
            new MethodWrapper<int>(ForeachUnorderedArray),
            new MethodWrapper<int>(ForeachUniqueIndexedUnorderedArray),
        ]);
    }

    private static int PopulateAndSumList()
    {
        List<int> list = new List<int>(ElementCount);
        for (int i = 0; i < ElementCount; i++) list.Add(i);

        int sum = 0;
        foreach (int item in list) sum += item;
        return sum;
    }

    private static int PopulateAndSumUnorderedArray()
    {
        UnorderedArray<int> array = UnorderedArray.Create<int>(capacity: ElementCount);
        for (int i = 0; i < ElementCount; i++) array.Add(i);

        int sum = 0;
        foreach (int item in array.AsSpan()) sum += item;
        return sum;
    }

    private static int PopulateAndSumUniqueIndexedUnorderedArray()
    {
        UnorderedArray<int> array = UnorderedArray.CreateUnique<int>(capacity: ElementCount);
        for (int i = 0; i < ElementCount; i++) array.Add(i);

        int sum = 0;
        foreach (int item in array.AsSpan()) sum += item;
        return sum;
    }

    private static int RefillAndSumList()
    {
        List<int> list = s_ReusedList;
        list.Clear();
        for (int i = 0; i < ElementCount; i++) list.Add(i);

        int sum = 0;
        foreach (int item in CollectionsMarshal.AsSpan(list)) sum += item;
        return sum;
    }

    private static int RefillAndSumUnorderedArray()
    {
        UnorderedArray<int> array = s_ReusedUnorderedArray;
        array.Clear();
        for (int i = 0; i < ElementCount; i++) array.Add(i);

        int sum = 0;
        foreach (int item in array.AsSpan()) sum += item;
        return sum;
    }

    private static int RefillAndSumUniqueIndexedUnorderedArray()
    {
        UnorderedArray<int> array = s_ReusedUniqueIndexedUnorderedArray;
        array.Clear();
        for (int i = 0; i < ElementCount; i++) array.Add(i);

        int sum = 0;
        foreach (int item in array.AsSpan()) sum += item;
        return sum;
    }

    private static int RemoveByValueList()
    {
        List<int> list = ResetList();
        int checksum = 0;

        foreach (int value in s_RemoveValues)
        {
            checksum += value;
            list.Remove(value);
        }

        return checksum + list.Count;
    }

    private static int RemoveByValueUnorderedArray()
    {
        UnorderedArray<int> array = ResetUnorderedArray();
        int checksum = 0;

        foreach (int value in s_RemoveValues)
        {
            checksum += value;
            array.Remove(value);
        }

        return checksum + array.Count;
    }

    private static int RemoveByValueUniqueIndexedUnorderedArray()
    {
        UnorderedArray<int> array = ResetUniqueIndexedUnorderedArray();
        int checksum = 0;

        foreach (int value in s_RemoveValues)
        {
            checksum += value;
            array.Remove(value);
        }

        return checksum + array.Count;
    }

    private static int RemoveMiddleByIndexList()
    {
        List<int> list = ResetList();
        int checksum = 0;

        while (list.Count > 0)
        {
            int index = list.Count >> 1;
            checksum += list[index];
            list.RemoveAt(index);
        }

        return checksum;
    }

    private static int RemoveMiddleByIndexUnorderedArray()
    {
        UnorderedArray<int> array = ResetUnorderedArray();
        int checksum = 0;

        while (array.Count > 0)
        {
            int index = array.Count >> 1;
            checksum += array[index];
            array.RemoveAt(index);
        }

        return checksum;
    }

    private static int RemoveMiddleByIndexUniqueIndexedUnorderedArray()
    {
        UnorderedArray<int> array = ResetUniqueIndexedUnorderedArray();
        int checksum = 0;

        while (array.Count > 0)
        {
            int index = array.Count >> 1;
            checksum += array[index];
            array.RemoveAt(index);
        }

        return checksum;
    }

    private static int ForeachList()
    {
        List<int> list = ResetList();
        int checksum = 0;

        foreach (int value in CollectionsMarshal.AsSpan(list)) checksum += value;
        return checksum;
    }

    private static int ForeachUnorderedArray()
    {
        UnorderedArray<int> array = ResetUnorderedArray();
        int checksum = 0;

        foreach (int value in array.AsSpan()) checksum += value;
        return checksum;
    }

    private static int ForeachUniqueIndexedUnorderedArray()
    {
        UnorderedArray<int> array = ResetUniqueIndexedUnorderedArray();
        int checksum = 0;

        foreach (int value in array.AsSpan()) checksum += value;
        return checksum;
    }

    private static int[] CreateSourceItems()
    {
        int[] items = new int[ElementCount];
        for (int i = 0; i < ElementCount; i++) items[i] = i;
        return items;
    }

    private static int[] CreateShuffledSourceItems()
    {
        int[] items = CreateSourceItems();
        Random random = new Random(20260424);
        for (int i = items.Length - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }

        return items;
    }

    private static List<int> ResetList()
    {
        s_ReusedList.Clear();
        s_ReusedList.AddRange(s_SourceItems);
        return s_ReusedList;
    }

    private static UnorderedArray<int> ResetUnorderedArray()
    {
        s_ReusedUnorderedArray.Clear();
        s_ReusedUnorderedArray.AddRange(s_SourceItems);
        return s_ReusedUnorderedArray;
    }

    private static UnorderedArray<int> ResetUniqueIndexedUnorderedArray()
    {
        s_ReusedUniqueIndexedUnorderedArray.Clear();
        s_ReusedUniqueIndexedUnorderedArray.AddRange(s_SourceItems);
        return s_ReusedUniqueIndexedUnorderedArray;
    }
}
