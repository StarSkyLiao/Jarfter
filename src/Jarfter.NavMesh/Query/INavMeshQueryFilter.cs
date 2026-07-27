namespace Jarfter.NavMesh.Query;

/// <summary>
/// 决定一次路径查询是否允许穿越指定凸多边形.
/// </summary>
public interface INavMeshQueryFilter
{
    /// <summary>
    /// 判断凸多边形是否允许参与本次查询.
    /// </summary>
    /// <param name="triangleIndex">凸多边形索引. 参数名称为兼容既有接口而保留.</param>
    /// <param name="areaId">凸多边形区域标识.</param>
    /// <param name="flags">凸多边形的通行位掩码.</param>
    /// <returns>允许穿越时为 <see langword="true"/>.</returns>
    bool Pass(int triangleIndex, int areaId, uint flags);
}
