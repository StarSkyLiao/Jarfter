namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 表示基准结果展示选项.
/// </summary>
public struct BenchmarkViewOptions()
{
    /// <summary>
    /// 无数据时的提示文本.
    /// </summary>
    public string EmptyMessage { get; init; } = "No benchmark items are enabled.";

    /// <summary>
    /// 未知方法名占位文本.
    /// </summary>
    public string UnknownMethodName { get; init; } = "<unknown>";

    /// <summary>
    /// 方法列标题.
    /// </summary>
    public string MethodHeader { get; init; } = "测试方法";

    /// <summary>
    /// 指标类型列标题.
    /// </summary>
    public string TypeHeader { get; init; } = "测量类型";

    /// <summary>
    /// 均值列标题.
    /// </summary>
    public string MeanHeader { get; init; } = "平均";

    /// <summary>
    /// 总和列标题.
    /// </summary>
    public string SumHeader { get; init; } = "总和";

    /// <summary>
    /// 标准差列标题.
    /// </summary>
    public string StdDevHeader { get; init; } = "标准差";

    /// <summary>
    /// 相对标准差列标题.
    /// </summary>
    public string RsdHeader { get; init; } = "相对标准差";

    /// <summary>
    /// 返回值列标题.
    /// </summary>
    public string ReturnHeader { get; init; } = "返回值";

    /// <summary>
    /// 是否启用控制台着色输出.
    /// </summary>
    public bool UseColor { get; init; } = true;
}
