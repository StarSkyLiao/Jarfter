using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Protocol.SignalR;
using Jarfter.Net.Server.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Jarfter.Net.Server.Service;

/// <summary>
/// 向已连接的客户端或指定房间中的客户端广播通知.
/// <para>通知方法名必须与客户端通过 <c>NetClient.On</c> 注册的方法名一致.</para>
/// </summary>
/// <param name="hubContext">用于在 Hub 外部向客户端发送消息的上下文.</param>
public sealed class DefaultBroadcaster(IHubContext<NetworkHub> hubContext) : IBroadcaster
{
    /// <inheritdoc />
    public Task BroadcastAsync<TMessage>(INetRequest<TMessage> message, CancellationToken cancellationToken = default)
        where TMessage : INetRequest<TMessage>
    {
        ArgumentNullException.ThrowIfNull(message);
        NetMessage netMessage = new NetMessage(TMessage.MessageName,
            TMessage.SerializeToElement(message), Guid.CreateVersion7()
        );
        return hubContext.Clients.All.SendAsync(TMessage.MessageName, netMessage, cancellationToken);
    }

    /// <inheritdoc />
    public Task BroadcastToRoomAsync<TMessage>(string roomId, INetRequest<TMessage> message,
        CancellationToken cancellationToken = default)
        where TMessage : INetRequest<TMessage>
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentNullException.ThrowIfNull(message);
        NetMessage netMessage = new NetMessage(TMessage.MessageName,
            TMessage.SerializeToElement(message), Guid.CreateVersion7()
        );
        return hubContext.Clients.Group(roomId).SendAsync(TMessage.MessageName, netMessage, cancellationToken);
    }

}
