using Jarfter.Net.Protocol.SignalR;
using Jarfter.Net.Server.Connection;
using Jarfter.Net.Server.Message.Context;
using Jarfter.Net.Server.Message.Patcher;
using Microsoft.AspNetCore.SignalR;

namespace Jarfter.Net.Server.SignalR;

/// <summary>
/// 提供房间通信与客户端消息提交的 SignalR Hub.
/// </summary>
/// <param name="clientManager">服务端连接注册表.</param>
/// <param name="dispatcher">服务端消息分发器.</param>
public sealed class NetworkHub(IClientManager clientManager, INetMsgDispatcher dispatcher) : Hub
{
    /// <summary>
    /// 记录客户端连接建立.
    /// </summary>
    /// <returns>表示异步连接过程的任务.</returns>
    public override Task OnConnectedAsync()
    {
        clientManager.Connect(Context.ConnectionId, Context.UserIdentifier);
        return base.OnConnectedAsync();
    }

    /// <summary>
    /// 记录客户端连接断开.
    /// </summary>
    /// <param name="exception">导致连接断开的异常.</param>
    /// <returns>表示异步断开过程的任务.</returns>
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        clientManager.Disconnect(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
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
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId, cancellationToken);
        clientManager.JoinRoom(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync(HubMethods.ClientJoined, Context.ConnectionId, cancellationToken);
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
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId, cancellationToken);
        clientManager.LeaveRoom(Context.ConnectionId, roomId);
        await Clients.Group(roomId).SendAsync(HubMethods.ClientLeft, Context.ConnectionId, cancellationToken);
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
