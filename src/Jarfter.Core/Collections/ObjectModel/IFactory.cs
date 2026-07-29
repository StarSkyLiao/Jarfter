namespace Jarfter.Core.Collections.ObjectModel;

/// <summary>
/// 定义由实现类型以静态抽象成员创建自身的无参数工厂契约.
/// 该契约让调用方可在编译期确定构造路径, 不依赖反射或 Activator.
/// </summary>
/// <typeparam name="T">工厂创建的目标类型.</typeparam>
public interface IFactory<out T>
{
    /// <summary>
    /// 创建目标类型实例.
    /// </summary>
    /// <returns>新创建的目标类型实例.</returns>
    static abstract T Create();
}
