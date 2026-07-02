using System.Collections;

namespace Jarfter.Core.Collections.Extensions;

/// <summary>
/// 拓展集合的一些方法, 主要用于快速展示集合的内容.
/// </summary>
public static class ViewerExtension
{
    extension(IEnumerable enumerable)
    {
        /// <summary>
        /// 使用默认选项和默认展示器渲染当前集合.
        /// </summary>
        /// <returns>渲染后的字符串结果.</returns>
        public string View() => enumerable.View(new ViewOptions(), DefaultDisplayer.Instance);

        /// <summary>
        /// 使用指定选项和默认展示器渲染当前集合.
        /// </summary>
        /// <param name="viewOptions">渲染选项.</param>
        /// <returns>渲染后的字符串结果.</returns>
        public string View(in ViewOptions viewOptions) => enumerable.View(viewOptions, DefaultDisplayer.Instance);

        /// <summary>
        /// 使用指定选项和指定展示器渲染当前集合.
        /// </summary>
        /// <param name="viewOptions">渲染选项.</param>
        /// <param name="displayer">集合展示器.</param>
        /// <returns>渲染后的字符串结果.</returns>
        public string View(in ViewOptions viewOptions, ICollectionDisplayer displayer)
        {
            using StringWriter writer = new StringWriter();
            displayer.Write(in viewOptions, writer, enumerable);
            return writer.ToString();
        }

        /// <summary>
        /// 使用默认选项和默认展示器, 将渲染结果写入指定文本写入器.
        /// </summary>
        /// <param name="textWriter">目标文本写入器.</param>
        public void View(TextWriter textWriter) => enumerable.View(new ViewOptions(), textWriter, DefaultDisplayer.Instance);

        /// <summary>
        /// 使用指定选项和默认展示器, 将渲染结果写入指定文本写入器.
        /// </summary>
        /// <param name="viewOptions">渲染选项.</param>
        /// <param name="textWriter">目标文本写入器.</param>
        public void View(in ViewOptions viewOptions, TextWriter textWriter) => enumerable.View(viewOptions, textWriter, DefaultDisplayer.Instance);

        /// <summary>
        /// 使用指定选项和指定展示器, 将渲染结果写入指定文本写入器.
        /// </summary>
        /// <param name="viewOptions">渲染选项.</param>
        /// <param name="textWriter">目标文本写入器.</param>
        /// <param name="displayer">集合展示器.</param>
        public void View(in ViewOptions viewOptions, TextWriter textWriter, ICollectionDisplayer displayer) => displayer.Write(in viewOptions, textWriter, enumerable);

    }
}
