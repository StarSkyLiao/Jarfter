using Jarfter.Net.Server.Connection;
using Jarfter.Net.Server.Hosting;
using Jarfter.Net.Server.Message.Context;
using Jarfter.Net.Server.Message.Patcher;
using Jarfter.Net.Server.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Jarfter.Net.xUnit.DemoTest;

internal static class DemoServerTest
{
    public static void RunDemo() => RunDemoAsync().GetAwaiter().GetResult();

    public static async Task RunDemoAsync()
    {
        await using WebApplication webApplication = await InitServer();
        Console.ReadLine();
        await webApplication.StopAsync();
    }

    public static async Task<WebApplication> InitServer()
    {
        NetServerBuilder<DemoProtocolTest.DemoOption> builder = NetServerBuilder<DemoProtocolTest.DemoOption>.CreateDefault(DemoProtocolTest.ServerUrl);
        builder.ConfigureNetServerOptions(static options => options.HubPath = DemoProtocolTest.HubRoutes);
        builder.Services.TryAddSingleton<IClientManager, DefaultClientManager>();
        builder.Services.TryAddSingleton<INetMsgDispatcher, DefaultNetMsgDispatcher>();
        WebApplication webApplication = builder.Build();


        NetServerOptions options = webApplication.Services.GetRequiredService<IOptions<DemoProtocolTest.DemoOption>>().Value;
        webApplication.MapHub<NetworkHub>(options.HubPath);

        PatchMsg();

        await webApplication.StartAsync();
        Console.WriteLine($"Server started: {DemoProtocolTest.ServerUrl}");
        return webApplication;

        void PatchMsg()
        {
            INetMsgDispatcher dispatcher = webApplication.Services.GetRequiredService<INetMsgDispatcher>();
            dispatcher.On(new DefaultNetMsgHandler<DemoProtocolTest.C2SNoRspReq>((message, _, _) =>
            {
                Console.WriteLine($"Received {message.Text}");
                return ValueTask.CompletedTask;
            }));
            dispatcher.On(new DefaultNetMsgHandler<DemoProtocolTest.C2SAskReq, int>((_, connectionId, _) =>
            {
                Console.WriteLine($"Received {nameof(DemoProtocolTest.C2SAskReq)} from {connectionId}");
                return new ValueTask<int>(100);
            }));
        }
    }
}
