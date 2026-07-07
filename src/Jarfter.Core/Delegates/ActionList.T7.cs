namespace Jarfter.Core.Delegates;

/// <summary>
/// 表示包含七个参数的 <see cref="Action{T1,T2,T3,T4,T5,T6,T7}"/> 委托轻量订阅列表.
/// 适用于需要把同一组七个参数按订阅顺序转发给多个回调, 且不允许在回调执行期间修改订阅关系的场景.
/// </summary>
/// <typeparam name="T1">第一个参数的类型.</typeparam>
/// <typeparam name="T2">第二个参数的类型.</typeparam>
/// <typeparam name="T3">第三个参数的类型.</typeparam>
/// <typeparam name="T4">第四个参数的类型.</typeparam>
/// <typeparam name="T5">第五个参数的类型.</typeparam>
/// <typeparam name="T6">第六个参数的类型.</typeparam>
/// <typeparam name="T7">第七个参数的类型.</typeparam>
public sealed class ActionList<T1, T2, T3, T4, T5, T6, T7>
{
    private DelegateCollection<Action<T1, T2, T3, T4, T5, T6, T7>> m_Delegates;

    /// <summary>
    /// 获取当前委托链的委托数量.
    /// </summary>
    public int Count => m_Delegates.Count;

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void Subscribe(Action<T1, T2, T3, T4, T5, T6, T7> action) => m_Delegates.Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void Unsubscribe(Action<T1, T2, T3, T4, T5, T6, T7> action) => m_Delegates.Unsubscribe(action);

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void operator +=(Action<T1, T2, T3, T4, T5, T6, T7> action) => Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void operator -=(Action<T1, T2, T3, T4, T5, T6, T7> action) => Unsubscribe(action);

    /// <summary>
    /// 按订阅顺序调用所有委托.
    /// </summary>
    /// <param name="arg1">传入委托的第一个参数.</param>
    /// <param name="arg2">传入委托的第二个参数.</param>
    /// <param name="arg3">传入委托的第三个参数.</param>
    /// <param name="arg4">传入委托的第四个参数.</param>
    /// <param name="arg5">传入委托的第五个参数.</param>
    /// <param name="arg6">传入委托的第六个参数.</param>
    /// <param name="arg7">传入委托的第七个参数.</param>
    public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
        object? delegateList = m_Delegates.DelegateList;
        if (delegateList is null) return;

        m_Delegates.BeginInvoke();
        try
        {
            switch (delegateList)
            {
                case Action<T1, T2, T3, T4, T5, T6, T7> action:
                {
                    action(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
                    return;
                }
                case List<Action<T1, T2, T3, T4, T5, T6, T7>> list:
                {
                    foreach (Action<T1, T2, T3, T4, T5, T6, T7> action in list)
                    {
                        action(arg1, arg2, arg3, arg4, arg5, arg6, arg7);
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
