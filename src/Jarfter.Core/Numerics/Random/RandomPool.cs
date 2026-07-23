namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 提供可增删元素的加权随机选取池.
/// <para>元素变更会在下一次选取前同步到内部别名表.</para>
/// </summary>
public sealed class RandomPool<T>
{
    private readonly AliasPool<T> m_AliasPool;
    private readonly List<(T Item, double Weight)> m_Entries;
    private bool m_VersionChanged;

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
    public void Add((T item, double weight) item)
    {
        m_Entries.Add((item.item, item.weight));
        m_VersionChanged = true;
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
