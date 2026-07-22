namespace Jarfter.Net.Server.Message;

/// <summary>
/// 表示服务端消息处理程序注册后返回的补丁句柄.
/// </summary>
public readonly struct NetMsgPatcherHandle
{
    internal NetMsgPatcherHandle(string messageType, Guid subscriptionId)
    {
        MessageType = messageType;
        SubscriptionId = subscriptionId;
    }

    internal string? MessageType { get; }

    internal Guid SubscriptionId { get; }

    /// <summary>
    /// 获取当前句柄是否表示一个可用于撤销注册的有效处理程序.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(MessageType) && SubscriptionId != Guid.Empty;
}
