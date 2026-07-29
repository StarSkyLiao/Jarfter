namespace Jarfter.Core.Collections.ObjectModel;

/// <summary>
/// 定义由实现类型以静态抽象成员创建自身的双参数工厂契约.
/// </summary>
/// <typeparam name="T">工厂创建的目标类型.</typeparam>
/// <typeparam name="T1">创建目标类型所需的第一个参数类型.</typeparam>
/// <typeparam name="T2">创建目标类型所需的第二个参数类型.</typeparam>
public interface IFactory<out T, in T1, in T2>
{
    /// <summary>
    /// 使用指定参数创建目标类型实例.
    /// </summary>
    /// <param name="firstParameter">创建目标类型所需的第一个参数.</param>
    /// <param name="secondParameter">创建目标类型所需的第二个参数.</param>
    /// <returns>新创建的目标类型实例.</returns>
    static abstract T Create(T1 firstParameter, T2 secondParameter);
}
