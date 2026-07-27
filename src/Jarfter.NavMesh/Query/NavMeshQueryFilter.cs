namespace Jarfter.NavMesh.Query;

/// <summary>
/// 使用 include/exclude 位掩码决定三角形是否可通行的默认查询过滤器.
/// 三角形至少需要拥有一个 IncludedFlags 位, 且不得拥有任何 ExcludedFlags 位.
/// </summary>
public sealed class NavMeshQueryFilter : INavMeshQueryFilter
{
    /// <summary>
    /// 获取或设置允许通行的 flags 位集合.
    /// 默认允许全部位.
    /// </summary>
    public uint IncludedFlags { get; init; } = uint.MaxValue;

    /// <summary>
    /// 获取或设置禁止通行的 flags 位集合.
    /// 默认不排除任何位.
    /// </summary>
    public uint ExcludedFlags { get; init; }

    /// <summary>
    /// 判断三角形 flags 是否满足 include/exclude 条件.
    /// </summary>
    /// <param name="triangleIndex">三角形索引.</param>
    /// <param name="areaId">三角形区域标识.</param>
    /// <param name="flags">三角形的通行位掩码.</param>
    /// <returns>允许穿越时为 <see langword="true"/>.</returns>
    public bool Pass(int triangleIndex, int areaId, uint flags)
    {
        return (flags & IncludedFlags) != 0 && (flags & ExcludedFlags) == 0;
    }
}
