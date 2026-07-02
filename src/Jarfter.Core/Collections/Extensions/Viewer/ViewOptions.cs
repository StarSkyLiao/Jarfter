namespace Jarfter.Core.Collections.Extensions;

/// <summary>
/// 表示集合文本化展示时使用的格式选项.
/// </summary>
public struct ViewOptions()
{
    /// <summary>
    /// 集合左边界文本.
    /// </summary>
    public string CollectionLeft = "[";

    /// <summary>
    /// 集合右边界文本.
    /// </summary>
    public string CollectionRight = "]";

    /// <summary>
    /// 集合元素之间的分隔符文本.
    /// </summary>
    public string CollectionSplitter = ",";

    /// <summary>
    /// 字典左边界文本.
    /// </summary>
    public string DictionaryLeft = "[";

    /// <summary>
    /// 字典右边界文本.
    /// </summary>
    public string DictionaryRight = "]";

    /// <summary>
    /// 字典元素之间的分隔符文本.
    /// </summary>
    public string DictionarySplitter = ",";

    /// <summary>
    /// 字典写入器的委托实现.
    /// </summary>
    public DictionaryViewMethod DictionaryWriter = WriteDictionary;

    /// <summary>
    /// 空值文本表示.
    /// </summary>
    public string NullLiteral = "null";

    /// <summary>
    /// 最大展开深度, 0 表示不限制.
    /// </summary>
    public uint MaxDepth = 0;

    /// <summary>
    /// 字典写入函数的委托定义.
    /// </summary>
    public delegate void DictionaryViewMethod(in ViewOptions options, ICollectionDisplayer self, TextWriter textWriter, object key, object? value, uint depth);

    /// <summary>
    /// 默认的字典写入函数.
    /// </summary>
    public static void WriteDictionary(in ViewOptions options, ICollectionDisplayer self, TextWriter textWriter, object key, object? value, uint depth)
    {
        textWriter.Write("[");
        self.Write(in options, textWriter, key, depth);
        textWriter.Write("] = ");
        self.Write(in options, textWriter, value, depth);
    }
}
