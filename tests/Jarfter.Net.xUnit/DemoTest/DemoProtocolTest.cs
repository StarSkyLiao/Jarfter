using Jarfter.Net.Server.Hosting;

namespace Jarfter.Net.xUnit.DemoTest;

internal static class DemoProtocolTest
{
    internal class DemoOption : NetServerOptions;
    internal const string ServerUrl = "http://127.0.0.1:5198";
    internal const string HubRoutes = "/game";
    internal const string ServerFullUrl = $"{ServerUrl}{HubRoutes}";

    internal record struct SimpleMsg(string Text);
    internal record struct C2SNoRspReq(string Text);
    internal record struct C2SAskReq;
}
