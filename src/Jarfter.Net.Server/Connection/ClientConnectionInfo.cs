using Jarfter.Net.Server.Collections;

namespace Jarfter.Net.Server.Connection;

/// <summary>
/// 表示一个当前连接到服务端的客户端快照.
/// </summary>
/// <param name="ConnectionId">获取 SignalR 连接标识.</param>
/// <param name="UserIdentifier">获取可选的用户标识.</param>
/// <param name="ConnectedAtUtc">获取连接建立的 UTC 时间.</param>
public sealed record ClientConnectionInfo(string ConnectionId, string? UserIdentifier, DateTimeOffset ConnectedAtUtc)
{
    /// <summary>
    /// 该连接当前加入的所有房间集合.
    /// </summary>
    internal ConcurrentSet<string> RoomIds { get; } = new ConcurrentSet<string>(StringComparer.Ordinal);
}
