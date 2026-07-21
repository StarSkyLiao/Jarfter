using Jarfter.Net.Protocol.Message;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 提供一个默认的 INetMsgHandler 实现. 实际上你也可以为每一种协议单独写一个实现.
/// </summary>
/// <param name="handler">协议处理函数.</param>
/// <typeparam name="TMessage">协议类型.</typeparam>
/// <typeparam name="TResponse">协议回复类型.</typeparam>
public class DefaultNetMsgHandler<TMessage, TResponse>(Func<TMessage, string, CancellationToken, ValueTask<TResponse>> handler)
    : INetMsgHandler<TMessage, TResponse> where TMessage : INetRequest<TMessage, TResponse>
{
    /// <inheritdoc />
    public ValueTask<TResponse> HandleAsync(TMessage message, string connectionId, CancellationToken cancellationToken)
    {
        return handler(message, connectionId, cancellationToken);
    }
}

