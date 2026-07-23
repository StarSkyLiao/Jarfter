namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 提供可增删元素的加权随机选取池.
/// <para>元素变更会在下一次选取前同步到内部别名表.</para>
/// </summary>
public sealed class RandomPool<T>
{
    private AliasPool<T> m_AliasPool;
    private readonly List<(T Item, double Weight)> m_Entries;
    private bool m_VersionChanged;

    /// <summary>
    /// 获取当前选取池中的元素数量.
    /// </summary>
    public int Count => m_Entries.Count;

    /// <summary>
    /// 使用指定元素及权重初始化随机选取池.
    /// </summary>
    /// <param name="entries">初始元素及其对应权重.</param>
    public RandomPool((T item, double weight)[] entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        m_Entries = [.. entries];
        m_AliasPool = new AliasPool<T>(m_Entries);
    }

    /// <summary>
    /// 向选取池添加一个元素及其权重.
    /// </summary>
    /// <param name="item">要添加的元素及其权重.</param>
    /// <exception cref="ArgumentOutOfRangeException">权重不是有限的非负数, 或加入后权重总和不是有限正数.</exception>
    public void Add((T item, double weight) item)
    {
        if (!double.IsFinite(item.weight) || item.weight < 0)
            throw new ArgumentOutOfRangeException(nameof(item), "权重必须是有限的非负数.");
        if (item.weight == 0) return;

        double totalWeight = item.weight;
        foreach ((T Item, double Weight) entry in m_Entries) totalWeight += entry.Weight;
        if (!double.IsFinite(totalWeight) || totalWeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(item), "加入后的权重总和必须是有限正数.");

        m_Entries.Add((item.item, item.weight));
        m_VersionChanged = true;
    }

    /// <summary>
    /// 使用指定元素及权重替换当前选取池的内容.
    /// </summary>
    /// <param name="entries">新的元素及其对应权重.</param>
    /// <exception cref="ArgumentNullException"><paramref name="entries"/> 为 <see langword="null"/>.</exception>
    public void Update(IEnumerable<(T item, double weight)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        List<(T Item, double Weight)> newEntries = [.. entries];
        AliasPool<T> newAliasPool = new AliasPool<T>(newEntries);

        m_Entries.Clear();
        m_Entries.AddRange(newEntries);

        m_AliasPool = newAliasPool;
        m_VersionChanged = false;
    }

    /// <summary>
    /// 判断选取池中是否包含指定元素.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>存在匹配元素时为 <see langword="true"/>; 否则为 <see langword="false"/>.</returns>
    public bool Contains(T item) => m_Entries.Exists(entry => EqualityComparer<T>.Default.Equals(entry.Item, item));

    /// <summary>
    /// 移除选取池中第一个匹配的元素.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>移除了元素时为 <see langword="true"/>; 否则为 <see langword="false"/>.</returns>
    public bool Remove(T item)
    {
        int index = m_Entries.FindIndex(entry => EqualityComparer<T>.Default.Equals(entry.Item, item));
        if (index < 0) return false;

        m_Entries.RemoveAt(index);
        m_VersionChanged = true;
        return true;
    }

    /// <summary>
    /// 按权重随机返回一个元素.
    /// </summary>
    /// <returns>随机选取的元素.</returns>
    /// <exception cref="InvalidOperationException">当前选取池为空.</exception>
    public T GetRandomly()
    {
        UpdateAliasPoolIfNeeded();
        return m_AliasPool.GetRandomly();
    }

    /// <summary>
    /// 使用指定随机源按权重返回一个元素.
    /// </summary>
    /// <param name="randomSource">要使用的随机数来源.</param>
    /// <returns>随机选取的元素.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="randomSource"/> 为 <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">当前选取池为空.</exception>
    public T GetRandomly(IRandomSource randomSource)
    {
        ArgumentNullException.ThrowIfNull(randomSource);
        UpdateAliasPoolIfNeeded();
        return m_AliasPool.GetRandomly(randomSource);
    }

    /// <summary>
    /// 按权重确定性地返回一个元素.
    /// </summary>
    /// <param name="seed">用于选取元素的种子.</param>
    /// <returns>由 <paramref name="seed"/> 决定的元素.</returns>
    /// <exception cref="InvalidOperationException">当前选取池为空.</exception>
    public T GetRandomly(int seed)
    {
        UpdateAliasPoolIfNeeded();
        return m_AliasPool.GetRandomly(seed);
    }

    /// <summary>
    /// 尝试按权重随机返回一个元素.
    /// </summary>
    /// <param name="item">成功时为随机选取的元素; 失败时为默认值.</param>
    /// <returns>当前选取池非空时为 <see langword="true"/>; 否则为 <see langword="false"/>.</returns>
    public bool TryGetRandomly(out T? item)
    {
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = GetRandomly();
        return true;
    }

    /// <summary>
    /// 使用指定随机源尝试按权重返回一个元素.
    /// </summary>
    /// <param name="randomSource">要使用的随机数来源.</param>
    /// <param name="item">成功时为随机选取的元素; 失败时为默认值.</param>
    /// <returns>当前选取池非空时为 <see langword="true"/>; 否则为 <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="randomSource"/> 为 <see langword="null"/>.</exception>
    public bool TryGetRandomly(IRandomSource randomSource, out T? item)
    {
        ArgumentNullException.ThrowIfNull(randomSource);
        if (Count == 0)
        {
            item = default;
            return false;
        }

        item = GetRandomly(randomSource);
        return true;
    }

    /// <summary>
    /// 移除选取池中的全部元素.
    /// </summary>
    public void Clear()
    {
        m_Entries.Clear();
        m_VersionChanged = true;
    }

    private void UpdateAliasPoolIfNeeded()
    {
        if (!m_VersionChanged) return;

        m_AliasPool.Update(m_Entries);
        m_VersionChanged = false;
    }
}
