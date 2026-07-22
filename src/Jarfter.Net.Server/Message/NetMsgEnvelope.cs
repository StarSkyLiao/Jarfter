using Jarfter.Net.Protocol.SignalR;

namespace Jarfter.Net.Server.Message;

/// <summary>
/// 表示服务端处理一条客户端消息时的上下文信封.
/// </summary>
public sealed record NetMsgEnvelope(NetMessage Message, string ConnectionId);
