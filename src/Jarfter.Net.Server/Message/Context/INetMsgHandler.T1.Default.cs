using Jarfter.Net.Protocol.Message;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 提供一个默认的 INetMsgHandler 实现. 实际上你也可以为每一种协议单独写一个实现.
/// </summary>
/// <param name="handler">协议处理函数.</param>
/// <typeparam name="TMessage">协议类型.</typeparam>
public class DefaultNetMsgHandler<TMessage>(Func<TMessage, string, CancellationToken, ValueTask> handler) : INetMsgHandler<TMessage>
    where TMessage : INetRequest<TMessage>
{
    /// <inheritdoc />
    public ValueTask HandleAsync(TMessage message, string connectionId, CancellationToken cancellationToken)
    {
        return handler(message, connectionId, cancellationToken);
    }
}

