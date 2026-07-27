namespace Jarfter.NavMesh.Query;

/// <summary>
/// 控制单次导航网格查询的性能与最优性取舍.
/// 查询开始时会捕获当前属性值, 后续修改仅影响之后发起的查询.
/// </summary>
public sealed class NavMeshQueryOptions
{

    /// <summary>
    /// 获取或设置 A* 启发式权重.
    /// 值为 1 时执行最优 corridor 搜索; 大于 1 时执行 Weighted A*, 以更少扩展节点换取搜索成本精度.
    /// </summary>
    public double HeuristicWeight
    {
        get;
        set
        {
            if (!double.IsFinite(value) || value < 1)
                throw new ArgumentOutOfRangeException(nameof(value), "启发式权重必须是大于等于 1 的有限 double 值.");
            field = value;
        }
    } = 1;
}
