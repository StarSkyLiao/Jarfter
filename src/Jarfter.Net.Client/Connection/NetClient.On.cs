using System.Text.Json;
using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Protocol.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Jarfter.Net.Client.Connection;

public partial class NetClient
{
    /// <summary>
    /// 注册一个处理程序, 收到指定的协议时会触发它.
    /// </summary>
    /// <param name="handler">处理程序.</param>
    /// <typeparam name="TMessage">协议类型.</typeparam>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<TMessage>(Action<TMessage> handler) where TMessage : INetRequest<TMessage>
    {
        return Connection.On(TMessage.MessageName, (Action<NetMessage>)Action);

        void Action(NetMessage netMessage)
        {
            TMessage? message = netMessage.Payload.Deserialize<TMessage>();
            ArgumentNullException.ThrowIfNull(message);
            handler(message);
        }
    }

    /// <summary>
    /// 注册一个异步的处理程序, 收到指定的协议时会触发它.
    /// </summary>
    /// <param name="handler">异步的处理程序.</param>
    /// <typeparam name="TMessage">协议类型.</typeparam>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable OnAsync<TMessage>(Func<TMessage, Task> handler) where TMessage : INetRequest<TMessage>
    {
        return Connection.On(TMessage.MessageName, (Func<NetMessage, Task>)Action);

        Task Action(NetMessage netMessage)
        {
            TMessage? message = netMessage.Payload.Deserialize<TMessage>();
            ArgumentNullException.ThrowIfNull(message);
            return handler(message);
        }
    }

    /// <summary>
    /// 注册一个处理程序, 当调用具有指定方法名的中心方法时将触发它.
    /// 如果服务器请求结果, 返回处理程序的返回值给服务器.
    /// </summary>
    /// <param name="handler">处理程序.</param>
    /// <typeparam name="TMessage">协议类型.</typeparam>
    /// <typeparam name="TResult">返回值的类型.</typeparam>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<TMessage, TResult>(Func<TMessage, TResult> handler) where TMessage : INetRequest<TMessage, TResult>
    {
        return Connection.On(TMessage.MessageName, (Func<NetMessage, TResult>)Action);

        TResult Action(NetMessage netMessage)
        {
            TMessage? message = netMessage.Payload.Deserialize<TMessage>();
            ArgumentNullException.ThrowIfNull(message);
            return handler(message);
        }
    }

    /// <summary>
    /// 注册一个异步的处理程序, 当调用具有指定方法名的中心方法时将触发它.
    /// 如果服务器请求结果, 返回处理程序的返回值给服务器.
    /// </summary>
    /// <param name="handler">异步的处理程序.</param>
    /// <typeparam name="TMessage">协议类型.</typeparam>
    /// <typeparam name="TResult">返回值的类型.</typeparam>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable OnAsync<TMessage, TResult>(Func<TMessage, Task<TResult>> handler)  where TMessage : INetRequest<TMessage, TResult>
    {
        return Connection.On(TMessage.MessageName, (Func<NetMessage, Task<TResult>>)Action);

        Task<TResult> Action(NetMessage netMessage)
        {
            TMessage? message = netMessage.Payload.Deserialize<TMessage>();
            ArgumentNullException.ThrowIfNull(message);
            return handler(message);
        }
    }
}
