using Microsoft.AspNetCore.SignalR.Client;

namespace Jarfter.Net.Client.Connection;

public partial class NetClient
{
    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On(string methodName, Action handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<T1>(string methodName, Action<T1> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<T1, T2>(string methodName, Action<T1, T2> handler) => Connection.On(methodName, handler);

    /// <summary>
    /// 注册一个处理程序, 当调用指定方法名的中心方法时会触发它.
    /// </summary>
    /// <returns>一个可以用来取消订阅中心方法的订阅.</returns>
    public IDisposable On<T1, T2, T3>(string methodName, Action<T1, T2, T3> handler) => Connection.On(methodName, handler);

}
