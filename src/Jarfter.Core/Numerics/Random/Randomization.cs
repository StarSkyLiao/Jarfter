namespace Jarfter.Core.Numerics.Random;

/// <summary>
/// 提供 <see cref="System.Random"/> 的常用随机操作.
/// <para>未显式传入随机数生成器的方法共享内部实例. 调用 <see cref="SetSeed"/> 可重置该实例的序列.</para>
/// </summary>
public static class Randomization
{
    private static System.Random s_Random = new System.Random();

    /// <summary>
    /// 重置共享随机数生成器的种子.
    /// </summary>
    /// <param name="seed">用于初始化共享随机数生成器的种子.</param>
    public static void SetSeed(int seed) => s_Random = new System.Random(seed);

    /// <summary>
    /// 获取位于 [0, 1) 的单精度浮点数.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <returns>位于 [0, 1) 的单精度浮点数.</returns>
    public static float Single(this System.Random self)
    {
        ArgumentNullException.ThrowIfNull(self);
        return self.NextSingle();
    }

    /// <summary>
    /// 从共享随机数生成器获取位于 [0, 1) 的单精度浮点数.
    /// </summary>
    /// <returns>位于 [0, 1) 的单精度浮点数.</returns>
    public static float Single() => s_Random.NextSingle();

    /// <summary>
    /// 获取位于 [0, 1) 的双精度浮点数.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <returns>位于 [0, 1) 的双精度浮点数.</returns>
    public static double Double(this System.Random self)
    {
        ArgumentNullException.ThrowIfNull(self);
        return self.NextDouble();
    }

    /// <summary>
    /// 从共享随机数生成器获取位于 [0, 1) 的双精度浮点数.
    /// </summary>
    /// <returns>位于 [0, 1) 的双精度浮点数.</returns>
    public static double Double() => s_Random.NextDouble();

    /// <summary>
    /// 从共享随机数生成器获取指定单精度浮点数范围内的值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的单精度浮点数.</returns>
    public static float Range(float minInclusive, float maxExclusive) => Range(s_Random, minInclusive, maxExclusive);

    /// <summary>
    /// 获取指定单精度浮点数范围内的值.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的单精度浮点数.</returns>
    public static float Range(this System.Random self, float minInclusive, float maxExclusive)
    {
        ArgumentNullException.ThrowIfNull(self);
        return self.NextSingle() * (maxExclusive - minInclusive) + minInclusive;
    }

    /// <summary>
    /// 从共享随机数生成器获取指定双精度浮点数范围内的值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的双精度浮点数.</returns>
    public static double Range(double minInclusive, double maxExclusive) => Range(s_Random, minInclusive, maxExclusive);

    /// <summary>
    /// 获取指定双精度浮点数范围内的值.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的双精度浮点数.</returns>
    public static double Range(this System.Random self, double minInclusive, double maxExclusive)
    {
        ArgumentNullException.ThrowIfNull(self);
        return self.NextDouble() * (maxExclusive - minInclusive) + minInclusive;
    }

    /// <summary>
    /// 从共享随机数生成器获取指定整数范围内的值.
    /// </summary>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的整数.</returns>
    public static int Range(int minInclusive, int maxExclusive) => s_Random.Next(minInclusive, maxExclusive);

    /// <summary>
    /// 获取指定整数范围内的值.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="minInclusive">返回值的最小值.</param>
    /// <param name="maxExclusive">返回值的上限, 不包含该值.</param>
    /// <returns>位于 [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>) 的整数.</returns>
    public static int Range(this System.Random self, int minInclusive, int maxExclusive)
    {
        ArgumentNullException.ThrowIfNull(self);
        return self.Next(minInclusive, maxExclusive);
    }

    /// <summary>
    /// 为单精度浮点数添加绝对随机偏差.
    /// </summary>
    /// <param name="self">基准值.</param>
    /// <param name="delta">允许的最大偏差.</param>
    /// <returns>位于 [<paramref name="self"/> - <paramref name="delta"/>, <paramref name="self"/> + <paramref name="delta"/>) 的值.</returns>
    public static float Delta(this float self, float delta) => Range(s_Random, self - delta, self + delta);

    /// <summary>
    /// 为双精度浮点数添加绝对随机偏差.
    /// </summary>
    /// <param name="self">基准值.</param>
    /// <param name="delta">允许的最大偏差.</param>
    /// <returns>位于 [<paramref name="self"/> - <paramref name="delta"/>, <paramref name="self"/> + <paramref name="delta"/>) 的值.</returns>
    public static double Delta(this double self, double delta) => Range(s_Random, self - delta, self + delta);

    /// <summary>
    /// 使用指定生成器为单精度浮点数添加绝对随机偏差.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="value">基准值.</param>
    /// <param name="delta">允许的最大偏差.</param>
    /// <returns>带随机偏差后的值.</returns>
    public static float Delta(this System.Random self, float value, float delta) => self.Range(value - delta, value + delta);

