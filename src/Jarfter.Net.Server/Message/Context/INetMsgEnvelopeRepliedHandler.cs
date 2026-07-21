using Jarfter.Net.Protocol.SignalR;

namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 定义处理 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/> 并回复的接口.
/// </summary>
public interface INetMsgEnvelopeRepliedHandler
{

    /// <summary>
    /// 异步处理指定的 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/>.
    /// </summary>
    public ValueTask<NetResponse> HandleAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken);

}
