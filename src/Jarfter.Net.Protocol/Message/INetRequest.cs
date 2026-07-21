using System.Text.Json;

namespace Jarfter.Net.Protocol.Message;

/// <summary>
/// 定义一条不带有返回值的请求或者回复.
/// </summary>
public interface INetMessage<TMessage> where TMessage : INetMessage<TMessage>
{
    /// <summary>
    /// 该协议的名称, 用于在信息流中进行标识.
    /// </summary>
    static abstract string MessageName { get; }

    /// <summary>
    /// 将协议序列化为 JsonElement 对象.
    /// </summary>
    static abstract JsonElement SerializeToElement(INetMessage<TMessage> message);
}

/// <summary>
/// 定义一条带有返回值的请求.
/// </summary>
/// <typeparam name="TMessage">协议类型本身.</typeparam>
/// <typeparam name="TResponse">期望的返回值类型.</typeparam>
public interface INetRequest<TMessage, TResponse> : INetMessage<TMessage> where TMessage : INetMessage<TMessage>;
