using Jarfter.Core.Collections.Extensions;
using Jarfter.Drawing;
using Jarfter.Drawing.GraphicIO;
using Jarfter.HexCube.Numerics;
using Jarfter.HexCube.Pathfinding;

namespace Jarfter.HexCube.xUnit;

/// <summary>
/// 运行 Theta* 路径搜索的性能测试, 并导出包含高代价地形和单位六边形障碍物的路径图像.
/// </summary>
public static class HexThetaStarRunTest
{
    private static readonly IPathfinder s_Pathfinder =
        new IPathfinder.ThetaStar(IHeuristic.Euclidean.Instance, HexPathfindingConfig.NavigationProvider);

    /// <summary>
    /// 执行 Theta* 搜索并将地图和路径保存为 BMP 图像.
    /// </summary>
    public static void RunResult()
    {
        PathfindingResult result = Run();
        SaveResult(result, "HexThetaStarPath.bmp", new Color32(147, 51, 234), "Theta*");
    }

    /// <summary>
    /// 执行一次 Theta* 搜索并返回从起点到终点的路径及总代价.
    /// </summary>
    /// <returns>路径搜索结果.</returns>
    internal static PathfindingResult Run()
    {
        return Run(s_Pathfinder);
    }

    /// <summary>
    /// 使用与当前运行地图一致的快照创建 Lazy Theta* 寻路器.
    /// </summary>
    /// <returns>配置完成的 Lazy Theta* 寻路器.</returns>
    internal static IPathfinder CreateLazyPathfinder() =>
        new IPathfinder.LazyThetaStar(IHeuristic.Euclidean.Instance, HexPathfindingConfig.NavigationProvider);

    /// <summary>
    /// 在当前运行地图上执行指定寻路器.
    /// </summary>
    /// <param name="pathfinder">要执行的寻路器.</param>
    /// <returns>从固定起点到固定终点的路径搜索结果.</returns>
    internal static PathfindingResult Run(IPathfinder pathfinder)
    {
        return pathfinder.FindPath(HexPathfindingConfig.Start, HexPathfindingConfig.Goal);
    }

    /// <summary>
    /// 将路径绘制为当前运行地图的 BMP 图像并输出生成位置.
    /// </summary>
    /// <param name="result">要绘制的路径搜索结果.</param>
    /// <param name="fileName">相对输出文件名.</param>
    /// <param name="pathColor">路径线颜色.</param>
    /// <param name="algorithmName">用于控制台输出的算法名称.</param>
    internal static void SaveResult(PathfindingResult result, string fileName, Color32 pathColor, string algorithmName)
    {
        IReadOnlyList<HexCubePoint> path = result.Path;
        string filePath = Path.Combine(fileName);

        BitmapExtension.SaveAsBmp(HexPathfindingConfig.RenderMap(
                HexPathfindingConfig.HexMap, path, pathColor), filePath
        );

        Console.WriteLine(path.View());
        Console.WriteLine($"{algorithmName} 总移动代价: {result.TotalCost}");
        Console.WriteLine($"{algorithmName} 路径图已生成: {Path.GetFullPath(filePath)}");
    }

}
