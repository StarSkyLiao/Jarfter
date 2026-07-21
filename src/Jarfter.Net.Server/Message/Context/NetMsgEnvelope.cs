using Jarfter.Net.Protocol.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 表示服务端处理一条客户端消息时的上下文信封.
/// </summary>
public sealed record NetMsgEnvelope(NetMessage Message, string ConnectionId);
