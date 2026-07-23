using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Protocol.SignalR;
using Jarfter.Net.Server.Message;
using Jarfter.Net.Server.Service;
using Microsoft.AspNetCore.SignalR;

namespace Jarfter.Net.Server.SignalR;

/// <summary>
/// 提供房间通信与客户端消息提交的 SignalR Hub.
/// </summary>
/// <param name="clientManager">服务端连接注册表.</param>
/// <param name="dispatcher">服务端消息分发器.</param>
/// <param name="broadcaster">消息广播器.</param>
public sealed class NetworkHub(IClientManager clientManager, INetMsgDispatcher dispatcher, IBroadcaster broadcaster) : Hub
{
    /// <summary>
    /// 记录客户端连接建立.
    /// </summary>
    /// <returns>表示异步连接过程的任务.</returns>
    public override async Task OnConnectedAsync()
    {
        CancellationToken cancellationToken = Context.ConnectionAborted;
        string connectionId = Context.ConnectionId;
        clientManager.Connect(connectionId, Context.UserIdentifier);
        await broadcaster.BroadcastAsync(new InternalMessage.ClientJoinedNtf(connectionId), cancellationToken);
    }

    /// <summary>
    /// 记录客户端连接断开.
    /// </summary>
    /// <param name="exception">导致连接断开的异常.</param>
    /// <returns>表示异步断开过程的任务.</returns>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        CancellationToken cancellationToken = Context.ConnectionAborted;
        string connectionId = Context.ConnectionId;
        clientManager.Disconnect(connectionId);
        await broadcaster.BroadcastAsync(new InternalMessage.ClientLeftNtf(connectionId), cancellationToken);
    }

    /// <summary>
    /// 将当前连接加入指定游戏房间.
    /// </summary>
    /// <param name="roomId">要加入的房间标识.</param>
    /// <returns>表示异步加入过程的任务.</returns>
    [HubMethodName(HubMethods.JoinRoom)]
    public async Task JoinRoomAsync(string roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

        CancellationToken cancellationToken = Context.ConnectionAborted;
        string connectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(connectionId, roomId, cancellationToken);
        clientManager.JoinRoom(connectionId, roomId);
        await broadcaster.BroadcastToRoomAsync(roomId, new InternalMessage.JoinRoomNtf(connectionId), cancellationToken);
    }

    /// <summary>
    /// 将当前连接移出指定游戏房间.
    /// </summary>
    /// <param name="roomId">要离开的房间标识.</param>
    /// <returns>表示异步离开过程的任务.</returns>
    [HubMethodName(HubMethods.LeaveRoom)]
    public async Task LeaveRoomAsync(string roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

        CancellationToken cancellationToken = Context.ConnectionAborted;
        string connectionId = Context.ConnectionId;
        await Groups.RemoveFromGroupAsync(connectionId, roomId, cancellationToken);
        clientManager.LeaveRoom(connectionId, roomId);
        await broadcaster.BroadcastToRoomAsync(roomId, new InternalMessage.LeaveRoomNtf(connectionId), cancellationToken);
    }

    /// <summary>
    /// 将客户端提交的房间消息交给服务端消息分发器.
    /// </summary>
    /// <param name="message">客户端提交的消息包.</param>
    /// <returns>表示异步提交过程的任务.</returns>
    [HubMethodName(HubMethods.SendMsg)]
    public async Task<NetResponse?> SendMessageAsync(NetMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        CancellationToken cancellationToken = Context.ConnectionAborted;
        NetMsgEnvelope context = new NetMsgEnvelope(message, Context.ConnectionId);

        return await dispatcher.DispatchAsync(context, cancellationToken);
    }
}
