using Jarfter.Net.Protocol.SignalR;

namespace Jarfter.Net.Server.Message;

/// <summary>
/// 定义处理一条网络协议的接口.
/// </summary>
public interface INetMsgHandler
{
    internal ValueTask<NetResponse?> HandleResultAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken);
}
