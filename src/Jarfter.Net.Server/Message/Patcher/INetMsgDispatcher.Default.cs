using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Protocol.SignalR;
using Jarfter.Net.Server.Message.Context;

namespace Jarfter.Net.Server.Message.Patcher;

/// <summary>
/// 服务端按消息类型分发客户端消息的调度器.
/// </summary>
public class DefaultNetMsgDispatcher : INetMsgDispatcher
{
    private readonly ConcurrentDictionary<Guid, string> m_GuidToName = [];

    private readonly ConcurrentDictionary<string, object> m_Handlers = [];

    /// <inheritdoc />
    public NetMsgPatcherHandle On(string messageType, INetMsgEnvelopeHandler handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(handler);

        if (!m_Handlers.TryAdd(messageType, handler))
        {
            ThrowsForMultiTimeRegistered(messageType);
        }
        Guid subscriptionId = Guid.CreateVersion7();
        m_GuidToName.TryAdd(subscriptionId, messageType);
        return new NetMsgPatcherHandle(messageType, subscriptionId);
    }

    /// <inheritdoc />
    public NetMsgPatcherHandle On<TIn>(string messageType, INetMsgEnvelopeHandler<TIn> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(handler);

        if (!m_Handlers.TryAdd(messageType, handler))
        {
            ThrowsForMultiTimeRegistered(messageType);
        }
        Guid subscriptionId = Guid.CreateVersion7();
        m_GuidToName.TryAdd(subscriptionId, messageType);
        return new NetMsgPatcherHandle(messageType, subscriptionId);
    }

    /// <inheritdoc />
    public NetMsgPatcherHandle On<TMessage>(INetMsgEnvelopeHandler<TMessage> handler) where TMessage : INetMessage<TMessage>
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!m_Handlers.TryAdd(TMessage.MessageName, handler))
        {
            ThrowsForMultiTimeRegistered(TMessage.MessageName);
        }
        Guid subscriptionId = Guid.CreateVersion7();
        m_GuidToName.TryAdd(subscriptionId, TMessage.MessageName);
        return new NetMsgPatcherHandle(TMessage.MessageName, subscriptionId);
    }

    /// <inheritdoc />
    public NetMsgPatcherHandle On(string messageType, INetMsgEnvelopeRepliedHandler handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(handler);

        if (!m_Handlers.TryAdd(messageType, handler))
        {
            ThrowsForMultiTimeRegistered(messageType);
        }
        Guid subscriptionId = Guid.CreateVersion7();
        m_GuidToName.TryAdd(subscriptionId, messageType);
        return new NetMsgPatcherHandle(messageType, subscriptionId);
    }

    /// <inheritdoc />
    public NetMsgPatcherHandle On<TIn>(string messageType, INetMsgEnvelopeRepliedHandler<TIn> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
        ArgumentNullException.ThrowIfNull(handler);

        if (!m_Handlers.TryAdd(messageType, handler))
        {
            ThrowsForMultiTimeRegistered(messageType);
        }
        Guid subscriptionId = Guid.CreateVersion7();
        m_GuidToName.TryAdd(subscriptionId, messageType);
        return new NetMsgPatcherHandle(messageType, subscriptionId);
    }

    /// <inheritdoc />
    public NetMsgPatcherHandle On<TMessage>(INetMsgEnvelopeRepliedHandler<TMessage> handler) where TMessage : INetMessage<TMessage>
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!m_Handlers.TryAdd(TMessage.MessageName, handler))
        {
            ThrowsForMultiTimeRegistered(TMessage.MessageName);
        }
        Guid subscriptionId = Guid.CreateVersion7();
        m_GuidToName.TryAdd(subscriptionId, TMessage.MessageName);
        return new NetMsgPatcherHandle(TMessage.MessageName, subscriptionId);
    }

    /// <inheritdoc />
    public bool Off(NetMsgPatcherHandle handle)
    {
        if (!handle.IsValid) return false;
        if (!m_GuidToName.TryRemove(handle.SubscriptionId, out string? messageType)) return false;
        if (handle.MessageType != messageType) ThrowsForInvalidPatcherHandle(handle, messageType);
        return m_Handlers.TryRemove(messageType, out object? _);
    }

    /// <inheritdoc />
    public async ValueTask<NetResponse?> DispatchAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (!m_Handlers.TryGetValue(envelope.Message.Name, out object? handlers)) return null;

        switch (handlers)
        {
            case INetMsgEnvelopeHandler envelopeHandler:
            {
                await envelopeHandler.HandleAsync(envelope, cancellationToken);
                return null;
            }
            case INetMsgEnvelopeRepliedHandler repliedHandler:
            {
                return await repliedHandler.HandleAsync(envelope, cancellationToken);
            }
            default: throw new UnreachableException();
        }
    }

    [DoesNotReturn]
    private static void ThrowsForMultiTimeRegistered(string messageType)
    {
        throw new DuplicateNameException($"The given message type {messageType} is registered multi-times.");
    }

    [DoesNotReturn]
    private static void ThrowsForInvalidPatcherHandle(NetMsgPatcherHandle handle, string messageType)
    {
        throw new NotSupportedException($"The given NetMsgPatcherHandle records type {handle.MessageType} other than {messageType}.");
    }

}
