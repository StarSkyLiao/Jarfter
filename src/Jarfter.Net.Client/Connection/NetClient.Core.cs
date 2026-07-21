using Microsoft.AspNetCore.SignalR.Client;

namespace Jarfter.Net.Client.Connection;

/// <summary>
/// 封装网络客户端到 SignalR Hub 的连接.
/// </summary>
/// <param name="serverAddress">服务端地址, 例如 http://127.0.0.1:5198/service.</param>
public partial class NetClient(Uri serverAddress) : IAsyncDisposable
{
    /// <summary>
    /// 获取底层与服务端的 SignalR Hub 连接.
    /// </summary>
    public readonly HubConnection Connection = new HubConnectionBuilder().WithUrl(serverAddress).WithAutomaticReconnect().Build();

    /// <summary>
    /// 启动与服务端的连接.
    /// </summary>
    /// <param name="cancelToken">用于取消异步连接的令牌.</param>
    /// <returns>表示异步连接过程的任务.</returns>
    public ValueTask StartAsync(CancellationToken cancelToken = default) => new ValueTask(Connection.StartAsync(cancelToken));

    /// <summary>
    /// 停止与服务端的连接.
    /// </summary>
    /// <param name="cancelToken">用于取消异步停止的令牌.</param>
    /// <returns>表示异步停止过程的任务.</returns>
    public ValueTask StopAsync(CancellationToken cancelToken = default) => new ValueTask(Connection.StopAsync(cancelToken));

    /// <summary>
    /// 异步释放 SignalR 连接.
    /// </summary>
    /// <returns>表示异步释放过程的值任务.</returns>
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return Connection.DisposeAsync();
    }
}
