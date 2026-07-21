using Microsoft.AspNetCore.SignalR.Client;

namespace Jarfter.Net.Client.Connection;

public partial class NetClient
{
    /// <summary>
    /// 注册一个处理程序, 当调用具有指定方法名的中心方法时将触发它.
    /// 如果服务器请求结果, 返回处理程序的返回值给服务器.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<TResult>(string methodName, Func<TResult> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用具有指定方法名的中心方法时将触发它.
    /// 如果服务器请求结果, 返回处理程序的返回值给服务器.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<T1, TResult>(string methodName, Func<T1, TResult> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用具有指定方法名的中心方法时将触发它.
    /// 如果服务器请求结果, 返回处理程序的返回值给服务器.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<T1, T2, TResult>(string methodName, Func<T1, T2, TResult> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用具有指定方法名的中心方法时将触发它.
    /// 如果服务器请求结果, 返回处理程序的返回值给服务器.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<T1, T2, T3, TResult>(string methodName, Func<T1, T2, T3, TResult> handler) => Connection.On(methodName, handler);

}
