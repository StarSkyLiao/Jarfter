using Microsoft.Extensions.Hosting;

namespace Jarfter.Net.Server.Service;

/// <summary>
/// 初始化实现了 <see cref="t:Jarfter.Net.Server.Service.IInitializableService"/> 接口的服务.
/// </summary>
public class ServiceInitializer(IEnumerable<IInitializableService> services) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken token)
    {
        foreach (IInitializableService service in services)
        {
            await service.InitializeAsync();
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
