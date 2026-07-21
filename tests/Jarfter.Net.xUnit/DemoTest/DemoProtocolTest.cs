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

    internal record struct SimpleMsg(string Text) : INetRequest<SimpleMsg>
    {
        static string INetRequest<SimpleMsg>.MessageName => typeof(SimpleMsg).FullName ?? typeof(SimpleMsg).Name;

        static JsonElement INetRequest<SimpleMsg>.SerializeToElement(INetRequest<SimpleMsg> request) =>
            JsonSerializer.SerializeToElement((SimpleMsg)request);
    }

    internal record struct C2SNoRspReq(string Text) : INetRequest<C2SNoRspReq>
    {
        static string INetRequest<C2SNoRspReq>.MessageName => typeof(C2SNoRspReq).FullName ?? typeof(C2SNoRspReq).Name;

        static JsonElement INetRequest<C2SNoRspReq>.SerializeToElement(INetRequest<C2SNoRspReq> request) =>
            JsonSerializer.SerializeToElement((C2SNoRspReq)request);
    }

    internal record struct C2SAskReq : INetRequest<C2SAskReq, int>
    {
        static string INetRequest<C2SAskReq, int>.MessageName => typeof(C2SAskReq).FullName ?? typeof(C2SAskReq).Name;

        static JsonElement INetRequest<C2SAskReq, int>.SerializeToElement(INetRequest<C2SAskReq, int> message) =>
            JsonSerializer.SerializeToElement((C2SAskReq)message);
    }
}
