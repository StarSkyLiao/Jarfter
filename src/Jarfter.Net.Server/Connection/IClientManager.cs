using System.Collections.ObjectModel;

namespace Jarfter.Net.Server.Connection;

/// <summary>
/// 用来记录服务器当前的所有客户端连接状态.
/// </summary>
public interface IClientManager
{
    /// <summary>
    /// 记录客户端连接已建立.
    /// </summary>
    /// <param name="connectionId">SignalR 连接标识.</param>
    /// <param name="userIdentifier">可选的用户标识.</param>
    void Connect(string connectionId, string? userIdentifier);

    /// <summary>
    /// 记录客户端连接已断开.
    /// </summary>
    /// <param name="connectionId">SignalR 连接标识.</param>
    void Disconnect(string connectionId);

    /// <summary>
    /// 记录客户端连接加入指定房间.
    /// </summary>
    /// <param name="connectionId">SignalR 连接标识.</param>
    /// <param name="roomId">房间标识.</param>
    void JoinRoom(string connectionId, string roomId);

    /// <summary>
    /// 记录客户端连接离开指定房间.
    /// </summary>
    /// <param name="connectionId">SignalR 连接标识.</param>
    /// <param name="roomId">房间标识.</param>
    void LeaveRoom(string connectionId, string roomId);

    /// <summary>
    /// 尝试获取指定连接的快照.
    /// </summary>
    /// <param name="connectionId">SignalR 连接标识.</param>
    /// <returns>找到连接时返回连接快照; 否则返回 null.</returns>
    ClientConnectionInfo? GetConnection(string connectionId);

    /// <summary>
    /// 获取所有当前连接的快照.
    /// </summary>
    /// <returns>当前连接快照集合.</returns>
    ReadOnlyCollection<ClientConnectionInfo> Clients();

    /// <summary>
    /// 获取指定房间中的连接标识集合.
    /// </summary>
    /// <param name="roomId">房间标识.</param>
    /// <returns>房间中的连接标识集合.</returns>
    ReadOnlyCollection<string> ClientsInRoom(string roomId);
}
