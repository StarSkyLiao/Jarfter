namespace Jarfter.Core.Delegates;

/// <summary>
/// 表示包含六个参数的 <see cref="Func{T1, T2, T3, T4, T5, T6, TResult}"/> 异步委托轻量订阅列表.
/// 适用于需要把同一组六个参数按订阅顺序转发给多个异步回调, 且不允许在回调执行期间修改订阅关系的场景.
/// </summary>
/// <typeparam name="T1">第一个参数的类型.</typeparam>
/// <typeparam name="T2">第二个参数的类型.</typeparam>
/// <typeparam name="T3">第三个参数的类型.</typeparam>
/// <typeparam name="T4">第四个参数的类型.</typeparam>
/// <typeparam name="T5">第五个参数的类型.</typeparam>
/// <typeparam name="T6">第六个参数的类型.</typeparam>
public sealed class TaskList<T1, T2, T3, T4, T5, T6>
{
    private DelegateCollection<Func<T1, T2, T3, T4, T5, T6, ValueTask>> m_Delegates;

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void Subscribe(Func<T1, T2, T3, T4, T5, T6, ValueTask> action) => m_Delegates.Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void Unsubscribe(Func<T1, T2, T3, T4, T5, T6, ValueTask> action) => m_Delegates.Unsubscribe(action);

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void operator +=(Func<T1, T2, T3, T4, T5, T6, ValueTask> action) => Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void operator -=(Func<T1, T2, T3, T4, T5, T6, ValueTask> action) => Unsubscribe(action);

    /// <summary>
    /// 按订阅顺序调用所有委托.
    /// </summary>
    /// <param name="arg1">传入委托的第一个参数.</param>
    /// <param name="arg2">传入委托的第二个参数.</param>
    /// <param name="arg3">传入委托的第三个参数.</param>
    /// <param name="arg4">传入委托的第四个参数.</param>
    /// <param name="arg5">传入委托的第五个参数.</param>
    /// <param name="arg6">传入委托的第六个参数.</param>
    public async ValueTask InvokeAsync(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        object? delegateList = m_Delegates.DelegateList;
        if (delegateList is null) return;

        m_Delegates.BeginInvoke();
        try
        {
            switch (delegateList)
            {
                case Func<T1, T2, T3, T4, T5, T6, ValueTask> action:
                {
                    await action(arg1, arg2, arg3, arg4, arg5, arg6).ConfigureAwait(false);
                    return;
                }
                case List<Func<T1, T2, T3, T4, T5, T6, ValueTask>> list:
                {
                    foreach (Func<T1, T2, T3, T4, T5, T6, ValueTask> action in list)
                    {
                        await action(arg1, arg2, arg3, arg4, arg5, arg6).ConfigureAwait(false);
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
