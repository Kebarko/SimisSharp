using KE.MSTS.SimisSharp.IO;

namespace KE.MSTS.SimisSharp.Tests;

public class QuotingTest
{
    private readonly SimisProcessor processor = new();

    [Theory]
    [InlineData("A ( \"abc\" x )", "A ( abc x )")]
    [InlineData("A ( \"\" x )", "A ( \"\" x )")]
    [InlineData("A ( \"a b\" x )", "A ( \"a b\" x )")]
    [InlineData("A ( \"a\tb\" x )", "A ( \"a\tb\" x )")]
    [InlineData("A ( \"a\r\nb\" x )", "A ( \"a\r\nb\" x )")]
    [InlineData("A ( \"a(b\" x )", "A ( \"a(b\" x )")]
    [InlineData("A ( \"a)b\" x )", "A ( \"a)b\" x )")]
    [InlineData("A ( \"a\\\"b\" x )", "A ( \"a\\\"b\" x )")]
    public void WriterQuotesValuesThatNeedQuotes(string input, string expected)
    {
        // Act
        string actual = processor.Save(processor.Parse(input), new WriterOptions { PreserveQuotes = false });

        // Assert
        Assert.Equal(expected + Environment.NewLine, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("a b")]
    [InlineData("a\tb")]
    [InlineData("a\nb")]
    [InlineData("a(b")]
    [InlineData("a)b")]
    [InlineData("a\\\"b")]
    [InlineData("comment")]
    [InlineData("_skip")]
    public void ValueRoundTrips(string value)
    {
        // Arrange
        SimisDocument document = new();
        SimisNode node = new("A");
        node.AddNode(value);
        node.AddNode("x");
        document.Root.AddNode(node);

        // Act
        string text = processor.Save(document, new WriterOptions { PreserveQuotes = false });
        SimisNode parsed = Assert.Single(processor.Parse(text).Root.GetNodes());

        // Assert
        Assert.Equal("A", parsed.Value);
        Assert.Equal([value, "x"], parsed.GetNodes().Select(n => n.Value));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("abc", false)]
    [InlineData("3.5f", false)]
    [InlineData("..\\\\common.cab\\\\cab.cvf", false)]
    [InlineData("a b", true)]
    [InlineData("a\tb", true)]
    [InlineData("a\r\nb", true)]
    [InlineData("a(b", true)]
    [InlineData("a)b", true)]
    [InlineData("a\"b", true)]
    public void RequiresQuotesTest(string value, bool expected)
    {
        Assert.Equal(expected, SimisNode.SimisNodeFormatting.RequiresQuotes(value));
    }
}
