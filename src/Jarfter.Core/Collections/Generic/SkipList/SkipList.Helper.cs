using System.Diagnostics;

namespace Jarfter.Core.Collections.Generic;

public partial class SkipList<T>
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Random s_Random = new Random();

    private static int GetRandomLevel()
    {
        int level = 1;
        while (level < MaxLevel &&
               (s_Random.Next(0, short.MaxValue) & ushort.MaxValue) < Probability * ushort.MaxValue
              ) ++level;
        return level;
    }

    /// <summary>
    /// 表示跳表中的节点, 其中包含各层的后继链接和跨度信息.
    /// </summary>
    [DebuggerDisplay("Node [{Value}]")]
    public class SkipListNode
    {
        /// <summary>
        /// 初始化 <see cref="Jarfter.Core.Collections.Generic.SkipList{T}.SkipListNode"/> 的新实例.
        /// </summary>
        /// <param name="level">节点的层数.</param>
        /// <param name="value">节点存储的元素.</param>
        public SkipListNode(int level, T value)
        {
            Forward = new (SkipListNode? Next, int Span)[level];
            Value = value;
        }

        /// <summary>
        /// 获取各层的后继节点及跨度信息.
        /// </summary>
        public readonly (SkipListNode? Next, int Span)[] Forward;

        /// <summary>
        /// 获取或设置节点存储的元素.
        /// </summary>
        public T Value { get; internal set; }
    }
}
