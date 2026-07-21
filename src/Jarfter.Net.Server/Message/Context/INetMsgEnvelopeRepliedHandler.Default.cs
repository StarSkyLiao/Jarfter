using System.Diagnostics;
using Jarfter.Net.Protocol.SignalR;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 默认的<see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/> 处理器.
/// </summary>
public class DefaultNetMsgEnvelopeRepliedHandler(Func<NetMsgEnvelope, CancellationToken, ValueTask<NetResponse>> handler)
    : INetMsgEnvelopeRepliedHandler
{
    /// <inheritdoc />
    public virtual ValueTask<NetResponse> HandleAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken)
    {
        Debug.Assert(envelope.Message is not null);
        return handler.Invoke(envelope, cancellationToken);
    }
}
