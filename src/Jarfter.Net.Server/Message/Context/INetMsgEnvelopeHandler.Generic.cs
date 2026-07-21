using System.Diagnostics;
using System.Text.Json;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 定义处理 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/> 的接口.
/// </summary>
public interface INetMsgEnvelopeHandler<in TIn> : INetMsgEnvelopeHandler
{
    ValueTask INetMsgEnvelopeHandler.HandleAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken)
    {
        TIn? message = envelope.Message.Payload.Deserialize<TIn>();
        Debug.Assert(message is not null);
        return HandleAsync(message, envelope, cancellationToken);
    }

    /// <summary>
    /// 异步处理指定的 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/>.
    /// </summary>
    public ValueTask HandleAsync(TIn message, NetMsgEnvelope envelope, CancellationToken cancellationToken);

}
