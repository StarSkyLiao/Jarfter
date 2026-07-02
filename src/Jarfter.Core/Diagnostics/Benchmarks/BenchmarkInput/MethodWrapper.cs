using System.Runtime.CompilerServices;

namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 包装待测方法及其显示名称.
/// </summary>
/// <typeparam name="TResult">方法返回值类型.</typeparam>
/// <param name="method">待执行的方法.</param>
/// <param name="methodName">方法显示名称.</param>
public class MethodWrapper<TResult>(Func<TResult> method, [CallerArgumentExpression(nameof(method))] string? methodName = null)
{
    /// <summary>
    /// 待执行的方法.
    /// </summary>
    internal Func<TResult> Method => method;

    /// <summary>
    /// 方法显示名称.
    /// </summary>
    internal string? MethodName => methodName;

    /// <summary>
    /// 执行一次目标方法.
    /// </summary>
    /// <returns>方法返回值.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TResult Execute() => Method.Invoke();

    /// <summary>
    /// 将委托隐式转换为方法包装器.
    /// </summary>
    /// <param name="other">待包装的方法.</param>
    public static implicit operator MethodWrapper<TResult>(Func<TResult> other) => new MethodWrapper<TResult>(other);

}
