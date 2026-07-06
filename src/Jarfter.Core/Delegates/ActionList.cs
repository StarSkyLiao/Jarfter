namespace Jarfter.Core.Delegates;

/// <summary>
/// 表示无参数 <see cref="Action"/> 委托的轻量订阅列表.
/// 适用于需要按订阅顺序广播回调, 且不允许在回调执行期间修改订阅关系的场景.
/// </summary>
public sealed class ActionList
{
    private DelegateCollection<Action> m_Delegates;

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void Subscribe(Action action) => m_Delegates.Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void Unsubscribe(Action action) => m_Delegates.Unsubscribe(action);

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void operator +=(Action action) => Subscribe(action);

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void operator -=(Action action) => Unsubscribe(action);

    /// <summary>
    /// 按订阅顺序调用所有委托.
    /// </summary>
    public void Invoke()
    {
        object? delegateList = m_Delegates.DelegateList;
        if (delegateList is null) return;

        m_Delegates.BeginInvoke();
        try
        {
            switch (delegateList)
            {
                case Action action:
                {
                    action();
                    return;
                }
                case List<Action> list:
                {
                    foreach (Action action in list)
                    {
                        action();
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
