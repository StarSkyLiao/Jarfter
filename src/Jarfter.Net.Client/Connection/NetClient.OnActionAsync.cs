using Microsoft.AspNetCore.SignalR.Client;

namespace Jarfter.Net.Client.Connection;

public partial class NetClient
{
    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable OnAsync(string methodName, Func<Task> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable OnAsync<T1>(string methodName, Func<T1, Task> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable OnAsync<T1, T2>(string methodName, Func<T1, T2, Task> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable OnAsync<T1, T2, T3>(string methodName, Func<T1, T2, T3, Task> handler) => Connection.On(methodName, handler);

}
