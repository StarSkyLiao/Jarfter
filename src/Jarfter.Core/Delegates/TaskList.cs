namespace Jarfter.Core.Delegates;

/// <summary>
/// 表示无参数 <see cref="Func{TResult}"/> 异步委托的轻量订阅列表.
/// 适用于需要按订阅顺序广播回调, 且不允许在回调执行期间修改订阅关系的场景.
/// </summary>
public sealed class TaskList
{
    private DelegateCollection<Func<ValueTask>> m_Delegates;

    /// <summary>
    /// 获取当前委托链的委托数量.
    /// </summary>
    public int Count => m_Delegates.Count;

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void Subscribe(Func<ValueTask> action) => m_Delegates.Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void Unsubscribe(Func<ValueTask> action) => m_Delegates.Unsubscribe(action);

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void operator +=(Func<ValueTask> action) => Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void operator -=(Func<ValueTask> action) => Unsubscribe(action);

    /// <summary>
    /// 按订阅顺序调用所有委托.
    /// </summary>
    public async ValueTask InvokeAsync()
    {
        object? delegateList = m_Delegates.DelegateList;
        if (delegateList is null) return;

        m_Delegates.BeginInvoke();
        try
        {
            switch (delegateList)
            {
                case Func<ValueTask> action:
                {
                    await action().ConfigureAwait(false);
                    return;
                }
                case List<Func<ValueTask>> list:
                {
                    foreach (Func<ValueTask> action in list)
                    {
                        await action().ConfigureAwait(false);
                    }

                    return;
                }
            }
        }
        finally
        {
            m_Delegates.EndInvoke();
        }
    }
}
