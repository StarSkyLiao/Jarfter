using System.Diagnostics;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 默认的<see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/> 处理器.
/// </summary>
public class DefaultNetMsgEnvelopeHandler(Func<NetMsgEnvelope, CancellationToken, ValueTask> handler)
    : INetMsgEnvelopeHandler
{
    /// <inheritdoc />
    public virtual ValueTask HandleAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken)
    {
        Debug.Assert(envelope.Message is not null);
        return handler.Invoke(envelope, cancellationToken);
    }
}
