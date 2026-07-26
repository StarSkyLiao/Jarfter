using Jarfter.Drawing;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 运行 Lazy Theta* 路径搜索的性能测试, 并使用与 Theta* 相同的地图导出路径图像.
/// </summary>
public static class HexLazyThetaStarRunTest
{
    private static readonly IPathfinder s_Pathfinder = HexThetaStarRunTest.CreateLazyPathfinder();

    /// <summary>
    /// 执行 Lazy Theta* 搜索并将地图和路径保存为 BMP 图像.
    /// </summary>
    public static void RunResult()
    {
        PathfindingResult result = Run();
        HexThetaStarRunTest.SaveResult(result, "HexLazyThetaStarPath.bmp", new Color32(14, 116, 144), "Lazy Theta*");
    }

    /// <summary>
    /// 执行一次 Lazy Theta* 搜索并返回从起点到终点的路径及总代价.
    /// </summary>
    /// <returns>路径搜索结果.</returns>
    internal static PathfindingResult Run()
    {
        return HexThetaStarRunTest.Run(s_Pathfinder);
    }
}
