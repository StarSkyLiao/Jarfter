namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 提供基准展示使用的数值格式化工具.
/// </summary>
internal static class BenchmarkStatistics
{
    /// <summary>
    /// 按数量级缩放数值并附加单位后缀.
    /// </summary>
    /// <param name="value">原始数值.</param>
    /// <param name="format">数值格式.</param>
    /// <returns>格式化后的字符串.</returns>
    internal static string Fmt(this decimal value, string? format = null)
    {
        decimal abs = Math.Abs(value);
        (decimal scaled, string suffix) result = abs switch
        {
            >= 1e+15m => (value / 1e+15m, "P"),
            >= 1e+12m => (value / 1e+12m, "T"),
            >= 1e+9m  => (value / 1e+9m,  "G"),
            >= 1e+6m  => (value / 1e+6m,  "M"),
            >= 1e+3m  => (value / 1e+3m,  "K"),
            >= 1e+0m  => (value,          string.Empty),
            >= 1e-3m  => (value / 1e-3m,  "m"),
            >= 1e-6m  => (value / 1e-6m,  "u"),
            >= 1e-9m  => (value / 1e-9m,  "n"),
            _         => (value / 1e-12m, "p"),
        };
        
        return $"{result.scaled.ToString(format)}{result.suffix}";
    }
}
