using Jarfter.Drawing;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 运行权重为 2 的 Theta* 路径搜索性能测试, 并导出路径图像.
/// </summary>
public static class HexThetaStarWeightedRunTest
{
    private static readonly IPathfinder s_Pathfinder =
        new IPathfinder.ThetaStar(IHeuristic.Euclidean.Instance, HexPathfindingConfig.NavigationProvider, 2);

    /// <summary>
    /// 执行权重 Theta* 搜索并将地图和路径保存为 BMP 图像.
    /// </summary>
    public static void RunResult()
    {
        PathfindingResult result = Run();
        HexThetaStarRunTest.SaveResult(result, "HexThetaStarWeightedPath.bmp", new Color32(126, 34, 206), "Theta* (权重 2)");
    }

    /// <summary>
    /// 执行一次权重 Theta* 搜索并返回从起点到终点的路径及总代价.
    /// </summary>
    /// <returns>路径搜索结果.</returns>
    internal static PathfindingResult Run() => HexThetaStarRunTest.Run(s_Pathfinder);
}
