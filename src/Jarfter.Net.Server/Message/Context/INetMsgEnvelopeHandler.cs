namespace Jarfter.Net.Server.Message.Context;

/// <summary>
/// 定义处理 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/> 的接口.
/// </summary>
public interface INetMsgEnvelopeHandler
{
    /// <summary>
    /// 异步处理指定的 <see cref="t:Jarfter.Net.Server.Message.Context.NetMsgEnvelope"/>.
    /// </summary>
    public ValueTask HandleAsync(NetMsgEnvelope envelope, CancellationToken cancellationToken);

}
