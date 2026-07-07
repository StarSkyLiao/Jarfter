namespace Jarfter.Core.Delegates;

/// <summary>
/// 表示包含一个参数的 <see cref="Func{T, TResult}"/> 异步委托轻量订阅列表.
/// 适用于需要把同一个参数按订阅顺序转发给多个异步回调, 且不允许在回调执行期间修改订阅关系的场景.
/// </summary>
/// <typeparam name="T1">第一个参数的类型.</typeparam>
public sealed class TaskList<T1>
{
    private DelegateCollection<Func<T1, ValueTask>> m_Delegates;

    /// <summary>
    /// 获取当前委托链的委托数量.
    /// </summary>
    public int Count => m_Delegates.Count;

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void Subscribe(Func<T1, ValueTask> action) => m_Delegates.Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void Unsubscribe(Func<T1, ValueTask> action) => m_Delegates.Unsubscribe(action);

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void operator +=(Func<T1, ValueTask> action) => Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void operator -=(Func<T1, ValueTask> action) => Unsubscribe(action);

    /// <summary>
    /// 按订阅顺序调用所有委托.
    /// </summary>
    /// <param name="arg1">传入委托的第一个参数.</param>
    public async ValueTask InvokeAsync(T1 arg1)
    {
        object? delegateList = m_Delegates.DelegateList;
        if (delegateList is null) return;

        m_Delegates.BeginInvoke();
        try
        {
            switch (delegateList)
            {
                case Func<T1, ValueTask> action:
                {
                    await action(arg1).ConfigureAwait(false);
                    return;
                }
                case List<Func<T1, ValueTask>> list:
                {
                    foreach (Func<T1, ValueTask> action in list)
                    {
                        await action(arg1).ConfigureAwait(false);
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
