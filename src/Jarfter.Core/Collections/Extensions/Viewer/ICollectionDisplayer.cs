using System.Collections;

namespace Jarfter.Core.Collections.Extensions;

/// <summary>
/// 定义集合内容写入到 <see cref="TextWriter"/> 的展示器接口.
/// </summary>
public interface ICollectionDisplayer
{
    /// <summary>
    /// 根据运行时类型将对象写入文本输出.
    /// </summary>
    /// <param name="viewOptions">渲染选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    /// <param name="item">待写入对象.</param>
    /// <param name="depth">当前递归深度, 根层为 0.</param>
    public void Write(in ViewOptions viewOptions, TextWriter textWriter, object? item, uint depth = 0)
    {
        switch (item)
        {
            case null:
            {
                WriteNull(viewOptions, textWriter);
                break;
            }
            case string content:
            {
                WriteString(viewOptions, textWriter, content);
                break;
            }
            case IDictionary dictionary:
            {
                // 深度超限时不再继续展开, 直接输出节点类型名.
                if (IsDepthExceeded(in viewOptions, depth))
                {
                    WriteTypeName(textWriter, dictionary);
                    break;
                }
                WriteDictionary(viewOptions, textWriter, dictionary, depth);
                break;
            }
            case ICollection collection:
            {
                // 深度超限时不再继续展开, 直接输出节点类型名.
                if (IsDepthExceeded(in viewOptions, depth))
                {
                    WriteTypeName(textWriter, collection);
                    break;
                }
                WriteCollection(viewOptions, textWriter, collection, depth);
                break;
            }
            case IEnumerable enumerable:
            {
                // 深度超限时不再继续展开, 直接输出节点类型名.
                if (IsDepthExceeded(in viewOptions, depth))
                {
                    WriteTypeName(textWriter, enumerable);
                    break;
                }
                WriteEnumerable(viewOptions, textWriter, enumerable, depth);
                break;
            }
            default:
            {
                WriteObject(viewOptions, textWriter, item);
                break;
            }
        }
    }

    /// <summary>
    /// 写入空值表示.
    /// </summary>
    /// <param name="viewOptions">渲染选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    public void WriteNull(in ViewOptions viewOptions, TextWriter textWriter)
    {
        textWriter.Write(viewOptions.NullLiteral);
    }

    /// <summary>
    /// 写入字符串值.
    /// </summary>
    /// <param name="viewOptions">渲染选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    /// <param name="content">待写入字符串内容.</param>
    public void WriteString(in ViewOptions viewOptions, TextWriter textWriter, string content)
    {
        textWriter.Write(content);
    }

    /// <summary>
    /// 写入可枚举对象.
    /// </summary>
    /// <param name="viewOptions">渲染选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    /// <param name="enumerable">待写入可枚举对象.</param>
    /// <param name="depth">当前递归深度.</param>
    public void WriteEnumerable(in ViewOptions viewOptions, TextWriter textWriter, IEnumerable enumerable, uint depth)
    {
        textWriter.Write(enumerable.ToString());
    }

    /// <summary>
    /// 写入集合对象.
    /// </summary>
    /// <param name="viewOptions">渲染选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    /// <param name="collection">待写入集合对象.</param>
    /// <param name="depth">当前递归深度.</param>
    public void WriteCollection(in ViewOptions viewOptions, TextWriter textWriter, ICollection collection, uint depth)
    {
        textWriter.Write(collection.ToString());
    }

    /// <summary>
    /// 写入字典对象.
    /// </summary>
    /// <param name="viewOptions">渲染选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    /// <param name="dictionary">待写入字典对象.</param>
    /// <param name="depth">当前递归深度.</param>
    public void WriteDictionary(in ViewOptions viewOptions, TextWriter textWriter, IDictionary dictionary, uint depth)
    {
        textWriter.Write(dictionary.ToString());
    }

    /// <summary>
    /// 写入普通对象.
    /// </summary>
    /// <param name="viewOptions">渲染选项.</param>
    /// <param name="textWriter">目标文本写入器.</param>
    /// <param name="object">待写入对象.</param>
    public void WriteObject(in ViewOptions viewOptions, TextWriter textWriter, object @object)
    {
        textWriter.Write(@object.ToString());
    }

    private static bool IsDepthExceeded(in ViewOptions viewOptions, uint depth)
    {
        return viewOptions.MaxDepth != 0 && depth > viewOptions.MaxDepth;
    }

    private static void WriteTypeName(TextWriter textWriter, object value)
    {
        Type type = value.GetType();
        textWriter.Write(type.FullName ?? type.Name);
    }
}
