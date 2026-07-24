namespace Jarfter.Core.Collections.ObjectModel;

/// <summary>
/// 定义可归还给对象池的对象.
/// </summary>
public interface IReusable
{
    /// <summary>
    /// 将当前对象归还到对象池.
    /// </summary>
    internal void Release();
}
