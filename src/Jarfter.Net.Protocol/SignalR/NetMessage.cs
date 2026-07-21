using System.Text.Json;

namespace Jarfter.Net.Protocol.SignalR;

/// <summary>
/// 表示在网络层上传递的通用消息包.
/// </summary>
/// <param name="Name">
/// 获取消息名称，用于标识消息类型或路由目标.
/// </param>
/// <param name="Payload">
/// 获取消息负载, 以 <see cref="JsonElement"/> 形式保存序列化后的消息数据.
/// </param>
/// <param name="MessageId">
/// 获取消息标识.
/// 当消息需要请求/响应关联或结果回传时应提供唯一标识; 否则为 <see langword="null"/>.
/// </param>
public sealed record NetMessage(string Name, JsonElement Payload, Guid? MessageId = null);
