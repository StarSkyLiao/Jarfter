using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Jarfter.Core.Collections.Generic;

/// <summary>
/// 表示基于红黑树实现的可重复有序集合.
/// 与不允许重复元素的 <see cref="SortedSet{T}"/> 不同, 此集合允许存储多个比较结果相等的元素,
/// 单次插入和删除的时间复杂度为 O(log N), 因此不实现 <see cref="ISet{T}"/>.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
public class MultiSortedSet<T> : ICollection<T>, ICollection, IReadOnlyCollection<T>
{
    #region 定义

    /// <summary>
    /// 表示红黑树节点的颜色.
    /// </summary>
    internal enum NodeColor : byte
    {
        /// <summary>
        /// 黑色节点.
        /// </summary>
        Black,

        /// <summary>
        /// 红色节点.
        /// </summary>
        Red
    }

    private delegate bool TreeWalkPredicate(Node node);

    /// <summary>
    /// 表示红黑树旋转的方向.
    /// </summary>
    internal enum TreeRotation : byte
    {
        /// <summary>
        /// 左旋.
        /// </summary>
        Left,

        /// <summary>
        /// 先左后右的双旋.
        /// </summary>
        LeftRight,

        /// <summary>
        /// 右旋.
        /// </summary>
        Right,

        /// <summary>
        /// 先右后左的双旋.
        /// </summary>
        RightLeft
    }

    #endregion

    #region 局部变量和常量

    private Node? m_Root;
    private int m_Version;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用默认比较器初始化 <see cref="MultiSortedSet{T}"/> 的新实例.
    /// </summary>
    public MultiSortedSet() => Comparer = Comparer<T>.Default;

    /// <summary>
    /// 使用指定比较器初始化 <see cref="MultiSortedSet{T}"/> 的新实例.
    /// </summary>
    /// <param name="comparer">用于排序元素的比较器. 为 <c>null</c> 时使用默认比较器.</param>
    public MultiSortedSet(IComparer<T>? comparer) => Comparer = comparer ?? Comparer<T>.Default;

    /// <summary>
    /// 使用指定集合中的元素和默认比较器初始化 <see cref="MultiSortedSet{T}"/> 的新实例.
    /// </summary>
    /// <param name="collection">要复制到集合中的元素.</param>
    public MultiSortedSet(IEnumerable<T> collection) : this(collection, Comparer<T>.Default) { }

    /// <summary>
    /// 使用指定集合中的元素和比较器初始化 <see cref="MultiSortedSet{T}"/> 的新实例.
    /// </summary>
    /// <param name="collection">要复制到集合中的元素.</param>
    /// <param name="comparer">用于排序元素的比较器. 为 <c>null</c> 时使用默认比较器.</param>
    public MultiSortedSet(IEnumerable<T> collection, IComparer<T>? comparer) : this(comparer)
    {
        switch (collection)
        {
            case null: throw new ArgumentNullException(nameof(collection));
            // 比较器相同时可直接深拷贝树, 避免重新排序和逐项插入.
            case MultiSortedSet<T> sortedSet when HasEqualComparer(sortedSet):
            {
                if (sortedSet.Count <= 0) return;
                Count = sortedSet.Count;
                m_Root = sortedSet.m_Root!.DeepClone(Count);
                return;
            }
        }

        T[] elements = collection.ToArray();
        int count = elements.Length;
        if (count <= 0) return;
        // 后续搜索直接调用 Comparer.Compare, 因此此处始终使用已标准化的比较器.
        comparer = Comparer;
        Array.Sort(elements, 0, count, comparer);

        m_Root = ConstructRootFromSortedArray(elements, 0, count - 1, null);
        Count = count;
    }

    #endregion

    #region 批量操作帮助程序

    /// <summary>
    /// 按中序遍历树, 并为每个节点调用委托.
    /// </summary>
    /// <param name="action">
    ///     要为每个节点调用的委托. 委托返回 <c>false</c> 时停止遍历.
    /// </param>
    /// <returns>
    /// 完成整棵树的遍历时为 <c>true</c>; 否则为 <c>false</c>.
    /// </returns>
    private void InOrderTreeWalk(TreeWalkPredicate action)
    {
        if (m_Root == null) return;

        // 红黑树的最大高度为 2 * log2(n + 1), 预设容量可避免栈增长时的额外数组分配.
        Stack<Node> stack = new Stack<Node>(2 * Log2(Count + 1));
        Node? current = m_Root;

        while (current != null)
        {
            stack.Push(current);
            current = current.Left;
        }

        while (stack.Count != 0)
        {
            current = stack.Pop();
            if (!action(current)) return;

            Node? node = current.Right;
            while (node != null)
            {
                stack.Push(node);
                node = node.Left;
            }
        }
    }

