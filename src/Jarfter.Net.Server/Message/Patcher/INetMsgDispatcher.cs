using Jarfter.Net.Protocol.SignalR;
using Jarfter.Net.Server.Message.Context;

namespace Jarfter.Net.Server.Message.Patcher;

/// <summary>
/// 服务端按消息类型分发客户端消息的调度器.
/// </summary>
public interface INetMsgDispatcher
{
    /// <summary>
    /// 为指定消息类型注册带反序列化负载的服务端处理程序.
    /// </summary>
    /// <param name="messageType">消息类型.</param>
    /// <param name="handler">处理程序.</param>
    /// <returns>可用于撤销注册的处理程序句柄.</returns>
    NetMsgPatcherHandle On(string messageType, INetMsgEnvelopeHandler handler);

    /// <summary>
    /// 为指定消息类型注册带反序列化负载的服务端处理程序.
    /// </summary>
    /// <typeparam name="TIn">消息负载类型.</typeparam>
    /// <param name="messageType">消息类型.</param>
    /// <param name="handler">处理程序.</param>
    /// <returns>可用于撤销注册的处理程序句柄.</returns>
    NetMsgPatcherHandle On<TIn>(string messageType, INetMsgEnvelopeHandler<TIn> handler);

    /// <summary>
    /// 为指定消息类型注册带反序列化负载的服务端处理程序.
    /// 处理完成后, 需要一个返回值.
    /// </summary>
    /// <param name="messageType">消息类型.</param>
    /// <param name="handler">处理程序.</param>
    /// <returns>可用于撤销注册的处理程序句柄.</returns>
    NetMsgPatcherHandle On(string messageType, INetMsgEnvelopeRepliedHandler handler);

    /// <summary>
    /// 为指定消息类型注册带反序列化负载的服务端处理程序.
    /// 处理完成后, 需要一个返回值.
    /// </summary>
    /// <typeparam name="TIn">消息负载类型.</typeparam>
    /// <param name="messageType">消息类型.</param>
    /// <param name="handler">处理程序.</param>
    /// <returns>可用于撤销注册的处理程序句柄.</returns>
    NetMsgPatcherHandle On<TIn>(string messageType, INetMsgEnvelopeRepliedHandler<TIn> handler);

    /// <summary>
    /// 撤销指定句柄对应的服务端处理程序注册.
    /// </summary>
    /// <param name="handle">处理程序注册句柄.</param>
    /// <returns>成功撤销注册时返回 true; 句柄无效或注册不存在时返回 false.</returns>
    bool Off(NetMsgPatcherHandle handle);

    /// <summary>
    /// 分发客户端提交的消息, 并根据需要回复消息.
    /// </summary>
    /// <param name="envelope">消息处理上下文.</param>
    /// <param name="cancellationToken">用于取消异步分发的令牌.</param>
    /// <returns>表示异步分发过程的任务. 如果需要进行回复, 则会返回一个 NetResponse 对象; 否则, 返回 null.</returns>
    ValueTask<NetResponse?> DispatchAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken = default);

}
