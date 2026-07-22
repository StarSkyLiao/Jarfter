using Jarfter.Net.Server.Hosting;
using Jarfter.Net.Server.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Jarfter.Net.Server.Connection;

/// <summary>
/// 用于配置并构建 NetServer Web 应用程序的值类型生成器.
/// <para>实例必须通过 <see cref="CreateDefault"/> 创建, 且在调用 <c>Build</c> 后不可再次使用.</para>
/// </summary>
/// <typeparam name="TOption">服务端配置选项的类型.</typeparam>
public class NetServerBuilder<TOption> where TOption : NetServerOptions
{
    /// <summary>
    /// 服务器监听的地址列表.
    /// </summary>
    public string[] ServerUrls { get; internal set; }

    /// <summary>
    /// 底层的 WebApplicationBuilder 对象.
    /// </summary>
    public WebApplicationBuilder WebApplicationBuilder { get; internal set; } = WebApplication.CreateBuilder();

    private NetServerBuilder(params string[] serverUrls) => ServerUrls = serverUrls;

    /// <summary>
    /// 使用给定的服务端地址创建一个 NetServerBuilder 对象.
    /// </summary>
    /// <param name="serverUrls">服务端监听地址列表.</param>
    /// <returns>已配置基础服务的服务端生成器.</returns>
    public static NetServerBuilder<TOption> CreateDefault(params string[] serverUrls)
    {
        NetServerBuilder<TOption> netServerBuilder = new NetServerBuilder<TOption>(serverUrls);
        netServerBuilder.WebApplicationBuilder.WebHost.UseUrls(netServerBuilder.ServerUrls);
        netServerBuilder.Services.TryAddSingleton<IBroadcaster, DefaultBroadcaster>();
        netServerBuilder.Services.TryAddSingleton<IClientManager, DefaultClientManager>();
        netServerBuilder.Services.TryAddSingleton<INetMsgDispatcher, DefaultNetMsgDispatcher>();
        netServerBuilder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });
        netServerBuilder.WebApplicationBuilder.Services.AddSignalR();
        netServerBuilder.WebApplicationBuilder.Services.AddOptions<TOption>();
        return netServerBuilder;
    }

    /// <summary>
    /// 配置 NetServerOptions 选项.
    /// </summary>
    /// <param name="configure">配置服务端选项的委托.</param>
    /// <returns>应用配置后的服务端生成器.</returns>
    public NetServerBuilder<TOption> ConfigureNetServerOptions(Action<TOption> configure)
    {
        WebApplicationBuilder.Services.Configure(configure);
        return this;
    }

    /// <summary>
    /// 构建 Web 应用程序, 并使当前生成器实例失效以防止重复使用.
    /// </summary>
    /// <returns>构建完成的 Web 应用程序.</returns>
    /// <exception cref="NotSupportedException">生成器未通过 <see cref="NetServerBuilder{TOption}.CreateDefault"/> 创建或已经使用过时抛出.</exception>
    public WebApplication Build()
    {
        if (ServerUrls == null!) throw new NotSupportedException($"The {nameof(NetServerBuilder<>)} is not valid!");
        WebApplication webApplication = WebApplicationBuilder.Build();
        ServerUrls = null!;
        return webApplication;
    }

    /// <summary>
    /// 一个供应用程序组合的服务集合. 这对于添加用户提供的或框架提供的服务很有用.
    /// </summary>
    public IServiceCollection Services => WebApplicationBuilder.Services;

}
