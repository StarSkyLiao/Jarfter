namespace Jarfter.Net.Server.Hosting;

/// <summary>
/// 表示服务端网络配置项.
/// </summary>
public class NetServerOptions
{
    /// <summary>
    /// 获取或设置 SignalR Hub 的路由.
    /// </summary>
    public required string HubPath { get; set; }
}
