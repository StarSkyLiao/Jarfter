using System.Runtime.CompilerServices;

namespace Jarfter.Core.Diagnostics;

/// <summary>
/// 封装无返回值方法, 并将执行成功状态转换为布尔结果。
/// </summary>
/// <param name="initMethod">需要执行的无返回值方法。</param>
/// <param name="methodName">方法名称或调用表达式文本。</param>
public sealed class MethodWrapperAction(Action initMethod,
    [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
) : MethodWrapper<bool>(() =>
{
    try
    {
        initMethod();
        return true;
    }
    catch (Exception)
    {
        return false;
    }
}, methodName);

/// <summary>
/// 封装包含一个参数的基准测试方法。
/// </summary>
/// <typeparam name="T1">第一个参数的类型。</typeparam>
/// <typeparam name="TResult">返回值类型。</typeparam>
/// <param name="initMethod">需要执行的测试方法。</param>
/// <param name="param1">传入测试方法的第一个参数。</param>
/// <param name="methodName">方法名称或调用表达式文本。</param>
public sealed class MethodWrapper<T1, TResult>(Func<T1, TResult> initMethod, T1 param1,
    [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
) : MethodWrapper<TResult>(() => initMethod(param1), methodName);

/// <summary>
/// 封装包含两个参数的基准测试方法。
/// </summary>
/// <typeparam name="T1">第一个参数的类型。</typeparam>
/// <typeparam name="T2">第二个参数的类型。</typeparam>
/// <typeparam name="TResult">返回值类型。</typeparam>
/// <param name="initMethod">需要执行的测试方法。</param>
/// <param name="param1">传入测试方法的第一个参数。</param>
/// <param name="param2">传入测试方法的第二个参数。</param>
/// <param name="methodName">方法名称或调用表达式文本。</param>
public sealed class MethodWrapper<T1, T2, TResult>(Func<T1, T2, TResult> initMethod, T1 param1, T2 param2,
    [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
) : MethodWrapper<TResult>(() => initMethod(param1, param2), methodName);

/// <summary>
/// 封装包含三个参数的基准测试方法。
/// </summary>
/// <typeparam name="T1">第一个参数的类型。</typeparam>
/// <typeparam name="T2">第二个参数的类型。</typeparam>
/// <typeparam name="T3">第三个参数的类型。</typeparam>
/// <typeparam name="TResult">返回值类型。</typeparam>
/// <param name="initMethod">需要执行的测试方法。</param>
/// <param name="param1">传入测试方法的第一个参数。</param>
/// <param name="param2">传入测试方法的第二个参数。</param>
/// <param name="param3">传入测试方法的第三个参数。</param>
/// <param name="methodName">方法名称或调用表达式文本。</param>
public sealed class MethodWrapper<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> initMethod, T1 param1, T2 param2, T3 param3,
    [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
) : MethodWrapper<TResult>(() => initMethod(param1, param2, param3), methodName);

/// <summary>
/// 提供创建不同参数数量方法包装器的工厂方法。
/// </summary>
public static class MethodWrapper
{
    /// <summary>
    /// 创建无返回值方法的包装器。
    /// </summary>
    /// <param name="initMethod">需要执行的无返回值方法。</param>
    /// <param name="methodName">方法名称或调用表达式文本。</param>
    /// <returns>封装后的方法包装器。</returns>
    public static MethodWrapper<bool> Create(Action initMethod,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapperAction(initMethod, methodName);

    /// <summary>
    /// 创建无参数方法的包装器。
    /// </summary>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="initMethod">需要执行的测试方法。</param>
    /// <param name="methodName">方法名称或调用表达式文本。</param>
    /// <returns>封装后的方法包装器。</returns>
    public static MethodWrapper<TResult> Create<TResult>(Func<TResult> initMethod,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<TResult>(initMethod, methodName);

    /// <summary>
    /// 创建包含一个参数方法的包装器。
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。</typeparam>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="initMethod">需要执行的测试方法。</param>
    /// <param name="param1">传入测试方法的第一个参数。</param>
    /// <param name="methodName">方法名称或调用表达式文本。</param>
    /// <returns>封装后的方法包装器。</returns>
    public static MethodWrapper<T1, TResult> Create<T1, TResult>(Func<T1, TResult> initMethod, T1 param1,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<T1, TResult>(initMethod, param1, methodName);

    /// <summary>
    /// 创建包含两个参数方法的包装器。
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。</typeparam>
    /// <typeparam name="T2">第二个参数的类型。</typeparam>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="initMethod">需要执行的测试方法。</param>
    /// <param name="param1">传入测试方法的第一个参数。</param>
    /// <param name="param2">传入测试方法的第二个参数。</param>
    /// <param name="methodName">方法名称或调用表达式文本。</param>
    /// <returns>封装后的方法包装器。</returns>
    public static MethodWrapper<T1, T2, TResult> Create<T1, T2, TResult>(Func<T1, T2, TResult> initMethod, T1 param1, T2 param2,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<T1, T2, TResult>(initMethod, param1, param2, methodName);

    /// <summary>
    /// 创建包含三个参数方法的包装器。
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。</typeparam>
    /// <typeparam name="T2">第二个参数的类型。</typeparam>
    /// <typeparam name="T3">第三个参数的类型。</typeparam>
    /// <typeparam name="TResult">返回值类型。</typeparam>
    /// <param name="initMethod">需要执行的测试方法。</param>
    /// <param name="param1">传入测试方法的第一个参数。</param>
    /// <param name="param2">传入测试方法的第二个参数。</param>
    /// <param name="param3">传入测试方法的第三个参数。</param>
    /// <param name="methodName">方法名称或调用表达式文本。</param>
    /// <returns>封装后的方法包装器。</returns>
    public static MethodWrapper<T1, T2, T3, TResult> Create<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> initMethod, T1 param1, T2 param2, T3 param3,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<T1, T2, T3, TResult>(initMethod, param1, param2, param3, methodName);

}
