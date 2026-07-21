using System.Diagnostics;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 默认的<see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/> 处理器.
/// </summary>
public class DefaultNetMsgEnvelopeHandler<TIn>(Func<TIn, NetMsgEnvelope, CancellationToken, ValueTask> handler)
    : INetMsgEnvelopeHandler<TIn>
{
    /// <inheritdoc />
    public virtual ValueTask HandleAsync(TIn message, NetMsgEnvelope envelope, CancellationToken cancellationToken)
    {
        Debug.Assert(message is not null);
        return handler.Invoke(message, envelope, cancellationToken);
    }
}
