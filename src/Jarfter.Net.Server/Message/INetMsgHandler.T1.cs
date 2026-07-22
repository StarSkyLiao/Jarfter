using System.Text.Json;
using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Protocol.SignalR;

namespace Jarfter.Net.Server.Message;

/// <summary>
/// 定义处理 <see cref="t:Jarfter.Net.Protocol.Message.INetMessage`1"/> 的接口.
/// </summary>
/// <typeparam name="TMessage">协议类型.</typeparam>
public interface INetMsgHandler<in TMessage> : INetMsgHandler where TMessage : INetRequest<TMessage>
{
    /// <summary>
    /// 异步处理指定的 <see cref="t:NetMsgEnvelope"/>.
    /// </summary>
    public ValueTask HandleAsync(TMessage message, string connectionId, CancellationToken cancellationToken);

    async ValueTask<NetResponse?> INetMsgHandler.HandleResultAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken)
    {
        TMessage? message = envelope.Message.Payload.Deserialize<TMessage>();
        ArgumentNullException.ThrowIfNull(message);
        await HandleAsync(message, envelope.ConnectionId, cancellationToken);
        return null;
    }
}
