namespace Jarfter.Net.Protocol.Message;

/// <summary>
/// 标记需要由源码生成器实现 <see cref="INetRequest{TMessage}"/> 的网络请求协议.
/// <para>省略返回值类型时生成无返回值请求; 指定返回值类型时生成带返回值请求.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class NetRequestAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="NetRequestAttribute"/> 的新实例.
    /// </summary>
    /// <param name="responseType">请求的返回值类型. 为 <see langword="null"/> 时表示没有返回值.</param>
    public NetRequestAttribute(Type? responseType = null) => ResponseType = responseType;

    /// <summary>
    /// 获取请求的返回值类型.
    /// </summary>
    public Type? ResponseType { get; }
}
