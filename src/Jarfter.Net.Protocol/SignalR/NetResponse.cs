using System.Text.Json;

namespace Jarfter.Net.Protocol.SignalR;

/// <summary>
/// 表示在网络层上传递的回复包, 专门用于服务器像客户端返回了数据的情况.
/// </summary>
/// <param name="Name">
/// 获取所回复消息的名称.
/// </param>
/// <param name="Payload">
/// 获取消息负载, 以 <see cref="JsonElement"/> 形式保存序列化后的消息数据.
/// </param>
/// <param name="RequestId">
/// 该回复消息所回复对象的 GUID.
/// </param>
/// <param name="MessageId">
/// 获取消息标识.
/// 当消息需要请求/响应关联或结果回传时应提供唯一标识; 否则为 <see langword="null"/>.
/// </param>
public sealed record NetResponse(string Name, JsonElement Payload, Guid? RequestId = null, Guid? MessageId = null);
