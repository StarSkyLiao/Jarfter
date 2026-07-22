namespace Jarfter.Net.Protocol.Message;

/// <summary>
/// 定义了一组内置的消息.
/// </summary>
public static partial class InternalMessage
{
    /// <summary>
    /// 服务端通知有客户端加入.
    /// </summary>
    [NetRequest]
    public partial record ClientJoinedNtf(string ConnectionId);

    /// <summary>
    /// 服务端通知有客户端离开.
    /// </summary>
    [NetRequest]
    public partial record ClientLeftNtf(string ConnectionId);

    /// <summary>
    /// 服务端通知有客户端加入该房间.
    /// </summary>
    [NetRequest]
    public partial record JoinRoomNtf(string ConnectionId);

    /// <summary>
    /// 服务端通知有客户端离开该房间.
    /// </summary>
    [NetRequest]
    public partial record LeaveRoomNtf(string ConnectionId);
}
