namespace Jarfter.Core.Collections.Extensions;

/// <summary>
/// 提供 <see cref="ViewOptions"/> 的链式配置扩展方法.
/// </summary>
public static class ViewOptionsWith
{
    extension(ref ViewOptions self)
    {
        /// <summary>
        /// 设置 <see cref="ViewOptions.CollectionLeft"/> 的值.
        /// </summary>
        /// <param name="value">新的集合左边界文本.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithCollectionLeft(string value)
        {
            self.CollectionLeft = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.CollectionRight"/> 的值.
        /// </summary>
        /// <param name="value">新的集合右边界文本.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithCollectionRight(string value)
        {
            self.CollectionRight = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.CollectionSplitter"/> 的值.
        /// </summary>
        /// <param name="value">新的集合元素分隔符文本.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithCollectionSplitter(string value)
        {
            self.CollectionSplitter = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.DictionaryLeft"/> 的值.
        /// </summary>
        /// <param name="value">新的字典左边界文本.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithDictionaryLeft(string value)
        {
            self.DictionaryLeft = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.DictionaryRight"/> 的值.
        /// </summary>
        /// <param name="value">新的字典右边界文本.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithDictionaryRight(string value)
        {
            self.DictionaryRight = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.DictionarySplitter"/> 的值.
        /// </summary>
        /// <param name="value">新的字典元素分隔符文本.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithDictionarySplitter(string value)
        {
            self.DictionarySplitter = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.DictionaryWriter"/> 的值.
        /// </summary>
        /// <param name="value">新的字典写入器的委托实现.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithDictionaryWriter(ViewOptions.DictionaryViewMethod value)
        {
            self.DictionaryWriter = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.NullLiteral"/> 的值.
        /// </summary>
        /// <param name="value">新的空值文本表示.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithNullLiteral(string value)
        {
            self.NullLiteral = value;
            return ref self;
        }

        /// <summary>
        /// 设置 <see cref="ViewOptions.MaxDepth"/> 的值.
        /// </summary>
        /// <param name="value">新的最大展开深度, 0 表示不限制.</param>
        /// <returns>当前 <see cref="ViewOptions"/> 的引用, 用于链式调用.</returns>
        public ref ViewOptions WithMaxDepth(uint value)
        {
            self.MaxDepth = value;
            return ref self;
        }
    }
}
