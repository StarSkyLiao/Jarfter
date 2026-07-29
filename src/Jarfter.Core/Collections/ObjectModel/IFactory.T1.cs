namespace Jarfter.Core.Collections.ObjectModel;

/// <summary>
/// 定义由实现类型以静态抽象成员创建自身的单参数工厂契约.
/// </summary>
/// <typeparam name="T">工厂创建的目标类型.</typeparam>
/// <typeparam name="T1">创建目标类型所需的参数类型.</typeparam>
public interface IFactory<out T, in T1>
{
    /// <summary>
    /// 使用指定参数创建目标类型实例.
    /// </summary>
    /// <param name="parameter">创建目标类型所需的参数.</param>
    /// <returns>新创建的目标类型实例.</returns>
    static abstract T Create(T1 parameter);
}