    /// <summary>
    /// 从左到右按广度遍历树, 并为每个节点调用委托.
    /// </summary>
    /// <param name="action">
    ///     要为每个节点调用的委托. 委托返回 <c>false</c> 时停止遍历.
    /// </param>
    /// <returns>
    /// 完成整棵树的遍历时为 <c>true</c>; 否则为 <c>false</c>.
    /// </returns>
    private void BreadthFirstTreeWalk(TreeWalkPredicate action)
    {
        if (m_Root == null) return;

        Queue<Node> processQueue = new Queue<Node>();
        processQueue.Enqueue(m_Root);

        while (processQueue.Count != 0)
        {
            Node current = processQueue.Dequeue();
            if (!action(current)) return;

            if (current.Left != null) processQueue.Enqueue(current.Left);
            if (current.Right != null) processQueue.Enqueue(current.Right);
        }
    }

    #endregion

    #region 属性

    /// <summary>
    /// 获取集合中的元素数.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// 获取用于排序元素的比较器.
    /// </summary>
    public IComparer<T> Comparer { get; }

    bool ICollection<T>.IsReadOnly => false;

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => this;

    #endregion

    #region ICollection<T> 成员

    /// <summary>
    /// 将元素添加到集合.
    /// </summary>
    /// <param name="item">要添加的元素.</param>
    public void Add(T item)
    {
        if (m_Root == null)
        {
            // 空树的根节点必须为黑色.
            m_Root = new Node(item, NodeColor.Black);
            Count = 1;
            m_Version++;
            return;
        }

        // 沿搜索路径拆分 4-节点, 以保证叶节点插入时不破坏红黑树约束.
        Node? current = m_Root;
        Node? parent = null;
        Node? grandParent = null;
        Node? greatGrandParent = null;

        // 搜索过程中可能发生旋转, 即使最终未插入也必须使现有枚举器失效.
        m_Version++;

        int order = 0;
        while (current != null)
        {
            order = Comparer.Compare(item, current.Item);

            // 将 4-节点拆分为两个 2-节点.
            if (current.Is4Node)
            {
                current.Split4Node();
                // 拆分可能产生连续红节点, 需要旋转恢复约束.
                if (Node.IsNonNullRed(parent))
                {
                    InsertionBalance(current, ref parent!, grandParent!, greatGrandParent!);
                }
            }

            greatGrandParent = grandParent;
            grandParent = parent;
            parent = current;
            current = order < 0 ? current.Left : current.Right;
        }

        // 在找到的叶位置插入新红节点.
        Node node = new Node(item, NodeColor.Red);
        if (order > 0) parent!.Right = node;
        else parent!.Left = node;

        // 新节点为红色, 父节点也为红色时需要重新平衡.
        if (parent.IsRed) InsertionBalance(node, ref parent, grandParent!, greatGrandParent!);

        // 根节点始终保持黑色.
        m_Root.ColorBlack();
        ++Count;
    }

