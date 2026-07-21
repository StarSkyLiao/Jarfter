using Jarfter.Net.Protocol.Message;
using Jarfter.Net.Server.Hosting;

namespace Jarfter.Net.xUnit.DemoTest;

internal static partial class DemoProtocolTest
{
    internal class DemoOption : NetServerOptions;
    internal const string ServerUrl = "http://127.0.0.1:5198";
    internal const string HubRoutes = "/game";
    internal const string ServerFullUrl = $"{ServerUrl}{HubRoutes}";

    [NetRequest]
    internal partial record struct SimpleMsg(string Text);

    [NetRequest]
    internal partial record struct C2SNoRspReq(string Text);

    [NetRequest(typeof(int))]
    internal partial record struct C2SAskReq;
}
