namespace Jarfter.Net.Server.Service;

/// <summary>
/// 定义一个可以被 <see cref="t:Jarfter.Net.Server.Service.ServiceInitializer"/> 初始化的服务.
/// </summary>
public interface IInitializableService
{
    /// <summary>
    /// 异步初始化该服务.
    /// </summary>
    ValueTask InitializeAsync();
}
