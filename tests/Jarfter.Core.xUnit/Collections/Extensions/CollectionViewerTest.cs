using System.Collections;
using Jarfter.Core.Collections.Extensions;

namespace Jarfter.Core.xUnit.Collections.Extensions;

public sealed class CollectionViewerTest
{
    [Fact]
    public void EmptyArray()
    {
        int[] array = [];
        Assert.Equal("[]", array.View());
    }

    [Fact]
    public void OneItemArray()
    {
        int[] array = [1];
        Assert.Equal("[1]", array.View());
    }

    [Fact]
    public void MultiItemArray()
    {
        int[] array = [12, 34];
        Assert.Equal("[12,34]", array.View());
    }

    [Fact]
    public void Array_WithNullAndString_ShouldUseDefaultBranchFormatting()
    {
        object?[] array = ["A", null, 2];
        Assert.Equal("[A,null,2]", array.View());
    }

    [Fact]
    public void NestedCollections_ShouldRenderRecursively()
    {
        object?[] nested = [new[] { 1, 2 }, new object?[] { null, "x" }];
        Assert.Equal("[[1,2],[null,x]]", nested.View());
    }

    [Fact]
    public void Dictionary_DefaultWriter_ShouldMatchCurrentImplementation()
    {
        IDictionary dictionary = new Dictionary<string, int>
        {
            ["k1"] = 1,
            ["k2"] = 2,
        };

        Assert.Equal("[[k1] = 1,[k2] = 2]", dictionary.View());
    }

    [Fact]
    public void Dictionary_WithCustomWriter_ShouldUseConfiguredWriter()
    {
        IDictionary dictionary = new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = null,
        };

        ViewOptions options = new ViewOptions();
        options.WithDictionaryWriter(static (in viewOptions, self, textWriter, key, value, depth) =>
        {
            textWriter.Write("<");
            self.Write(in viewOptions, textWriter, key, depth);
            textWriter.Write("=>");
            self.Write(in viewOptions, textWriter, value, depth);
            textWriter.Write(">");
        });

        Assert.Equal("[<a=>1>,<b=>null>]", dictionary.View(in options, DefaultDisplayer.Instance));
    }

    [Fact]
    public void ViewOptionsWith_ShouldSupportIndependentCollectionAndDictionaryConfiguration()
    {
        ViewOptions options = new ViewOptions();
        options
            .WithCollectionLeft("(")
            .WithCollectionRight(")")
            .WithCollectionSplitter(" | ")
            .WithDictionaryLeft("{")
            .WithDictionaryRight("}")
            .WithDictionarySplitter(" ; ")
            .WithNullLiteral("NULL")
            .WithDictionaryWriter(static (in _, _, textWriter, _, _, _) => textWriter.Write("ITEM"));

        object?[] array = [1, null, 2];
        IDictionary dictionary = new Dictionary<string, int> { ["k1"] = 1, ["k2"] = 2 };

        Assert.Equal("(1 | NULL | 2)", array.View(in options, DefaultDisplayer.Instance));
        Assert.Equal("{ITEM ; ITEM}", dictionary.View(in options, DefaultDisplayer.Instance));
    }

    [Fact]
    public void Write_ShouldDispatchToExpectedMethodByRuntimeType()
    {
        ViewOptions options = new ViewOptions();
        using StringWriter writer = new StringWriter();
        ICollectionDisplayer displayer = new RoutingDisplayer();

        displayer.Write(in options, writer, null);
        Assert.Equal(nameof(RoutingDisplayer.WriteNull), RoutingDisplayer.LastCall);

        displayer.Write(in options, writer, "s");
        Assert.Equal(nameof(RoutingDisplayer.WriteString), RoutingDisplayer.LastCall);

        displayer.Write(in options, writer, new Dictionary<string, int>());
        Assert.Equal(nameof(RoutingDisplayer.WriteDictionary), RoutingDisplayer.LastCall);

        displayer.Write(in options, writer, new[] { 1 });
        Assert.Equal(nameof(RoutingDisplayer.WriteCollection), RoutingDisplayer.LastCall);

        displayer.Write(in options, writer, new EnumerableOnly());
        Assert.Equal(nameof(RoutingDisplayer.WriteEnumerable), RoutingDisplayer.LastCall);

        displayer.Write(in options, writer, 1);
        Assert.Equal(nameof(RoutingDisplayer.WriteObject), RoutingDisplayer.LastCall);
    }

    [Fact]
    public void View_WithCustomDisplayer_ShouldUseProvidedDisplayer()
    {
        IEnumerable source = new[] { 1, 2, 3 };
        ViewOptions options = new ViewOptions();
        ICollectionDisplayer displayer = new ConstantDisplayer("CUSTOM");

        Assert.Equal("CUSTOM", source.View(in options, displayer));
    }

    [Fact]
    public void MaxDepth_Zero_ShouldNotLimitExpansion()
    {
        object[] nested = [1, new object[] { 2, new object[] { 3 } }];

        ViewOptions options = new ViewOptions();
        options.WithMaxDepth(0);

        Assert.Equal("[1,[2,[3]]]", nested.View(in options, DefaultDisplayer.Instance));
    }

    [Fact]
    public void MaxDepth_WhenExceeded_ShouldWriteTypeNameForCollectionNodes()
    {
        object[] nested = [1, new object[] { 2, new object[] { 3 } }];

        ViewOptions options = new ViewOptions();
        options.WithMaxDepth(1);

        Assert.Equal("[1,[2,System.Object[]]]", nested.View(in options, DefaultDisplayer.Instance));
    }

    private sealed class EnumerableOnly : IEnumerable
    {
        public IEnumerator GetEnumerator() => new[] { 1 }.GetEnumerator();
    }

    private sealed class ConstantDisplayer(string output) : ICollectionDisplayer
    {
        public void Write(in ViewOptions viewOptions, TextWriter textWriter, object? item, uint depth = 0)
        {
            textWriter.Write(output);
        }
    }

    private sealed class RoutingDisplayer : ICollectionDisplayer
    {
        public static string LastCall { get; private set; } = string.Empty;

        public void WriteNull(in ViewOptions viewOptions, TextWriter textWriter) => LastCall = nameof(WriteNull);

        public void WriteString(in ViewOptions viewOptions, TextWriter textWriter, string content) => LastCall = nameof(WriteString);

        public void WriteEnumerable(in ViewOptions viewOptions, TextWriter textWriter, IEnumerable enumerable, uint depth) => LastCall = nameof(WriteEnumerable);

        public void WriteCollection(in ViewOptions viewOptions, TextWriter textWriter, ICollection collection, uint depth) => LastCall = nameof(WriteCollection);

        public void WriteDictionary(in ViewOptions viewOptions, TextWriter textWriter, IDictionary dictionary, uint depth) => LastCall = nameof(WriteDictionary);

        public void WriteObject(in ViewOptions viewOptions, TextWriter textWriter, object @object) => LastCall = nameof(WriteObject);
    }
}
