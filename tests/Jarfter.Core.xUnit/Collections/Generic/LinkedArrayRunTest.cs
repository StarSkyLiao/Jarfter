using Jarfter.Core.Collections.Generic;
using Jarfter.Core.Diagnostics;

namespace Jarfter.Core.xUnit.Collections.Generic;

public static class LinkedArrayRunTest
{
    private const int ElementCount = 512;

    public static void Run()
    {
        Benchmark.RunQuickTest(new BenchmarkOption(50) { LoopCount = 200 }, [
            new MethodWrapper<int>(TailInsertLinkedArray),
            new MethodWrapper<int>(TailInsertList),
            new MethodWrapper<int>(TailInsertLinkedList),
            new MethodWrapper<int>(HeadRemoveLinkedArray),
            new MethodWrapper<int>(HeadRemoveList),
            new MethodWrapper<int>(HeadRemoveLinkedList),
            new MethodWrapper<int>(TailRemoveLinkedArray),
            new MethodWrapper<int>(TailRemoveList),
            new MethodWrapper<int>(TailRemoveLinkedList),
            new MethodWrapper<int>(ForeachLinkedArray),
            new MethodWrapper<int>(ForeachList),
            new MethodWrapper<int>(ForeachLinkedList),
        ]);
    }

    private static int TailInsertLinkedArray()
    {
        LinkedArray<int> list = new LinkedArray<int>();
        for (int i = 0; i < ElementCount; i++) list.AddLast(i);
        return list.Count;
    }

    private static int TailInsertList()
    {
        List<int> list = new List<int>();
        for (int i = 0; i < ElementCount; i++) list.Add(i);
        return list.Count;
    }

    private static int TailInsertLinkedList()
    {
        LinkedList<int> list = new LinkedList<int>();
        for (int i = 0; i < ElementCount; i++) list.AddLast(i);
        return list.Count;
    }

    private static int HeadRemoveLinkedArray()
    {
        LinkedArray<int> list = CreateLinkedArray();
        int checksum = 0;

        while (list.TryDequeue(out int value)) checksum += value;
        return checksum;
    }

    private static int HeadRemoveList()
    {
        List<int> list = CreateList();
        int checksum = 0;

        while (list.Count > 0)
        {
            checksum += list[0];
            list.RemoveAt(0);
        }

        return checksum;
    }

    private static int HeadRemoveLinkedList()
    {
        LinkedList<int> list = CreateLinkedList();
        int checksum = 0;

        while (list.First is not null)
        {
            checksum += list.First.Value;
            list.RemoveFirst();
        }

        return checksum;
    }

    private static int TailRemoveLinkedArray()
    {
        LinkedArray<int> list = CreateLinkedArray();
        int checksum = 0;

        while (list.Last is { } node)
        {
            checksum += node.Value;
            list.RemoveLast();
        }

        return checksum;
    }

    private static int TailRemoveList()
    {
        List<int> list = CreateList();
        int checksum = 0;

        while (list.Count > 0)
        {
            int lastIndex = list.Count - 1;
            checksum += list[lastIndex];
            list.RemoveAt(lastIndex);
        }

        return checksum;
    }

    private static int TailRemoveLinkedList()
    {
        LinkedList<int> list = CreateLinkedList();
        int checksum = 0;

        while (list.Last is not null)
        {
            checksum += list.Last.Value;
            list.RemoveLast();
        }

        return checksum;
    }

    private static int ForeachLinkedArray()
    {
        LinkedArray<int> list = CreateLinkedArray();
        int checksum = 0;

        foreach (int value in list) checksum += value;
        return checksum;
    }

    private static int ForeachList()
    {
        List<int> list = CreateList();
        int checksum = 0;

        foreach (int value in list) checksum += value;
        return checksum;
    }

    private static int ForeachLinkedList()
    {
        LinkedList<int> list = CreateLinkedList();
        int checksum = 0;

        foreach (int value in list) checksum += value;
        return checksum;
    }

    private static LinkedArray<int> CreateLinkedArray()
    {
        LinkedArray<int> list = new LinkedArray<int>(ElementCount);
        for (int i = 0; i < ElementCount; i++) list.AddLast(i);
        return list;
    }

    private static List<int> CreateList()
    {
        List<int> list = new List<int>(ElementCount);
        for (int i = 0; i < ElementCount; i++) list.Add(i);
        return list;
    }

    private static LinkedList<int> CreateLinkedList()
    {
        LinkedList<int> list = new LinkedList<int>();
        for (int i = 0; i < ElementCount; i++) list.AddLast(i);
        return list;
    }
}