    /// <summary>
    /// 从集合中移除第一个与指定元素比较相等的元素.
    /// </summary>
    /// <param name="item">要移除的元素.</param>
    /// <returns>成功移除元素时为 <c>true</c>; 否则为 <c>false</c>.</returns>
    public bool Remove(T item)
    {
        if (m_Root == null)
        {
            return false;
        }

        // 删除前沿搜索路径修复 2-节点, 确保目标节点及其后继节点可安全删除.

        // 搜索过程中可能发生旋转, 即使最终未删除也必须使现有枚举器失效.
        m_Version++;

        Node? current = m_Root;
        Node? parent = null;
        Node? grandParent = null;
        Node? match = null;
        Node? parentOfMatch = null;
        bool foundMatch = false;
        while (current != null)
        {
            if (current.Is2Node)
            {
                // 修复路径上的 2-节点.
                if (parent == null)
                {
                    // 根节点暂时标红, 以便与后续节点合并或旋转.
                    current.ColorRed();
                }
                else
                {
                    Node sibling = parent.GetSibling(current);
                    if (sibling.IsRed)
                    {
                        // 父节点为 3-节点时, 通过单旋转翻转红链接方向, 再转入后续分支.
                        if (parent.Right == sibling)
                        {
                            parent.RotateLeft();
                        }
                        else
                        {
                            parent.RotateRight();
                        }

                        parent.ColorRed();
                        sibling.ColorBlack(); // 红色父节点不能拥有黑色子节点.
                        // 旋转后 sibling 接替父节点位置, 必须更新祖父节点或根节点的链接.
                        ReplaceChildOrRoot(grandParent, parent, sibling);
                        // sibling 随后成为 current 的祖父节点.
                        grandParent = sibling;
                        if (parent == match) parentOfMatch = sibling;

                        sibling = parent.GetSibling(current);
                    }

                    if (sibling.Is2Node)
                    {
                        parent.Merge2Nodes();
                    }
                    else
                    {
                        // 同级为 3-或 4-节点时, 通过旋转将 current 转为红色节点.
                        Node newGrandParent = parent.Rotate(parent.GetRotation(current, sibling));

                        newGrandParent.Color = parent.Color;
                        parent.ColorBlack();
                        current.ColorRed();

                        ReplaceChildOrRoot(grandParent, parent, newGrandParent);
                        if (parent == match)
                        {
                            parentOfMatch = newGrandParent;
                        }
                    }
                }
            }

            // 找到匹配节点后仅继续向右查找后继节点, 无需再次比较.
            int order = foundMatch ? -1 : Comparer.Compare(item, current.Item);
            if (order == 0)
            {
                // 保存匹配节点及其父节点, 以便随后用后继节点替换.
                foundMatch = true;
                match = current;
                parentOfMatch = parent;
            }

            grandParent = parent;
            parent = current;

            // 找到匹配节点后继续在右子树中查找后继节点.
            current = order < 0 ? current.Left : current.Right;
        }

        // 将后继节点移动到匹配节点的位置并更新链接.
        if (match != null)
        {
            ReplaceNode(match, parentOfMatch!, parent!, grandParent!);
            --Count;
        }

        m_Root?.ColorBlack();
        return foundMatch;
    }

    /// <summary>
    /// 移除集合中的所有元素.
    /// </summary>
    public void Clear()
    {
        m_Root = null;
        Count = 0;
        ++m_Version;
    }

    /// <summary>
    /// 确定集合是否包含与指定元素比较相等的元素.
    /// </summary>
    /// <param name="item">要定位的元素.</param>
    /// <returns>集合包含元素时为 <c>true</c>; 否则为 <c>false</c>.</returns>
    public bool Contains(T item) => FindNode(item) != null;

    /// <summary>
    /// 将所有元素复制到指定数组.
    /// </summary>
    /// <param name="array">元素的目标数组.</param>
    public void CopyTo(T[] array) => CopyTo(array, 0, Count);

    /// <summary>
    /// 从指定索引开始, 将所有元素复制到指定数组.
    /// </summary>
    /// <param name="array">元素的目标数组.</param>
    /// <param name="index">数组中复制起始位置的从零开始索引.</param>
    public void CopyTo(T[] array, int index) => CopyTo(array, index, Count);

