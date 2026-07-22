// ReSharper disable once CheckNamespace
namespace JetBrains.Annotations;

/// <summary>
/// 标记一个方法, 以要求编译器检查其返回值: 未被使用的返回值会被给出一个警告.
/// </summary>
/// <remarks>
/// 使用此属性修饰的方法（与纯方法相反）可能会改变状态,
/// 但如果不使用它们的返回值就没有意义.<br />
/// 与 <see cref="T:System.Diagnostics.Contracts.PureAttribute" /> 类似, 这个属性
/// 可以帮助检测在未使用返回值时的方法调用.
/// 可选地, 你可以指定在显示警告时使用的消息, 例如:
/// <code>[MustUseReturnValue("请使用返回值来...")]</code>.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MustUseReturnValueAttribute(string? justification = null) : Attribute
{
    /// <summary>
    /// 自定义诊断信息.
    /// </summary>
    public string? Justification { get; } = justification;

    /// <summary>
    /// 启用对执行变更并返回 'this' 对象的 "流式" API的特殊处理.
    /// 在这种情况下, 分析会检查流式调用链, 并且只有在初始接收对象很可能是临时值时才会发出警告:
    /// 此时最后一个流式方法的返回值也会被认为是临时的, 因此如果未使用将会收到警告.
    /// 如果初始接收对象是本地变量或 'this' 引用, 分析会假设流式调用用于修改现有值, 这样就不会显示警告.
    /// </summary>
    /// <remarks>
    /// 这个属性只能用于返回类型与接收类型匹配的方法.
    /// </remarks>
    public bool IsFluentBuilderMethod { get; set; }
}
