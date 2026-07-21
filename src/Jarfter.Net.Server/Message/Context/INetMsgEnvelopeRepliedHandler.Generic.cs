using System.Diagnostics;
using System.Text.Json;
using Jarfter.Net.Protocol.SignalR;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 定义处理 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/> 的接口.
/// </summary>
public interface INetMsgEnvelopeRepliedHandler<in TIn> : INetMsgEnvelopeRepliedHandler
{
    ValueTask<NetResponse> INetMsgEnvelopeRepliedHandler.HandleAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken)
    {
        TIn? message = envelope.Message.Payload.Deserialize<TIn>();
        Debug.Assert(message is not null);
        return HandleAsync(message, envelope, cancellationToken);
    }

    /// <summary>
    /// 异步处理指定的 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/>.
    /// </summary>
    public ValueTask<NetResponse> HandleAsync(TIn message, NetMsgEnvelope envelope, CancellationToken cancellationToken);

}
