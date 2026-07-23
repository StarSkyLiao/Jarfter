using Jarfter.Core.Numerics.Hash;

namespace Jarfter.Core.xUnit.Numerics.Hash;

public sealed class HashCodesTest
{
    [Fact]
    public void Combine_WhenSameValuesProvided_ShouldReturnStableHash()
    {
        int expected = HashCodes.Combine("alpha", 42, true, 1.5d);

        Assert.Equal(expected, HashCodes.Combine("alpha", 42, true, 1.5d));
        Assert.NotEqual(expected, HashCodes.Combine("alpha", 43, true, 1.5d));
        Assert.Equal(HashCodes.Combine<string?>(null), HashCodes.Combine(0));
    }

    [Fact]
    public void StringHash_WhenStringAndSpanContainSameText_ShouldReturnSameHash()
    {
        const string value = "哈希 Hash 123";

        Assert.Equal(value.StringHash(), value.AsSpan().StringHash());
        Assert.Equal(0, string.Empty.StringHash());
        Assert.Equal(0, ReadOnlySpan<char>.Empty.StringHash());
    }
}
