namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 提供基于 Alias Method 的加权随机选取池.
/// <para>更新权重表的时间复杂度为 O(n), 单次选取的时间复杂度为 O(1). 空池不能选取元素.</para>
/// </summary>
public sealed class AliasPool<T>
{
    private (double Probability, int Index)[] m_Alias = [];
    private (T Item, double Weight)[] m_ChanceTable = [];

    /// <summary>
    /// 获取当前权重表中的元素数量.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// 使用指定权重表初始化随机选取池.
    /// </summary>
    /// <param name="chanceTable">元素及其对应权重.</param>
    public AliasPool(ICollection<(T item, double weight)> chanceTable)
    {
        ArgumentNullException.ThrowIfNull(chanceTable);
        Update(chanceTable);
    }

    /// <summary>
    /// 使用指定权重表更新选取池.
    /// </summary>
    /// <param name="newChanceTable">元素及其对应权重.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newChanceTable"/> 为 <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">权重不是有限的非负数, 或非空表的权重总和不为正数.</exception>
    public void Update(ICollection<(T item, double weight)> newChanceTable)
    {
        ArgumentNullException.ThrowIfNull(newChanceTable);

        int count = newChanceTable.Count;
        (T Item, double Weight)[] chanceTable = new (T Item, double Weight)[count];

        int index = 0;
        double totalWeight = 0;
        foreach ((T item, double weight) entry in newChanceTable)
        {
            if (!double.IsFinite(entry.weight) || entry.weight < 0)
                throw new ArgumentOutOfRangeException(nameof(newChanceTable), "权重必须是有限的非负数.");

            chanceTable[index++] = (entry.item, entry.weight);
            totalWeight += entry.weight;
        }

        if (count > 0 && (!double.IsFinite(totalWeight) || totalWeight <= 0))
            throw new ArgumentOutOfRangeException(nameof(newChanceTable), "非空权重表的权重总和必须为正数.");

        (double Probability, int Index)[] alias = new (double Probability, int Index)[count];
        if (count == 0)
        {
            CommitUpdate(chanceTable, alias, count);
            return;
        }

        Queue<int> smaller = new Queue<int>(count);
        Queue<int> larger = new Queue<int>(count);

        for (int i = 0; i < count; i++)
        {
            chanceTable[i].Weight = chanceTable[i].Weight / totalWeight * count;
            if (chanceTable[i].Weight < 1) smaller.Enqueue(i);
            else larger.Enqueue(i);
        }

        // 将小于平均权重的元素与大于平均权重的元素配对, 使每个槽位恰好承载一个单位概率.
        while (smaller.Count > 0 && larger.Count > 0)
        {
            int smallIndex = smaller.Dequeue();
            int largeIndex = larger.Dequeue();
            alias[smallIndex] = (chanceTable[smallIndex].Weight, largeIndex);
            chanceTable[largeIndex].Weight = chanceTable[smallIndex].Weight + chanceTable[largeIndex].Weight - 1;

            if (chanceTable[largeIndex].Weight < 1) smaller.Enqueue(largeIndex);
            else larger.Enqueue(largeIndex);
        }

        while (larger.Count > 0) alias[larger.Dequeue()].Probability = 1;
        while (smaller.Count > 0) alias[smaller.Dequeue()].Probability = 1;

        CommitUpdate(chanceTable, alias, count);
    }

    /// <summary>
    /// 按权重随机返回一个元素.
    /// </summary>
    /// <returns>随机选取的元素.</returns>
    /// <exception cref="InvalidOperationException">当前权重表为空.</exception>
    public T GetRandomly()
    {
        EnsureNotEmpty();
        int index = Randomization.Range(0, Count);
        return Randomization.Try(m_Alias[index].Probability) ? m_ChanceTable[index].Item : m_ChanceTable[m_Alias[index].Index].Item;
    }

    /// <summary>
    /// 使用指定随机源按权重返回一个元素.
    /// </summary>
    /// <param name="randomSource">要使用的随机数来源.</param>
    /// <returns>随机选取的元素.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="randomSource"/> 为 <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">当前权重表为空.</exception>
    public T GetRandomly(IRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(randomSource);
        EnsureNotEmpty();

        int index = randomSource.NextInt32(0, Count);
        return randomSource.NextDouble() < m_Alias[index].Probability ? m_ChanceTable[index].Item : m_ChanceTable[m_Alias[index].Index].Item;
    }

    /// <summary>
    /// 按权重确定性地返回一个元素.
    /// </summary>
    /// <param name="seed">用于选取元素的种子.</param>
    /// <returns>由 <paramref name="seed"/> 决定的元素.</returns>
    /// <exception cref="InvalidOperationException">当前权重表为空.</exception>
    public T GetRandomly(int seed)
    {
        EnsureNotEmpty();
        int index = HashRandomUtil.Range(seed, 0, Count);
        return HashRandomUtil.Single(seed, index) < m_Alias[index].Probability ? m_ChanceTable[index].Item : m_ChanceTable[m_Alias[index].Index].Item;
    }

    private void EnsureNotEmpty()
    {
        if (Count == 0) throw new InvalidOperationException("不能从空的随机选取池中获取元素.");
    }

    private void CommitUpdate((T Item, double Weight)[] chanceTable, (double Probability, int Index)[] alias, int count)
    {
        m_ChanceTable = chanceTable;
        m_Alias = alias;
        Count = count;
    }
}