    /// <summary>
    /// 使用指定生成器为双精度浮点数添加绝对随机偏差.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="value">基准值.</param>
    /// <param name="delta">允许的最大偏差.</param>
    /// <returns>带随机偏差后的值.</returns>
    public static double Delta(this System.Random self, double value, double delta) => self.Range(value - delta, value + delta);

    /// <summary>
    /// 为单精度浮点数添加百分比随机偏差.
    /// </summary>
    /// <param name="self">基准值.</param>
    /// <param name="delta">允许的最大偏差比例.</param>
    /// <returns>带随机偏差后的值.</returns>
    public static float DeltaPct(this float self, float delta) => Range(s_Random, self * (1 - delta), self * (1 + delta));

    /// <summary>
    /// 使用指定生成器为单精度浮点数添加百分比随机偏差.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="value">基准值.</param>
    /// <param name="delta">允许的最大偏差比例.</param>
    /// <returns>带随机偏差后的值.</returns>
    public static float DeltaPct(this System.Random self, float value, float delta) => self.Range(value * (1 - delta), value * (1 + delta));

    /// <summary>
    /// 按指定概率返回结果.
    /// </summary>
    /// <param name="chance">返回 <see langword="true"/> 的概率.</param>
    /// <returns>本次随机尝试是否成功.</returns>
    public static bool Try(float chance) => chance > s_Random.Single();

    /// <summary>
    /// 按指定概率返回结果.
    /// </summary>
    /// <param name="chance">返回 <see langword="true"/> 的概率.</param>
    /// <returns>本次随机尝试是否成功.</returns>
    public static bool Try(double chance) => chance > s_Random.Double();

    /// <summary>
    /// 使用指定生成器按指定概率返回结果.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="chance">返回 <see langword="true"/> 的概率.</param>
    /// <returns>本次随机尝试是否成功.</returns>
    public static bool Try(this System.Random self, float chance) => chance > self.Range(0f, 1f);

    /// <summary>
    /// 从数组中随机选取一个元素.
    /// </summary>
    /// <param name="self">要选取元素的数组.</param>
    /// <returns>随机选取的元素; 当数组为空时返回默认值.</returns>
    public static T? RandomSelect<T>(this T[] self)
    {
        ArgumentNullException.ThrowIfNull(self);
        return self.Length == 0 ? default : self[s_Random.Next(self.Length)];
    }

    /// <summary>
    /// 从序列中随机选取一个元素.
    /// </summary>
    /// <param name="enumerable">要选取元素的序列.</param>
    /// <returns>随机选取的元素; 当序列为空时返回默认值.</returns>
    public static T? Select<T>(IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);
        T[] entries = enumerable as T[] ?? [.. enumerable];
        return entries.Length == 0 ? default : entries[s_Random.Next(entries.Length)];
    }

    /// <summary>
    /// 使用指定生成器从序列中随机选取一个元素.
    /// </summary>
    /// <param name="self">要使用的随机数生成器.</param>
    /// <param name="enumerable">要选取元素的序列.</param>
    /// <returns>随机选取的元素; 当序列为空时返回默认值.</returns>
    public static T? Select<T>(this System.Random self, IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(self);
        ArgumentNullException.ThrowIfNull(enumerable);
        T[] entries = enumerable as T[] ?? [.. enumerable];
        return entries.Length == 0 ? default : entries[self.Range(0, entries.Length)];
    }

    /// <summary>
    /// 原地随机打乱跨度中的元素.
    /// </summary>
    /// <param name="self">要打乱的元素跨度.</param>
    public static void Shuffle<T>(this Span<T> self)
    {
        for (int index = self.Length - 1; index > 0; index--)
        {
            int swapIndex = s_Random.Next(index + 1);
            (self[index], self[swapIndex]) = (self[swapIndex], self[index]);
        }
    }

    /// <summary>
    /// 原地随机打乱数组中的元素.
    /// </summary>
    /// <param name="self">要打乱的数组.</param>
    public static void Shuffle<T>(this T[] self)
    {
        ArgumentNullException.ThrowIfNull(self);
        Shuffle(self.AsSpan());
    }

    /// <summary>
    /// 使用指定生成器原地随机打乱数组中的元素.
    /// </summary>
    /// <param name="self">要打乱的数组.</param>
    /// <param name="random">要使用的随机数生成器.</param>
    public static void Shuffle<T>(this T[] self, System.Random random)
    {
        ArgumentNullException.ThrowIfNull(self);
        ArgumentNullException.ThrowIfNull(random);

        for (int index = self.Length - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (self[index], self[swapIndex]) = (self[swapIndex], self[index]);
        }
    }
}
