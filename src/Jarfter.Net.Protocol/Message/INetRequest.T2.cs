using System.Text.Json;

namespace Jarfter.Net.Protocol.Message;

/// <summary>
/// 定义一条带有返回值的请求.
/// </summary>
/// <typeparam name="TMessage">协议类型本身.</typeparam>
/// <typeparam name="TResponse">期望的返回值类型.</typeparam>
public interface INetRequest<TMessage, TResponse> where TMessage : INetRequest<TMessage, TResponse>
{
    /// <summary>
    /// 该协议的名称, 用于在信息流中进行标识.
    /// </summary>
    static abstract string MessageName { get; }

    /// <summary>
    /// 将协议序列化为 JsonElement 对象.
    /// </summary>
    static abstract JsonElement SerializeToElement(INetRequest<TMessage, TResponse> request);
}
