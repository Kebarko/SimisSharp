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
    [InlineData("common.cab/cab.cvf", false)]
    [InlineData(@"..\common.cab\cab.cvf", true)]
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

    [Theory]
    [InlineData(@"""..\\..\\common.snd\\gen.sms""", @"..\..\common.snd\gen.sms")]
    [InlineData(@"""Line one\nLine two""", "Line one\nLine two")]
    [InlineData(@"""Tab\there""", "Tab\there")]
    [InlineData(@"""say \""hi\""""", "say \"hi\"")]
    [InlineData(@"""\M""", "M")]
    [InlineData(@"""plain""", "plain")]
    [InlineData(@"..\plain\path", @"..\plain\path")]
    public void ParserDecodesQuotedValues(string token, string expected)
    {
        // Act
        SimisNode node = processor.Parse($"A ( {token} )").Root.Nodes[0].Nodes[0];

        // Assert
        Assert.Equal(expected, node.Value);
    }

    [Theory]
    [InlineData(@"A ( ""..\\..\\common.snd\\gen.sms"" )")]
    [InlineData(@"A ( ""Line one\nLine two"" )")]
    [InlineData(@"A ( ""\M"" )")]
    [InlineData(@"A ( ..\plain\path )")]
    [InlineData(@"A ( a\""b )")]
    public void UnchangedValuesAreWrittenAsRead(string input)
    {
        Assert.Equal(input + Environment.NewLine, processor.Save(processor.Parse(input)));
    }

    [Theory]
    [InlineData(@"say ""hi""", @"""say \""hi\""""")]
    [InlineData(@"C:\My Folder\", @"""C:\\My Folder\\""")]
    [InlineData(@"..\..\common.cab\cab.cvf", @"""..\\..\\common.cab\\cab.cvf""")]
    [InlineData("Line one\nLine two", @"""Line one\nLine two""")]
    [InlineData("Tab\there", @"""Tab\there""")]
    [InlineData("5\" gauge", @"""5\"" gauge""")]
    [InlineData("plain", "plain")]
    public void ChangedValuesAreQuotedAndEscaped(string value, string expectedText)
    {
        // Arrange
        SimisDocument document = processor.Parse("A ( x )");
        SimisNode node = document.Root.Nodes[0].Nodes[0];

        // Act
        node.Value = value;
        string text = processor.Save(document);
        SimisNode reparsed = processor.Parse(text).Root.Nodes[0].Nodes[0];

        // Assert
        Assert.Equal($"A ( {expectedText} ){Environment.NewLine}", text);
        Assert.Equal(value, reparsed.Value);
    }

    [Theory]
    [InlineData(@"A ( ""abc"" )", "A ( abc )")]
    [InlineData(@"A ( ""a\\b"" )", @"A ( ""a\\b"" )")]
    [InlineData(@"A ( ""\M"" )", "A ( M )")]
    [InlineData(@"A ( ""a\""b"" )", @"A ( ""a\""b"" )")]
    public void DroppingQuotesKeepsEscapesWhereNeeded(string input, string expected)
    {
        Assert.Equal(expected + Environment.NewLine, processor.Save(processor.Parse(input), new WriterOptions { PreserveQuotes = false }));
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("plain", "plain")]
    [InlineData(@"a\\b", @"a\b")]
    [InlineData(@"a\""b", "a\"b")]
    [InlineData(@"\n\t", "\n\t")]
    [InlineData(@"\x\y", "xy")]
    [InlineData(@"end\", @"end\")]
    public void UnescapeTest(string text, string expected)
    {
        Assert.Equal(expected, SimisValue.Unescape(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData(@"C:\My Folder\")]
    [InlineData("say \"hi\"")]
    [InlineData("a\nb\tc\rd")]
    public void EscapeRoundTrips(string text)
    {
        Assert.Equal(text, SimisValue.Unescape(SimisValue.Escape(text)));
    }
}
