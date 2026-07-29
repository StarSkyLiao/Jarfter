using System.Collections.Concurrent;

namespace Jarfter.Core.Collections.ObjectModel;

/// <summary>
/// 定义可由 <see cref="Factory"/> 管理的对象池对象.
/// 实现类型应通过 <see cref="CreatePooled"/> 创建实例, 并在不再使用时归还对象.
/// </summary>
/// <typeparam name="T">对象池对象的具体类型.</typeparam>
public interface IPoolable<T> : IReusable where T : class, IPoolable<T>
{
    /// <summary>
    /// 创建一个尚未被租借过的对象池对象.
    /// </summary>
    /// <returns>新创建的对象池对象.</returns>
    internal static abstract T CreatePooled();

    /// <summary>
    /// 获取或设置当前对象所属的对象池.
    /// </summary>
    internal ConcurrentStack<T>? SourcePool { get; set; }

    /// <summary>
    /// 将当前对象归还到其所属的对象池.
    /// </summary>
    void IReusable.Release()
    {
        SourcePool?.Push((T)this);
        SourcePool = null;
    }
}
