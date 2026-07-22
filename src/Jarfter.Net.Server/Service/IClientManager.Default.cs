using System.Collections.ObjectModel;
using Jarfter.Net.Server.Collections;
using Jarfter.Net.Server.Connection;

namespace Jarfter.Net.Server.Service;

/// <summary>
/// 默认的用来记录服务器当前的所有客户端连接状态的对象.
/// </summary>
public class DefaultClientManager : IClientManager
{
    private readonly LookupTable<ClientConnectionInfo> m_Connections = new LookupTable<ClientConnectionInfo>();
    private readonly LookupTable<ConcurrentSet<string>> m_Rooms = new LookupTable<ConcurrentSet<string>>();

    /// <inheritdoc />
    public void Connect(string connectionId, string? userIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        m_Connections[connectionId] = new ClientConnectionInfo(connectionId, userIdentifier, DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public void Disconnect(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        if (!m_Connections.TryRemove(connectionId, out ClientConnectionInfo? connection)) return;
        foreach (string roomId in connection.RoomIds.Keys) RemoveConnectionFromRoom(roomId, connectionId);
    }

    /// <inheritdoc />
    public void JoinRoom(string connectionId, string roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

        ClientConnectionInfo connection = m_Connections.GetOrAdd(connectionId, static id =>
            new ClientConnectionInfo(id, null, DateTimeOffset.UtcNow)
        );
        connection.RoomIds.TryAdd(roomId, 0);
        ConcurrentSet<string> roomConnections = m_Rooms.GetOrAdd(roomId, static _ =>
            new ConcurrentSet<string>(StringComparer.Ordinal)
        );
        roomConnections.TryAdd(connectionId, 0);
    }

    /// <inheritdoc />
    public void LeaveRoom(string connectionId, string roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);

        if (m_Connections.TryGetValue(connectionId, out ClientConnectionInfo? connection))
        {
            connection.RoomIds.TryRemove(roomId, out _);
        }

        RemoveConnectionFromRoom(roomId, connectionId);
    }

    /// <inheritdoc />
    public ClientConnectionInfo? GetConnection(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        return m_Connections.GetValueOrDefault(connectionId);
    }

    // ReSharper disable once SuspiciousTypeConversion.Global
    /// <inheritdoc />
    public ReadOnlyCollection<ClientConnectionInfo> Clients() => (ReadOnlyCollection<ClientConnectionInfo>)m_Connections.Values;

    /// <inheritdoc />
    public ReadOnlyCollection<string> ClientsInRoom(string roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        if (!m_Rooms.TryGetValue(roomId, out ConcurrentSet<string>? connections)) return ReadOnlyCollection<string>.Empty;
        return (ReadOnlyCollection<string>)connections.Keys;
    }

    private void RemoveConnectionFromRoom(string roomId, string connectionId)
    {
        if (!m_Rooms.TryGetValue(roomId, out ConcurrentSet<string>? roomConnections)) return;
        roomConnections.TryRemove(connectionId, out _);
        if (!roomConnections.IsEmpty) return;
        m_Rooms.TryRemove(new KeyValuePair<string, ConcurrentSet<string>>(roomId, roomConnections));
    }

}
