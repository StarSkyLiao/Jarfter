using System.Text.Json;

namespace Jarfter.Net.Protocol.Message;

/// <summary>
/// 定义一条不带有返回值的请求或者回复.
/// </summary>
public interface INetRequest<TMessage> where TMessage : INetRequest<TMessage>
{
    /// <summary>
    /// 该协议的名称, 用于在信息流中进行标识.
    /// </summary>
    static abstract string MessageName { get; }

    /// <summary>
    /// 将协议序列化为 JsonElement 对象.
    /// </summary>
    static abstract JsonElement SerializeToElement(INetRequest<TMessage> request);
}
