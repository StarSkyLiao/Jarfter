using Jarfter.Net.Protocol.Message;

namespace Jarfter.Net.Server.Service;

/// <summary>
/// 定义一个用户广播到所有用户的服务.
/// </summary>
public interface IBroadcaster
{
    /// <summary>
    /// 向所有已连接的客户端广播不带参数的通知.
    /// </summary>
    /// <param name="message">要通知的协议内容.</param>
    /// <param name="cancellationToken">用于取消发送操作的令牌.</param>
    /// <returns>表示异步广播过程的任务.</returns>
    Task BroadcastAsync<TMessage>(INetRequest<TMessage> message, CancellationToken cancellationToken = default)
        where TMessage : INetRequest<TMessage>;

    /// <summary>
    /// 向指定房间中的所有客户端广播不带参数的通知.
    /// </summary>
    /// <param name="roomId">目标房间标识.</param>
    /// <param name="message">要通知的协议内容.</param>
    /// <param name="cancellationToken">用于取消发送操作的令牌.</param>
    /// <returns>表示异步广播过程的任务.</returns>
    Task BroadcastToRoomAsync<TMessage>(string roomId, INetRequest<TMessage> message,
        CancellationToken cancellationToken = default)
        where TMessage : INetRequest<TMessage>;

}
