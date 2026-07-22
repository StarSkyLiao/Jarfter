using Jarfter.Net.Client.Connection;
using Jarfter.Net.Protocol.Message;

namespace Jarfter.Net.xUnit.DemoTest;

internal static class DemoClientATest
{
    public static void RunDemo() => RunDemoAsync().GetAwaiter().GetResult();

    public static async Task RunDemoAsync()
    {
        await using NetClient netClient = await HandlePlayerA();
        Console.ReadLine();
        await netClient.StopAsync();
    }

    public static async Task<NetClient> InitClient()
    {
        NetClient player = new NetClient(new Uri(DemoProtocolTest.ServerFullUrl));
        player.On<DemoProtocolTest.SimpleMsg>(msg =>
            Console.WriteLine($"Player {player.Connection.ConnectionId} received {nameof(DemoProtocolTest.SimpleMsg)}: {msg.Text}")
        );
        player.On<InternalMessage.ClientJoinedNtf>(msg =>
            Console.WriteLine($"Player {msg.ConnectionId} joined!")
        );
        await player.StartAsync();
        return player;
    }

    public static async Task<NetClient> HandlePlayerA()
    {
        NetClient playerA = await InitClient();
        await playerA.SendMessageAsync(new DemoProtocolTest.C2SNoRspReq(
            $"client message from {playerA.Connection.ConnectionId}")
        );
        int askResult = await playerA.RequestAsync(new DemoProtocolTest.C2SAskReq());
        Console.WriteLine($"Player {playerA.Connection.ConnectionId} received ask result: {askResult}");
        return playerA;
    }
}
