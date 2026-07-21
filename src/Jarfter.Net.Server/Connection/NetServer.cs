using Jarfter.Net.Server.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jarfter.Net.Server.Connection;

/// <summary>
/// 管理服务端 Web 应用程序生成器的创建.
/// <para>每个实例只能创建一次生成器, 以确保服务配置集中在同一个 <see cref="WebApplicationBuilder"/> 中.</para>
/// </summary>
/// <param name="serverUrls">服务端监听地址列表.</param>
public class NetServer(params string[] serverUrls)
{
    private WebApplicationBuilder? m_WebApplicationBuilder = null;

    /// <summary>
    /// 服务器监听的地址列表.
    /// </summary>
    public string[] ServerUrls => serverUrls;

    /// <summary>
    /// 使用构造时提供的服务端地址创建一个 WebApplicationBuilder 对象.
    /// <para>每个 NetServer 对象只允许调用一次.</para>
    /// </summary>
    /// <returns>已配置基础服务的 Web 应用程序生成器.</returns>
    /// <exception cref="NotSupportedException">多次调用该方法.</exception>
    public WebApplicationBuilder CreateBuilder()
    {
        if (m_WebApplicationBuilder is not null)
        {
            throw new NotSupportedException("WebApplicationBuilder is created multi-time.");
        }
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(ServerUrls);
        builder.Logging.ClearProviders();
        builder.Services.AddSignalR();
        builder.Services.AddOptions<NetServerOptions>();
        return m_WebApplicationBuilder = builder;
    }

}
