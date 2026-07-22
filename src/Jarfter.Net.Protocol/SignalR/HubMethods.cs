namespace Jarfter.Net.Protocol.SignalR;

/// <summary>
/// 定义 SignalR Hub 的协议方法名.
/// </summary>
public static class HubMethods
{
    /// <summary>
    /// 获取客户端向服务端提交消息的方法名.
    /// </summary>
    public const string SendMsg = nameof(SendMsg);

    /// <summary>
    /// 获取客户端调用服务端加入房间的方法名.
    /// </summary>
    public const string JoinRoom = nameof(JoinRoom);

    /// <summary>
    /// 获取客户端调用服务端离开房间的方法名.
    /// </summary>
    public const string LeaveRoom = nameof(LeaveRoom);

}