    /// <summary>
    /// 从指定索引开始, 将指定数量的元素复制到指定数组.
    /// </summary>
    /// <param name="array">元素的目标数组.</param>
    /// <param name="index">数组中复制起始位置的从零开始索引.</param>
    /// <param name="count">要复制的元素数量.</param>
    public void CopyTo(T[] array, int index, int count)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_NeedNonNegNum");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "ArgumentOutOfRange_NeedNonNegNum");
        }

        if (count > array.Length - index)
        {
            throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
        }

        // 将 count 转换为结束索引, 以便遍历时直接判断是否达到上界.
        count += index;

        InOrderTreeWalk(node =>
        {
            if (index >= count) return false;
            array[index++] = node.Item;
            return true;
        });
    }

    void ICollection.CopyTo(Array array, int index)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (array.Rank != 1)
        {
            throw new ArgumentException("Arg_RankMultiDimNotSupported", nameof(array));
        }

        if (array.GetLowerBound(0) != 0)
        {
            throw new ArgumentException("Arg_NonZeroLowerBound", nameof(array));
        }

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "ArgumentOutOfRange_NeedNonNegNum");
        }

        if (array.Length - index < Count)
        {
            throw new ArgumentException("Arg_ArrayPlusOffTooSmall");
        }

        if (array is T[] tArray)
        {
            CopyTo(tArray, index);
        }
        else
        {
            object?[]? objects = array as object[];
            if (objects == null)
            {
                throw new ArgumentException("Argument_InvalidArrayType", nameof(array));
            }

            try
            {
                InOrderTreeWalk(node =>
                {
                    objects[index++] = node.Item;
                    return true;
                });
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("Argument_InvalidArrayType", nameof(array));
            }
        }
    }

    #endregion

    #region IEnumerable<T> 成员

    /// <summary>
    /// 返回循环访问集合的枚举器.
    /// </summary>
    /// <returns>集合的枚举器.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region 树操作

    // 平衡后只需更新 current 和 parent. 下次需要拆分时, 祖先节点引用会重新正确赋值.
    private void InsertionBalance(Node current, ref Node parent, Node grandParent, Node greatGrandParent)
    {
        bool parentIsOnRight = grandParent.Right == parent;
        bool currentIsOnRight = parent.Right == current;

        Node newChildOfGreatGrandParent;
        if (parentIsOnRight == currentIsOnRight)
        {
            // 父节点与当前节点方向相同, 执行单旋转.
            newChildOfGreatGrandParent = currentIsOnRight ? grandParent.RotateLeft() : grandParent.RotateRight();
        }
        else
        {
            // 父节点与当前节点方向不同, 执行双旋转.
            newChildOfGreatGrandParent = currentIsOnRight ? grandParent.RotateLeftRight() : grandParent.RotateRightLeft();
            // 旋转后当前节点成为曾祖父节点的子节点.
            parent = greatGrandParent;
        }

        // 祖父节点将成为父节点或当前节点的子节点, 因此必须标红.
        grandParent.ColorRed();
        newChildOfGreatGrandParent.ColorBlack();

        ReplaceChildOrRoot(greatGrandParent, grandParent, newChildOfGreatGrandParent);
    }

    /// <summary>
    /// 替换父节点的子节点; 父节点为 <c>null</c> 时替换根节点.
    /// </summary>
    /// <param name="parent">父节点, 可以为 <c>null</c>.</param>
    /// <param name="child">要替换的子节点.</param>
    /// <param name="newChild">用于替换 <paramref name="child"/> 的节点.</param>
    private void ReplaceChildOrRoot(Node? parent, Node child, Node newChild)
    {
        if (parent != null) parent.ReplaceChild(child, newChild);
        else m_Root = newChild;
    }

    /// <summary>
    /// 使用后继节点替换匹配节点.
    /// </summary>
    private void ReplaceNode(Node match, Node parentOfMatch, Node successor, Node parentOfSuccessor)
    {
        if (successor == match)
        {
            // 匹配节点没有右子树, 其左子节点可直接替代它.
            successor = match.Left!;
        }
        else
        {
            successor.Right?.ColorBlack();

            if (parentOfSuccessor != match)
            {
                // 将后继节点从原父节点分离, 并连接匹配节点的右子树.
                parentOfSuccessor.Left = successor.Right;
                successor.Right = match.Right;
            }

            successor.Left = match.Left;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (successor != null) successor.Color = match.Color;

        ReplaceChildOrRoot(parentOfMatch, match, successor!);
    }

    /// <summary>
    /// 查找与指定元素比较相等的节点.
    /// </summary>
    /// <param name="item">要查找的元素.</param>
    /// <returns>找到的节点; 未找到时为 <c>null</c>.</returns>
    internal Node? FindNode(T item)
    {
        Node? current = m_Root;
        while (current != null)
        {
            int order = Comparer.Compare(item, current.Item);
            if (order == 0) return current;

            current = order < 0 ? current.Left : current.Right;
        }

        return null;
    }

    private Node? FindRange(T? from, T? to) => FindRange(from, to, lowerBoundActive: true, upperBoundActive: true);

    private Node? FindRange(T? from, T? to, bool lowerBoundActive, bool upperBoundActive)
    {
        Node? current = m_Root;
        while (current != null)
        {
            if (lowerBoundActive && Comparer.Compare(from, current.Item) > 0)
            {
                current = current.Right;
            }
            else
            {
                if (upperBoundActive && Comparer.Compare(to, current.Item) < 0)
                    current = current.Left;
                else return current;
            }
        }

        return null;
    }

    /// <summary>
    /// 递增集合版本号, 使现有枚举器失效.
    /// </summary>
    internal void UpdateVersion() => ++m_Version;

    /// <summary>
    /// 确定两个 <see cref="MultiSortedSet{T}"/> 实例是否使用相同的比较器.
    /// 默认比较器通常引用相等, 先进行引用比较可避免不必要的 <see cref="object.Equals(object?)"/> 调用.
    /// </summary>
    /// <param name="other">要比较的另一个 <see cref="MultiSortedSet{T}"/>.</param>
    /// <returns>两个集合使用相同比较器时为 <c>true</c>; 否则为 <c>false</c>.</returns>
    private bool HasEqualComparer(MultiSortedSet<T> other) =>
        ReferenceEquals(Comparer, other.Comparer) || Comparer.Equals(other.Comparer);

    #endregion

    #region 集合操作

    private static Node? ConstructRootFromSortedArray(T[] arr, int startIndex, int endIndex, Node? redNode)
    {
        // 元素数为偶数时选取左中位数作为根, 将右中位数作为红节点插入右子树最左侧.
        // 此构造方式在保持红黑树约束的同时避免逐项插入; 递归实现的栈开销也小于迭代实现的额外存储.

        int size = endIndex - startIndex + 1;
        Node root;

        switch (size)
        {
            case 0:
                return null;
            case 1:
                root = new Node(arr[startIndex], NodeColor.Black);
                if (redNode != null) root.Left = redNode;
                break;
            case 2:
                root = new Node(arr[startIndex], NodeColor.Black)
                {
                    Right = new Node(arr[endIndex], NodeColor.Black)
                };
                root.Right.ColorRed();
                if (redNode != null) root.Left = redNode;
                break;
            case 3:
                root = new Node(arr[startIndex + 1], NodeColor.Black)
                {
                    Left = new Node(arr[startIndex], NodeColor.Black),
                    Right = new Node(arr[endIndex], NodeColor.Black)
                };
                if (redNode != null) root.Left.Left = redNode;
                break;
            default:
                int midIndex = (startIndex + endIndex) / 2;
                root = new Node(arr[midIndex], NodeColor.Black)
                {
                    Left = ConstructRootFromSortedArray(arr, startIndex, midIndex - 1, redNode),
                    Right = size % 2 == 0 ?
                        ConstructRootFromSortedArray(arr, midIndex + 2, endIndex, new Node(arr[midIndex + 1], NodeColor.Red)) :
                        ConstructRootFromSortedArray(arr, midIndex + 1, endIndex, null)
                };
                break;

        }

        return root;
    }

    /// <summary>
    /// 移除满足指定条件的所有元素.
    /// </summary>
    /// <param name="match">用于测试元素的谓词.</param>
    /// <returns>实际移除的元素数量.</returns>
    public int RemoveWhere(Predicate<T> match)
    {
        if (match == null)
        {
            throw new ArgumentNullException(nameof(match));
        }
        List<T> matches = new List<T>(Count);

        BreadthFirstTreeWalk(n =>
        {
            if (match(n.Item)) matches.Add(n.Item);
            return true;
        });

        // 逆序删除广度优先遍历结果, 尽可能减少后续删除的重平衡开销.
        int actuallyRemoved = 0;
        for (int i = matches.Count - 1; i >= 0; i--)
        {
            if (Remove(matches[i])) actuallyRemoved++;
        }

        return actuallyRemoved;
    }

    #endregion

    #region 有序集合成员

    /// <summary>
    /// 获取集合中的最小元素; 集合为空时为默认值.
    /// </summary>
    public T? Min => MinInternal;

    private T? MinInternal
    {
        get
        {
            if (m_Root == null) return default;
            Node current = m_Root;
            while (current.Left != null) current = current.Left;
            return current.Item;
        }
    }

    /// <summary>
    /// 获取集合中的最大元素; 集合为空时为默认值.
    /// </summary>
    public T? Max => MaxInternal;

    private T? MaxInternal
    {
        get
        {
            if (m_Root == null) return default;
            Node current = m_Root;
            while (current.Right != null) current = current.Right;
            return current.Item;
        }
    }

    /// <summary>
    /// 返回按降序循环访问集合的序列.
    /// </summary>
    /// <returns>按降序排列的元素序列.</returns>
    public IEnumerable<T> Reverse()
    {
        Enumerator enumerator = new Enumerator(this, reverse: true);
        while (enumerator.MoveNext()) yield return enumerator.Current;
    }

    /// <summary>
    /// 返回位于指定闭区间内的元素序列.
    /// </summary>
    /// <param name="lowerValue">范围的下限.</param>
    /// <param name="upperValue">范围的上限.</param>
    /// <returns>位于指定范围内的元素序列.</returns>
    public IEnumerable<T> GetViewBetween(T? lowerValue, T? upperValue)
    {
        if (Comparer.Compare(lowerValue, upperValue) > 0)
            throw new ArgumentException("SortedSet_LowerValueGreaterThanUpperValue", nameof(lowerValue));

        if (Comparer.Compare(lowerValue, Max) > 0) yield break;
        if (Comparer.Compare(upperValue, Min) < 0) yield break;

        using var enumerator = new Enumerator(this, FindRange(lowerValue, upperValue));
        do
        {
            if (Comparer.Compare(enumerator.Current, lowerValue) < 0) continue;
            if (Comparer.Compare(enumerator.Current, upperValue) > 0) break;
            yield return enumerator.Current;
        } while (enumerator.MoveNext());
    }

    #endregion

    #region 帮助类型

    /// <summary>
    /// 表示红黑树中的一个节点.
    /// </summary>
    internal sealed class Node(T item, NodeColor color)
    {
        /// <summary>
        /// 确定节点是否非空且为黑色.
        /// </summary>
        /// <param name="node">要检查的节点.</param>
        /// <returns>节点非空且为黑色时为 <c>true</c>; 否则为 <c>false</c>.</returns>
        public static bool IsNonNullBlack(Node? node) => node is { IsBlack: true };

        /// <summary>
        /// 确定节点是否非空且为红色.
        /// </summary>
        /// <param name="node">要检查的节点.</param>
        /// <returns>节点非空且为红色时为 <c>true</c>; 否则为 <c>false</c>.</returns>
        public static bool IsNonNullRed(Node? node) => node is { IsRed: true };

        /// <summary>
        /// 确定节点是否为空或为黑色.
        /// </summary>
        /// <param name="node">要检查的节点.</param>
        /// <returns>节点为空或为黑色时为 <c>true</c>; 否则为 <c>false</c>.</returns>
        public static bool IsNullOrBlack(Node? node) => node == null || node.IsBlack;

        /// <summary>
        /// 获取或设置节点存储的元素.
        /// </summary>
        public T Item { get; set; } = item;

        /// <summary>
        /// 获取或设置左子节点.
        /// </summary>
        public Node? Left { get; set; }

        /// <summary>
        /// 获取或设置右子节点.
        /// </summary>
        public Node? Right { get; set; }

        /// <summary>
        /// 获取或设置节点颜色.
        /// </summary>
        public NodeColor Color { get; set; } = color;

        /// <summary>
        /// 获取一个值, 指示节点是否为黑色.
        /// </summary>
        public bool IsBlack => Color == NodeColor.Black;

        /// <summary>
        /// 获取一个值, 指示节点是否为红色.
        /// </summary>
        public bool IsRed => Color == NodeColor.Red;

        /// <summary>
        /// 获取一个值, 指示节点是否为 2-节点.
        /// </summary>
        public bool Is2Node => IsBlack && IsNullOrBlack(Left) && IsNullOrBlack(Right);

        /// <summary>
        /// 获取一个值, 指示节点是否为 4-节点.
        /// </summary>
        public bool Is4Node => IsNonNullRed(Left) && IsNonNullRed(Right);

        /// <summary>
        /// 将节点标记为黑色.
        /// </summary>
        public void ColorBlack() => Color = NodeColor.Black;

        /// <summary>
        /// 将节点标记为红色.
        /// </summary>
        public void ColorRed() => Color = NodeColor.Red;

        /// <summary>
        /// 深度复制以当前节点为根的树.
        /// </summary>
        /// <param name="count">树中的节点数.</param>
        /// <returns>复制后树的根节点.</returns>
        public Node DeepClone(int count)
        {
            // 使用栈模拟遍历, 以避免深树递归造成的调用栈增长.
            Stack<Node> originalNodes = new Stack<Node>(2 * Log2(count) + 2);
            Stack<Node> newNodes = new Stack<Node>(2 * Log2(count) + 2);
            Node newRoot = ShallowClone();

            Node? originalCurrent = this;
            Node newCurrent = newRoot;

            while (originalCurrent != null)
            {
                originalNodes.Push(originalCurrent);
                newNodes.Push(newCurrent);
                newCurrent.Left = originalCurrent.Left?.ShallowClone();
                originalCurrent = originalCurrent.Left;
                newCurrent = newCurrent.Left!;
            }

            while (originalNodes.Count != 0)
            {
                originalCurrent = originalNodes.Pop();
                newCurrent = newNodes.Pop();

                Node? originalRight = originalCurrent.Right;
                Node? newRight = originalRight?.ShallowClone();
                newCurrent.Right = newRight;

                while (originalRight != null)
                {
                    originalNodes.Push(originalRight);
                    newNodes.Push(newRight!);
                    newRight!.Left = originalRight.Left?.ShallowClone();
                    originalRight = originalRight.Left;
                    newRight = newRight.Left;
                }
            }

            return newRoot;
        }

        /// <summary>
        /// 获取删除期间此节点应执行的旋转方向.
        /// </summary>
        /// <param name="current">当前路径上的节点.</param>
        /// <param name="sibling">当前节点的兄弟节点.</param>
        /// <returns>用于恢复红黑树约束的旋转方向.</returns>
        internal TreeRotation GetRotation(Node current, Node sibling)
        {
            bool currentIsLeftChild = Left == current;
            return IsNonNullRed(sibling.Left) ?
                currentIsLeftChild ? TreeRotation.RightLeft : TreeRotation.Right :
                currentIsLeftChild ? TreeRotation.Left : TreeRotation.LeftRight;
        }

        /// <summary>
        /// 获取指定子节点的兄弟节点.
        /// </summary>
        /// <param name="node">要查找其兄弟节点的子节点.</param>
        /// <returns>指定子节点的兄弟节点.</returns>
        public Node GetSibling(Node node) => node == Left ? Right! : Left!;

        /// <summary>
        /// 创建当前节点的浅副本.
        /// </summary>
        /// <returns>不包含子节点的新节点.</returns>
        public Node ShallowClone() => new Node(Item, Color);

        /// <summary>
        /// 将 4-节点拆分为两个 2-节点.
        /// </summary>
        public void Split4Node()
        {
            ColorRed();
            Left!.ColorBlack();
            Right!.ColorBlack();
        }

        /// <summary>
        /// 对当前子树执行指定旋转. 旋转可能将孙节点从红色改为黑色.
        /// </summary>
        /// <param name="rotation">要执行的旋转方向.</param>
        /// <returns>旋转后子树的新根节点.</returns>
        public Node Rotate(TreeRotation rotation)
        {
            Node removeRed;
            switch (rotation)
            {
                case TreeRotation.Right:
                    removeRed = Left!.Left!;
                    removeRed.ColorBlack();
                    return RotateRight();
                case TreeRotation.Left:
                    removeRed = Right!.Right!;
                    removeRed.ColorBlack();
                    return RotateLeft();
                case TreeRotation.RightLeft:
                    return RotateRightLeft();
                case TreeRotation.LeftRight:
                    return RotateLeftRight();
                default:
                    return null!;
            }
        }

        /// <summary>
        /// 对当前子树执行左旋, 使当前节点成为原右子节点的左子节点.
        /// </summary>
        /// <returns>旋转后子树的新根节点.</returns>
        public Node RotateLeft()
        {
            Node child = Right!;
            Right = child.Left;
            child.Left = this;
            return child;
        }

        /// <summary>
        /// 对当前子树执行先左后右的双旋.
        /// </summary>
        /// <returns>旋转后子树的新根节点.</returns>
        public Node RotateLeftRight()
        {
            Node child = Left!;
            Node grandChild = child.Right!;

            Left = grandChild.Right;
            grandChild.Right = this;
            child.Right = grandChild.Left;
            grandChild.Left = child;
            return grandChild;
        }

        /// <summary>
        /// 对当前子树执行右旋, 使当前节点成为原左子节点的右子节点.
        /// </summary>
        /// <returns>旋转后子树的新根节点.</returns>
        public Node RotateRight()
        {
            Node child = Left!;
            Left = child.Right;
            child.Right = this;
            return child;
        }

        /// <summary>
        /// 对当前子树执行先右后左的双旋.
        /// </summary>
        /// <returns>旋转后子树的新根节点.</returns>
        public Node RotateRightLeft()
        {
            Node child = Right!;
            Node grandChild = child.Left!;

            Right = grandChild.Left;
            grandChild.Left = this;
            child.Left = grandChild.Right;
            grandChild.Right = child;
            return grandChild;
        }

        /// <summary>
        /// 将两个 2-节点合并为一个 4-节点.
        /// </summary>
        public void Merge2Nodes()
        {
            // 将父节点和两个黑色子节点重新着色为 4-节点.
            ColorBlack();
            Left!.ColorRed();
            Right!.ColorRed();
        }

        /// <summary>
        /// 使用新节点替换当前节点的子节点.
        /// </summary>
        /// <param name="child">要替换的子节点.</param>
        /// <param name="newChild">用于替换 <paramref name="child"/> 的节点.</param>
        public void ReplaceChild(Node child, Node newChild)
        {
            if (Left == child) Left = newChild;
            else Right = newChild;
        }
    }

    /// <summary>
    /// 表示用于循环访问 <see cref="MultiSortedSet{T}"/> 的枚举器.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly MultiSortedSet<T> m_Tree;
        private readonly int m_Version;

        private readonly Stack<Node> m_Stack;
        private Node? m_Current;

        private readonly bool m_Reverse;

        /// <summary>
        /// 初始化 <see cref="Enumerator"/> 的新实例.
        /// </summary>
        /// <param name="set">要枚举的集合.</param>
        /// <param name="reverse">是否按降序枚举.</param>
        internal Enumerator(MultiSortedSet<T> set, bool reverse = false)
        {
            m_Tree = set;
            m_Version = set.m_Version;

            // 红黑树最大高度为 2 * log2(n + 1), 可据此预设栈容量.
            m_Stack = new Stack<Node>(2 * Log2(set.Count + 1));
            m_Current = null;
            m_Reverse = reverse;

            Initialize();
        }

        /// <summary>
        /// 初始化从指定节点开始的 <see cref="Enumerator"/> 新实例.
        /// </summary>
        /// <param name="set">要枚举的集合.</param>
        /// <param name="current">枚举起始节点.</param>
        internal Enumerator(MultiSortedSet<T> set, Node? current)
        {
            m_Tree = set;
            m_Version = set.m_Version;

            // 红黑树最大高度为 2 * log2(n + 1), 可据此预设栈容量.
            m_Stack = new Stack<Node>(2 * Log2(set.Count + 1));
            m_Current = current;
            m_Reverse = false;

            Initialize();
        }

        private void Initialize()
        {
            m_Current = null;
            Node? node = m_Tree.m_Root;
            while (node != null)
            {
                Node? next = m_Reverse ? node.Right : node.Left;
                m_Stack.Push(node);
                node = next;
            }
        }

        /// <summary>
        /// 将枚举器推进到集合中的下一个元素.
        /// </summary>
        /// <returns>枚举器已成功推进到下一个元素时为 <c>true</c>; 否则为 <c>false</c>.</returns>
        public bool MoveNext()
        {
            if (m_Version != m_Tree.m_Version)
            {
                throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
            }

            if (m_Stack.Count == 0)
            {
                m_Current = null;
                return false;
            }

            m_Current = m_Stack.Pop();
            Node? node = m_Reverse ? m_Current.Left : m_Current.Right;
            while (node != null)
            {
                Node? next = m_Reverse ? node.Right : node.Left;
                m_Stack.Push(node);
                node = next;
            }
            return true;
        }

        /// <summary>
        /// 释放枚举器使用的资源.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// 获取枚举器当前位置的元素.
        /// </summary>
        // 仅在枚举器位于无效位置时为 null; 此时访问 Current 的行为未定义.
        public T Current => m_Current != null ? m_Current.Item : default!;

        object? IEnumerator.Current =>
            m_Current == null
                ? throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen")
                : m_Current.Item;

        /// <summary>
        /// 将枚举器重置到初始位置.
        /// </summary>
        internal void Reset()
        {
            if (m_Version != m_Tree.m_Version)
                throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");

            m_Stack.Clear();
            Initialize();
        }

        void IEnumerator.Reset() => Reset();
    }

    #endregion

    #region 其他

    /// <summary>
    /// 在集合中查找指定值, 并返回找到的比较相等的实际值.
    /// </summary>
    /// <param name="equalValue">要查找的值.</param>
    /// <param name="actualValue">搜索找到的集合元素; 未找到时为 <typeparamref name="T"/> 的默认值.</param>
    /// <returns>搜索成功时为 <c>true</c>; 否则为 <c>false</c>.</returns>
    /// <remarks>
    /// 当需要复用已存储的引用, 或比较器认为相等但集合值包含更完整数据时, 此方法尤其有用.
    /// </remarks>
    public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
    {
        Node? node = FindNode(equalValue);
        if (node != null)
        {
            actualValue = node.Item;
            return true;
        }
        actualValue = default;
        return false;
    }

    // 用于依赖元素计数的集合检查操作.
    private static int Log2(int value)
    {
        int result = 0;
        while (value > 0)
        {
            result++;
            value >>= 1;
        }
        return result;
    }

    #endregion
}
