namespace Jarfter.Core.Delegates;

/// <summary>
/// 提供委托订阅列表的底层存储, 在无订阅、单委托和多委托列表三种状态之间切换.
/// 该类型同时负责阻止调用过程中的订阅变更, 避免遍历期间修改集合导致调用顺序和状态不确定.
/// </summary>
/// <typeparam name="TDelegate">委托类型.</typeparam>
internal struct DelegateCollection<TDelegate> where TDelegate : Delegate
{
    private int m_InvokeDepth;

    /// <summary>
    /// 获取当前订阅存储.
    /// </summary>
    public object? DelegateList { get; private set; }

    /// <summary>
    /// 订阅一个委托.
    /// </summary>
    /// <param name="action">需要订阅的委托.</param>
    public void Subscribe(TDelegate action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ThrowIfInvoking();

        switch (DelegateList)
        {
            case null:
            {
                DelegateList = action;
                return;
            }
            case TDelegate old:
            {
                DelegateList = new List<TDelegate> { old, action };
                return;
            }
            case List<TDelegate> list:
            {
                list.Add(action);
                return;
            }
        }
    }

    /// <summary>
    /// 取消订阅一个委托.
    /// </summary>
    /// <param name="action">需要取消订阅的委托.</param>
    public void Unsubscribe(TDelegate action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ThrowIfInvoking();

        switch (DelegateList)
        {
            case null:
            {
                return;
            }
            case TDelegate old:
            {
                if (old == action)
                {
                    DelegateList = null;
                }

                return;
            }
            case List<TDelegate> list:
            {
                if (!list.Remove(action))
                {
                    return;
                }

                if (list.Count == 1)
                {
                    DelegateList = list[0];
                }

                return;
            }
        }
    }

    /// <summary>
    /// 标记进入调用过程.
    /// </summary>
    public void BeginInvoke() => m_InvokeDepth++;

    /// <summary>
    /// 标记离开调用过程.
    /// </summary>
    public void EndInvoke() => m_InvokeDepth--;

    private readonly void ThrowIfInvoking()
    {
        if (m_InvokeDepth > 0)
        {
            throw new InvalidOperationException("Delegate list cannot be modified while it is invoking delegates.");
        }
    }
}
