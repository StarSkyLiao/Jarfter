using System.Text.Json;
using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Protocol.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jarfter.Net.Client.Connection;

public partial class NetClient
{
    /// <summary>
    /// 向服务端提交指定房间中的通用消息.
    /// </summary>
    /// <param name="message">要提交的消息包.</param>
    /// <param name="cancelToken">用于取消异步调用的令牌.</param>
    /// <returns>表示异步调用过程的任务.</returns>
    public ValueTask SendMessageAsync(NetMessage message, CancellationToken cancelToken = default) =>
        new ValueTask(Connection.InvokeAsync(HubMethods.SendMsg, message, cancelToken));

    /// <summary>
    /// 向服务端提交指定房间中的通用消息.
    /// </summary>
    /// <param name="request">要提交的消息包.</param>
    /// <param name="cancelToken">用于取消异步调用的令牌.</param>
    /// <returns>表示异步调用过程的任务.</returns>
    public ValueTask SendMessageAsync<TMessage>(INetRequest<TMessage> request, CancellationToken cancelToken = default)
        where TMessage : INetRequest<TMessage>
    {
        NetMessage netMessage = new NetMessage(TMessage.MessageName,
            TMessage.SerializeToElement(request), Guid.CreateVersion7()
        );
        return new ValueTask(Connection.InvokeAsync(HubMethods.SendMsg, netMessage, cancelToken));
    }

    /// <summary>
    /// 向服务端提交指定房间中的通用请求消息并等待返回值.
    /// </summary>
    /// <typeparam name="T">服务端返回值类型.</typeparam>
    /// <param name="message">要提交的消息包.</param>
    /// <param name="cancelToken">用于取消异步调用的令牌.</param>
    /// <returns>服务端处理程序返回的结果.</returns>
    public async ValueTask<T?> RequestAsync<T>(NetMessage message, CancellationToken cancelToken = default)
    {
        NetResponse response = await Connection.InvokeAsync<NetResponse>(HubMethods.SendMsg, message, cancelToken);
        return response.Payload.Deserialize<T>();
    }

    /// <summary>
    /// 向服务端提交指定房间中的通用请求消息并等待返回值.
    /// </summary>
    /// <typeparam name="TMessage">协议的类型.</typeparam>
    /// <typeparam name="TOut">服务端返回值类型.</typeparam>
    /// <param name="message">要提交的消息包.</param>
    /// <param name="cancelToken">用于取消异步调用的令牌.</param>
    /// <returns>服务端处理程序返回的结果.</returns>
    public async ValueTask<TOut?> RequestAsync<TMessage, TOut>(INetRequest<TMessage, TOut> message, CancellationToken cancelToken = default)
        where TMessage : INetRequest<TMessage, TOut>
    {
        NetMessage netMessage = new NetMessage(TMessage.MessageName,
            TMessage.SerializeToElement(message), Guid.CreateVersion7()
        );

        NetResponse response = await Connection.InvokeAsync<NetResponse>(HubMethods.SendMsg, netMessage, cancelToken);
        return response.Payload.Deserialize<TOut>();
    }
}
