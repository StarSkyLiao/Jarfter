using System.Runtime.CompilerServices;

namespace Jarfter.Core.Diagnostics;

public sealed record MethodWrapperAction(Action InitMethod, 
    [CallerArgumentExpression(nameof(InitMethod))] string? MethodName = null
) : MethodWrapper<bool>(() =>
{
    try
    {
        InitMethod();
        return true;
    }
    catch (Exception)
    {
        return false;
    }
}, MethodName);

public sealed record MethodWrapper<T1, TResult>(Func<T1, TResult> InitMethod, T1 Param1,
    [CallerArgumentExpression(nameof(InitMethod))] string? MethodName = null
) : MethodWrapper<TResult>(() => InitMethod(Param1), MethodName);

public sealed record MethodWrapper<T1, T2, TResult>(Func<T1, T2, TResult> InitMethod, T1 Param1, T2 Param2, 
    [CallerArgumentExpression(nameof(InitMethod))] string? MethodName = null
) : MethodWrapper<TResult>(() => InitMethod(Param1, Param2), MethodName);

public sealed record MethodWrapper<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> InitMethod, T1 Param1, T2 Param2, T3 Param3,
    [CallerArgumentExpression(nameof(InitMethod))] string? MethodName = null
) : MethodWrapper<TResult>(() => InitMethod(Param1, Param2, Param3), MethodName);

public static class MethodWrapper
{
    public static MethodWrapper<bool> Create(Action initMethod,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapperAction(initMethod, methodName);
    
    public static MethodWrapper<TResult> Create<TResult>(Func<TResult> initMethod,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<TResult>(initMethod, methodName);
    
    public static MethodWrapper<T1, TResult> Create<T1, TResult>(Func<T1, TResult> initMethod, T1 param1,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<T1, TResult>(initMethod, param1, methodName);
    
    public static MethodWrapper<T1, T2, TResult> Create<T1, T2, TResult>(Func<T1, T2, TResult> initMethod, T1 param1, T2 param2,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<T1, T2, TResult>(initMethod, param1, param2, methodName);
    
    public static MethodWrapper<T1, T2, T3, TResult> Create<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> initMethod, T1 param1, T2 param2, T3 param3,
        [CallerArgumentExpression(nameof(initMethod))] string? methodName = null
    ) => new MethodWrapper<T1, T2, T3, TResult>(initMethod, param1, param2, param3, methodName);
    
}
