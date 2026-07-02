using System.Collections;

namespace Jarfter.Core.Collections.Generic;

public sealed partial class LinkedArray<T>
{
    /// <summary>
    /// 获取一个值, 指示是否固定大小.
    /// </summary>
    bool IList.IsFixedSize => false;

    /// <summary>
    /// 获取一个值, 指示是否只读.
    /// </summary>
    bool IList.IsReadOnly => false;

    /// <summary>
    /// 获取或设置指定索引处的对象值.
    /// </summary>
    /// <param name="index">逻辑索引.</param>
    object? IList.this[int index]
    {
        get => this[index];
        set
        {
            ThrowIfWrongValueType(value);
            this[index] = (T)value!;
        }
    }

    /// <summary>
    /// 在尾部添加对象值.
    /// </summary>
    /// <param name="value">要添加的对象.</param>
    /// <returns>新元素的逻辑索引.</returns>
    int IList.Add(object? value)
    {
        ThrowIfWrongValueType(value);
        Add((T)value!);
        return Count - 1;
    }

    /// <summary>
    /// 判断是否包含指定对象值.
    /// </summary>
    /// <param name="value">待判断对象.</param>
    /// <returns>包含返回 <see langword="true"/>, 否则返回 <see langword="false"/>.</returns>
    bool IList.Contains(object? value) => IsCompatibleObject(value) && Contains((T)value!);

    /// <summary>
    /// 查找指定对象值的逻辑索引.
    /// </summary>
    /// <param name="value">待查找对象.</param>
    /// <returns>找到返回索引, 否则返回 -1.</returns>
    int IList.IndexOf(object? value) => IsCompatibleObject(value) ? IndexOf((T)value!) : -1;

    /// <summary>
    /// 在指定逻辑索引处插入对象值.
    /// </summary>
    /// <param name="index">插入位置.</param>
    /// <param name="value">要插入的对象.</param>
    void IList.Insert(int index, object? value)
    {
        ThrowIfWrongValueType(value);
        Insert(index, (T)value!);
    }

    /// <summary>
    /// 移除首个匹配对象值.
    /// </summary>
    /// <param name="value">要移除的对象.</param>
    void IList.Remove(object? value)
    {
        if (!IsCompatibleObject(value)) return;
        Remove((T)value!);
    }
}
