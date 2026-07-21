using System.Text.Json;
using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Server.Hosting;

namespace Jarfter.Net.xUnit.DemoTest;

internal static class DemoProtocolTest
{
    internal class DemoOption : NetServerOptions;
    internal const string ServerUrl = "http://127.0.0.1:5198";
    internal const string HubRoutes = "/game";
    internal const string ServerFullUrl = $"{ServerUrl}{HubRoutes}";

    internal record struct SimpleMsg(string Text) : INetMessage<SimpleMsg>
    {
        static string INetMessage<SimpleMsg>.MessageName => typeof(SimpleMsg).FullName ?? typeof(SimpleMsg).Name;

        static JsonElement INetMessage<SimpleMsg>.SerializeToElement(INetMessage<SimpleMsg> message) =>
            JsonSerializer.SerializeToElement((SimpleMsg)message);
    }

    internal record struct C2SNoRspReq(string Text) : INetMessage<C2SNoRspReq>
    {
        static string INetMessage<C2SNoRspReq>.MessageName => typeof(C2SNoRspReq).FullName ?? typeof(C2SNoRspReq).Name;

        static JsonElement INetMessage<C2SNoRspReq>.SerializeToElement(INetMessage<C2SNoRspReq> message) =>
            JsonSerializer.SerializeToElement((C2SNoRspReq)message);
    }

    internal record struct C2SAskReq : INetRequest<C2SAskReq, int>
    {
        static string INetMessage<C2SAskReq>.MessageName => typeof(C2SAskReq).FullName ?? typeof(C2SAskReq).Name;

        static JsonElement INetMessage<C2SAskReq>.SerializeToElement(INetMessage<C2SAskReq> message) =>
            JsonSerializer.SerializeToElement((C2SAskReq)message);
    }
}
