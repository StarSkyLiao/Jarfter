namespace Jarfter.NavMesh.Query;

/// <summary>
/// 表示沿导航网格三角 corridor 执行二维射线查询的结果.
/// 未抵达终点时, Hit 描述首个边界或过滤器阻挡位置.
/// </summary>
public sealed class NavMeshRaycastResult
{
    internal NavMeshRaycastResult(bool reachedEnd, NavMeshRaycastHit? hit, int[] corridor)
    {
        ReachedEnd = reachedEnd;
        Hit = hit;
        Corridor = corridor;
    }

    /// <summary>
    /// 获取射线是否在不穿越边界或被过滤器阻挡的情况下抵达终点.
    /// </summary>
    public bool ReachedEnd { get; }

    /// <summary>
    /// 获取首次阻挡信息.
    /// 当 ReachedEnd 为 <see langword="true"/> 时为 <see langword="null"/>.
    /// </summary>
    public NavMeshRaycastHit? Hit { get; }

    /// <summary>
    /// 获取射线依次经过的三角形索引.
    /// </summary>
    public IReadOnlyList<int> Corridor { get; }
}
