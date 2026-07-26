namespace Jarfter.HexCube.Pathfinding;

/// <summary>
/// 验证寻路算法使用的启发函数权重.
/// </summary>
internal static class HeuristicWeight
{
    /// <summary>
    /// 验证并返回指定的启发函数权重.
    /// </summary>
    /// <param name="value">待验证的权重.</param>
    /// <param name="parameterName">权重参数名称.</param>
    /// <returns>已验证的有限正数权重.</returns>
    /// <exception cref="ArgumentOutOfRangeException">当权重不是有限正数时抛出.</exception>
    internal static double Validate(double value, string parameterName)
    {
        if (!(value > 0) || !double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Heuristic weight must be a finite positive number.");
        }

        return value;
    }
}
