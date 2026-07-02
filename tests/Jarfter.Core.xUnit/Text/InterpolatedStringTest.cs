using Jarfter.Core.Text;

namespace Jarfter.Core.xUnit.Text;

public sealed class InterpolatedStringTest
{
    [Fact]
    public void Constructor_WithSmallCapacity_ShouldUseMinimumCapacity()
    {
        InterpolatedString value = new InterpolatedString(1);
        try
        {
            Assert.Equal(0, value.Length);
            Assert.True(value.InternalCharSpan.Length >= 8);
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void Constructor_WithInitialString_ShouldCopyContent()
    {
        InterpolatedString value = new InterpolatedString("hello");
        try
        {
            Assert.Equal(5, value.Length);
            Assert.Equal("hello", value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void ToStringAndClear_ShouldReturnSnapshotAndResetLength()
    {
        InterpolatedString value = new InterpolatedString("abc");
        string text = value.ToStringAndClear();

        Assert.Equal("abc", text);
        Assert.Equal(0, value.Length);
    }

    [Fact]
    public void SafeClear_ShouldOnlyResetLengthAndAllowReuse()
    {
        InterpolatedString value = new InterpolatedString("abc");
        try
        {
            int originalCapacity = value.InternalCharSpan.Length;

            value.SafeClear();
            value.Append("x");

            Assert.Equal(1, value.Length);
            Assert.Equal("x", value.ToString());
            Assert.Equal(originalCapacity, value.InternalCharSpan.Length);
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void AppendOverloads_ShouldComposeExpectedContent()
    {
        InterpolatedString value = new InterpolatedString();
        try
        {
            value
                .Append("A")
                .Append(true)
                .Append(' ')
                .Append((int)42)
                .Append(ReadOnlyMemory<char>.Empty)
                .AppendLine("B")
                .AppendLine();

            Assert.Equal("ATrue 42B\n\n", value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void AppendSpan_WhenRequiredCapacityExceedsDoubleCapacity_ShouldSucceed()
    {
        InterpolatedString value = new InterpolatedString(8);
        try
        {
            int capacity = value.InternalCharSpan.Length;
            int currentLength = capacity - 1;
            value.Length = currentLength;
            value.InternalCharSpan[..currentLength].Fill('a');

            string appendText = new string('b', capacity + 2);
            value.Append(appendText.AsSpan());

            Assert.Equal(currentLength + appendText.Length, value.Length);
            Assert.Equal(new string('a', currentLength) + appendText, value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void InsertOverloads_ShouldInsertAtExpectedPositions()
    {
        InterpolatedString value = new InterpolatedString("ace");
        try
        {
            value
                .Insert(1, 'b')
                .Insert(3, "d")
                .Insert(5, "f".AsSpan())
                .Insert(6, new ValueWithText("!"));

            Assert.Equal("abcdef!", value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void Insert_WhenIndexOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        InterpolatedString value = new InterpolatedString("abc");
        try
        {
            AssertInsertCharOutOfRange(ref value, -1);
            AssertInsertStringOutOfRange(ref value, 4);
            AssertInsertSpanOutOfRange(ref value, 5);
            AssertInsertFormattableOutOfRange(ref value, -1);
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void InsertSpan_WhenInsertValueIsEmpty_ShouldKeepOriginalContent()
    {
        InterpolatedString value = new InterpolatedString("abc");
        try
        {
            value.Insert(1, ReadOnlySpan<char>.Empty);

            Assert.Equal(3, value.Length);
            Assert.Equal("abc", value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void InsertSpan_WhenInsertLengthIsMuchLargerThanCapacity_ShouldSucceed()
    {
        InterpolatedString value = new InterpolatedString("x");
        try
        {
            int capacity = value.InternalCharSpan.Length;
            string inserted = new string('y', capacity * 3);

            value.Insert(0, inserted.AsSpan());

            Assert.Equal(inserted.Length + 1, value.Length);
            Assert.Equal(inserted + "x", value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void InsertFormattable_WhenDestinationIsTooSmall_ShouldUseFallbackPath()
    {
        InterpolatedString value = new InterpolatedString(8);
        try
        {
            int currentLength = value.InternalCharSpan.Length - 1;
            value.Length = currentLength;
            value.InternalCharSpan[..currentLength].Fill('x');

            value.InsertFormattable(currentLength, 12345);

            Assert.Equal(currentLength + 5, value.Length);
            Assert.Equal(new string('x', currentLength) + "12345", value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void InsertFormattable_WhenTailIsLarge_ShouldKeepTextCorrect()
    {
        string tail = new string('z', 700_000);
        InterpolatedString value = new InterpolatedString(tail);
        try
        {
            value.InsertFormattable(0, 42);

            Assert.Equal(tail.Length + 2, value.Length);
            Assert.Equal("42" + tail, value.ToString());
        }
        finally
        {
            value.Clear();
        }
    }

    [Fact]
    public void Dispose_CanBeCalledTwiceWithoutThrowing()
    {
        InterpolatedString value = new InterpolatedString("abc");
        value.Dispose();
        value.Dispose();
    }

    private sealed class ValueWithText(string text)
    {
        public override string ToString() => text;
    }

    private static void AssertInsertCharOutOfRange(ref InterpolatedString value, int index)
    {
        bool thrown = false;
        try
        {
            value.Insert(index, 'x');
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }

    private static void AssertInsertStringOutOfRange(ref InterpolatedString value, int index)
    {
        bool thrown = false;
        try
        {
            value.Insert(index, "x");
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }

    private static void AssertInsertSpanOutOfRange(ref InterpolatedString value, int index)
    {
        bool thrown = false;
        try
        {
            value.Insert(index, "x".AsSpan());
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }

    private static void AssertInsertFormattableOutOfRange(ref InterpolatedString value, int index)
    {
        bool thrown = false;
        try
        {
            value.InsertFormattable(index, 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            thrown = true;
        }

        Assert.True(thrown);
    }
}
