namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示 <see cref="LinkedArray{T}"/> 的节点句柄.
/// 节点索引会随结构调整而变化, 因此通过版本号确保句柄一致性.
/// </summary>
/// <typeparam name="T">元素类型.</typeparam>
public readonly struct LinkedArrayNode<T>
{
    /// <summary>
    /// 获取当前节点在内部数组中的索引.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 获取当前节点的值.
    /// 当节点版本已失效时抛出异常.
    /// </summary>
    /// <exception cref="InvalidOperationException">节点版本与所属容器不一致时抛出.</exception>
    public T Value
    {
        get
        {
            EnsureVersion();
            return Target.Items[Index].Value;
        }
    }

    /// <summary>
    /// 获取前驱节点.
    /// </summary>
    public LinkedArrayNode<T>? Previous
    {
        get
        {
            EnsureVersion();
            int previous = Target.Items[Index].Front;
            return previous == 0 ? null : new LinkedArrayNode<T>(previous, Target);
        }
    }

    /// <summary>
    /// 获取后继节点.
    /// </summary>
    public LinkedArrayNode<T>? Next
    {
        get
        {
            EnsureVersion();
            int next = Target.Items[Index].Next;
            return next == 0 ? null : new LinkedArrayNode<T>(next, Target);
        }
    }

    /// <summary>
    /// 获取节点所属容器的版本号快照.
    /// </summary>
    internal int Version { get; }

    /// <summary>
    /// 获取节点所属的容器实例.
    /// </summary>
    internal LinkedArray<T> Target { get; }

    /// <summary>
    /// 初始化一个节点句柄.
    /// </summary>
    /// <param name="index">节点索引.</param>
    /// <param name="target">所属容器.</param>
    internal LinkedArrayNode(int index, LinkedArray<T> target)
    {
        Index = index;
        Target = target;
        Version = target.Version;
    }

    private void EnsureVersion()
    {
        if (Target is null)
        {
            throw new InvalidOperationException("The linked-array node is not initialized.");
        }

        if (Target.Version != Version)
        {
            throw new InvalidOperationException("The linked-array node has expired.");
        }
    }
}
