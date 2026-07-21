using System.Text.Json;
using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Protocol.SignalR;

namespace Jarfter.Net.Server.Message.Context;


/// <summary>
/// 定义处理 <see cref="t:Jarfter.Net.Protocol.Message.INetRequest`2"/> 的接口.
/// </summary>
/// <typeparam name="TMessage">协议类型.</typeparam>
/// <typeparam name="TResponse">期望的返回值类型.</typeparam>
public interface INetMsgHandler<in TMessage, TResponse> : INetMsgHandler where TMessage : INetRequest<TMessage, TResponse>
{
    /// <summary>
    /// 异步处理指定的 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/>.
    /// </summary>
    public ValueTask<TResponse> HandleAsync(TMessage message, string connectionId, CancellationToken cancellationToken);

    async ValueTask<NetResponse?> INetMsgHandler.HandleResultAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken)
    {
        TMessage? message = envelope.Message.Payload.Deserialize<TMessage>();
        ArgumentNullException.ThrowIfNull(message);
        TResponse response = await HandleAsync(message, envelope.ConnectionId, cancellationToken);
        return new NetResponse(TMessage.MessageName, JsonSerializer.SerializeToElement(response));
    }
}
