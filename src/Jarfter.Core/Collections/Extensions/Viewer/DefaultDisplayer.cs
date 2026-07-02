using System.Collections;

namespace Jarfter.Core.Collections.Extensions;

/// <summary>
/// 提供默认的集合与字典文本展示实现。
/// </summary>
public class DefaultDisplayer: ICollectionDisplayer
{
    private DefaultDisplayer(){}
    static DefaultDisplayer() {} // 强制去掉 beforefieldinit, 要求懒加载该类型

    /// <summary>
    /// 获取默认展示器的全局单例实例。
    /// </summary>
    public static readonly DefaultDisplayer Instance = new DefaultDisplayer();

    void ICollectionDisplayer.WriteEnumerable(in ViewOptions viewOptions, TextWriter textWriter, IEnumerable enumerable, uint depth)
    {
        ICollectionDisplayer self = this;
        IEnumerator enumerator = enumerable.GetEnumerator();
        uint childDepth = NextDepth(depth);
        try
        {
            textWriter.Write(viewOptions.CollectionLeft);
            if (enumerator.MoveNext())
            {
                object? current = enumerator.Current;
                self.Write(in viewOptions, textWriter, current, childDepth);
                while (enumerator.MoveNext())
                {
                    textWriter.Write(viewOptions.CollectionSplitter);
                    current = enumerator.Current;
                    self.Write(in viewOptions, textWriter, current, childDepth);
                }
            }
            textWriter.Write(viewOptions.CollectionRight);
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    void ICollectionDisplayer.WriteCollection(in ViewOptions viewOptions, TextWriter textWriter, ICollection collection, uint depth)
    {
        ICollectionDisplayer self = this;
        self.WriteEnumerable(in viewOptions, textWriter, collection, depth);
    }

    void ICollectionDisplayer.WriteDictionary(in ViewOptions viewOptions, TextWriter textWriter, IDictionary dictionary, uint depth)
    {
        ICollectionDisplayer self = this;
        IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
        uint childDepth = NextDepth(depth);
        try
        {
            textWriter.Write(viewOptions.DictionaryLeft);
            if (enumerator.MoveNext())
            {
                viewOptions.DictionaryWriter(in viewOptions, self, textWriter, enumerator.Key, enumerator.Value, childDepth);
                while (enumerator.MoveNext())
                {
                    textWriter.Write(viewOptions.DictionarySplitter);
                    viewOptions.DictionaryWriter(in viewOptions, self, textWriter, enumerator.Key, enumerator.Value, childDepth);
                }
            }
            textWriter.Write(viewOptions.DictionaryRight);
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    private static uint NextDepth(uint depth)
    {
        return depth == uint.MaxValue ? uint.MaxValue : depth + 1;
    }
}
