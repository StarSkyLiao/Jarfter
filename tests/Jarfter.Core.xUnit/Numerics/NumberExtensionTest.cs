using System.Numerics;
using Jarfter.Core.Numerics;

namespace Jarfter.Core.xUnit.Numerics;

public sealed class NumberExtensionTest
{
    [Fact]
    public void IntExtension_ShouldMatchExpectedBehavior()
    {
        Assert.Equal(5, (-5).Abs());
        Assert.Equal(-1, (-5).Sign());
        Assert.Equal(0, 0.Sign());
        Assert.Equal(1, 5.Sign());
        Assert.Equal(1024, 2.Pow(10));
        Assert.Equal(1, 2.Pow(-3));

        Assert.Equal(0, (-3).Min(0));
        Assert.Equal(5, 7.Max(5));

        Assert.Equal(0, (-1).Clamp01());
        Assert.Equal(1, 2.Clamp01());

        Assert.Equal(0, (-5).Clamp(0, 10));
        Assert.Equal(10, 15.Clamp(0, 10));
        Assert.Equal(7, 7.Clamp(0, 10));

        Assert.Equal(9, (-1).CircleClamp(0, 10));
        Assert.Equal(0, 10.CircleClamp(0, 10));
        Assert.Equal(3, 23.CircleClamp(0, 10));
        Assert.Equal(4, 4.CircleClamp(0, 10));
    }

    [Fact]
    public void LongExtension_ShouldMatchExpectedBehavior()
    {
        const long baseValue = 2;
        Assert.Equal(1_125_899_906_842_624L, baseValue.Pow(50));
        Assert.Equal(1L, baseValue.Pow(-5));
        Assert.Equal(0L, (-9L).Min(0L));
        Assert.Equal(5L, 9L.Max(5L));
        Assert.Equal(7L, 17L.CircleClamp(0L, 10L));
    }

    [Fact]
    public void FloatAndDoubleExtension_ShouldMatchExpectedBehavior()
    {
        Assert.Equal(8f, 2f.Pow(3));
        Assert.Equal(1f, 2f.Pow(-3));
        Assert.Equal(2.5f, 12.5f.CircleClamp(0f, 10f), 6);

        Assert.Equal(8d, 2d.Pow(3));
        Assert.Equal(1d, 2d.Pow(-3));
        Assert.Equal(2.5d, 12.5d.CircleClamp(0d, 10d), 12);
    }

    [Fact]
    public void DecimalExtension_ShouldMatchExpectedBehavior()
    {
        const decimal value = -123.45m;
        Assert.Equal(123.45m, value.Abs());
        Assert.Equal(-1, value.Sign());
        Assert.Equal(81m, 3m.Pow(4));
        Assert.Equal(1m, 3m.Pow(-1));
        Assert.Equal(0.25m, 1.25m.CircleClamp(0m, 1m));
    }

    [Fact]
    public void GenericExtension_WithBigInteger_ShouldSupportBigNumberCase()
    {
        BigInteger big = BigInteger.Parse("123456789012345678901234567890");
        BigInteger negativeBig = -big;

        Assert.Equal(big, negativeBig.Abs());
        Assert.Equal(-1, negativeBig.Sign());
        Assert.Equal(1, big.Sign());
        Assert.Equal(BigInteger.Pow(new BigInteger(12), 20), new BigInteger(12).Pow(20));
        Assert.Equal(BigInteger.One, new BigInteger(12).Pow(-2));

        Assert.Equal(big, big.Min(BigInteger.Zero));
        Assert.Equal(big, big.Max(big + 1));
        Assert.Equal(BigInteger.One, big.Clamp01());
        Assert.Equal(big + 10, (big + 23).CircleClamp(big, big + 13));
        Assert.Equal(big + 10, (big - 3).CircleClamp(big, big + 13));
    }

    [Fact]
    public void CircleClamp_WithInvalidRange_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => 1.CircleClamp(5, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => 1L.CircleClamp(8L, 8L));
        Assert.Throws<ArgumentOutOfRangeException>(() => 1f.CircleClamp(2f, 2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => 1d.CircleClamp(3d, 3d));
        Assert.Throws<ArgumentOutOfRangeException>(() => 1m.CircleClamp(4m, 4m));

        BigInteger value = new BigInteger(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => value.CircleClamp(new BigInteger(6), new BigInteger(6)));
    }
}
