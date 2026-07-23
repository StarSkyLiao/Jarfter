//------------------------------------------------------------
// MiHoMiao
// Written by Mingxuan Liao.
// [Version] 1.0
//------------------------------------------------------------

using Jarfter.Core.Numerics.Noise.Calculators;
using Point = (int x, int y);
using Chunk = (Jarfter.Core.Numerics.Noise.Providers.NoiseTree2D? node, double value);

namespace Jarfter.Core.Numerics.Noise.Providers;

/// <summary>
/// 噪声地图, 包含了一张无限大小的原始噪声地图.
/// 使用一个前缀树, 每一个节点都是一个区块,
/// 前缀树的每一个链式区块的末端对应的就是某个特定坐标的值.
/// </summary>
/// <param name="seed">噪声图的种子.</param>
/// <param name="calculator">未缓存点使用的噪声计算器.</param>
public class NoiseTree2D(int seed, INoiseCalculator? calculator = null) : INoise2DProvider
{
    private const int ChunkSize = 16;
    private const long Mask = ChunkSize * ChunkSize - 1;
    private static readonly int s_Offset = (int)Math.Round(Math.Log2(ChunkSize)) * 2;

    private readonly Chunk[] m_NoiseMap = InitArray(ChunkSize * ChunkSize);

    /// <inheritdoc />
    public int NoiseSeed { get; } = seed;

    /// <inheritdoc />
    public INoiseCalculator Calculator { get; } = calculator ?? new HashNoiseCalculator();

    /// <inheritdoc />
    public double ValueAt(Point position)
    {
        return ValueAtInternal(PointToIndex(position), position);

        static ulong PointToIndex(Point point)
        {
            uint item1 = EncodeCoordinate(point.x);
            uint item2 = EncodeCoordinate(point.y);
            ulong result = 0;
            for (int i = 0; i < 32; ++i)
            {
                ulong mask = 1UL << i;
                result |= (mask & item1) << i | (mask & item2) << (i + 1);
            }

            return result;
        }

        // ZigZag 编码会将有符号坐标一一映射为无符号值, 再交织两个坐标的位以形成 Morton 索引.
        static uint EncodeCoordinate(int value) => (uint)(value << 1) ^ (uint)(value >> 31);
    }

    private double ValueAtInternal(ulong index, Point position)
    {
        if (index > Mask)
        {
            ref NoiseTree2D? node = ref m_NoiseMap[index & Mask].node;
            node ??= new NoiseTree2D(NoiseSeed, Calculator);
            return node.ValueAtInternal(index >> s_Offset, position);
        }
        ref double cached = ref m_NoiseMap[index].value;
        if (cached >= 0) return cached;
        cached = Calculator.Calculate(NoiseSeed, position);
        return cached;
    }

    private static Chunk[] InitArray(int size)
    {
        Chunk[] noiseMap = new Chunk[size];
        Array.Fill(noiseMap, (null, -1));
        return noiseMap;
    }

}
