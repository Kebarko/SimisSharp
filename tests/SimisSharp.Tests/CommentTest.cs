using KE.MSTS.SimisSharp.IO;

namespace KE.MSTS.SimisSharp.Tests;

public class CommentTest
{
    private readonly SimisProcessor processor = new();

    [Theory]
    [InlineData("comment ( note )", "comment", " note ")]
    [InlineData("Comment(note)", "Comment", "note")]
    [InlineData("skip ( note )", "skip", " note ")]
    [InlineData("SKIP ( note )", "SKIP", " note ")]
    [InlineData("_skip ( note )", "_skip", " note ")]
    [InlineData("_Info ( note )", "_Info", " note ")]
    [InlineData("_ORTSMaxPower ( 1000kW )", "_ORTSMaxPower", " 1000kW ")]
    [InlineData("#ORTSMaxPower ( 1000kW )", "#ORTSMaxPower", " 1000kW ")]
    [InlineData("# ( note )", "#", " note ")]
    [InlineData("#( note )", "#", " note ")]
    [InlineData("_ ( note )", "_", " note ")]
    [InlineData("comment\r\n(\r\n  note\r\n)", "comment", "\r\n  note\r\n")]
    public void CommentBlockKeepsContentVerbatim(string input, string name, string content)
    {
        // Act
        SimisNode comment = Assert.Single(processor.Parse(input).Root.GetNodes());

        // Assert
        Assert.True(comment.IsComment);
        Assert.Equal(name, comment.Value);
        Assert.Equal(content, Assert.Single(comment.GetNodes()).Value);
    }

    [Theory]
    [InlineData("comment ( \"a ) b\" )", " \"a ) b\" ")]
    [InlineData("comment ( \"a \\\" ) b\" )", " \"a \\\" ) b\" ")]
    [InlineData("comment ( \"a\" + \"b ) c\" )", " \"a\" + \"b ) c\" ")]
    [InlineData("comment ( \"a\" +\r\n  \"b ) c\" )", " \"a\" +\r\n  \"b ) c\" ")]
    [InlineData("comment ( Wire ( 0.0   6.2 -0.219 ) )", " Wire ( 0.0   6.2 -0.219 ) ")]
    [InlineData("comment ( 5\" tyre )", " 5\" tyre ")]
    [InlineData("comment ( a\\) )", " a\\) ")]
    [InlineData("_ORTSMaxTractiveForceCurves ( 2 0 ( 0 0 ) 1 ( \"x)\" ) )", " 2 0 ( 0 0 ) 1 ( \"x)\" ) ")]
    public void CommentBlockEndsWhereOpenRailsEndsIt(string input, string content)
    {
        // Act
        IList<SimisNode> nodes = processor.Parse(input + "\r\nNext ( 1 )").Root.GetNodes();

        // Assert
        Assert.Equal(2, nodes.Count);
        Assert.True(nodes[0].IsComment);
        Assert.Equal(content, Assert.Single(nodes[0].GetNodes()).Value);
        Assert.Equal("Next", nodes[1].Value);
        Assert.Equal("1", Assert.Single(nodes[1].GetNodes()).Value);
    }

    [Theory]
    [InlineData("comment (\r\n)")]
    [InlineData("comment (\n)")]
    [InlineData("comment (\"abc\")")]
    [InlineData("comment ( )")]
    [InlineData("comment ( Wire ( 0.0   6.2 -0.219 ) )")]
    [InlineData("A ( 1 ) comment ( note )")]
    public void CommentBlockRoundTripsVerbatim(string input)
    {
        // Act
        string actual = processor.Save(processor.Parse(input));

        // Assert
        Assert.Equal(input + Environment.NewLine, actual);
    }

    [Theory]
    [InlineData("Name ( comment )")]
    [InlineData("Name ( Skip )")]
    [InlineData("A ( x skip y )")]
    [InlineData("A ( 1 comment )")]
    [InlineData("Filename ( _shape.s )")]
    [InlineData("Description ( #1 engine )")]
    [InlineData("\"comment\" ( x )")]
    public void CommentTokenWithoutBlockIsValue(string input)
    {
        // Act
        SimisDocument document = processor.Parse(input);
        string actual = processor.Save(document);

        // Assert
        Assert.DoesNotContain(Descendants(document.Root), n => n.IsComment);
        Assert.Equal(input + Environment.NewLine, actual);
    }

    [Theory]
    [InlineData("comment ( note")]
    [InlineData("comment ( \"a ) b )")]
    [InlineData("_ORTSFoo ( ( 1 )")]
    public void UnterminatedCommentBlockThrows(string input)
    {
        Assert.Throws<FormatException>(() => processor.Parse(input));
    }

    [Fact]
    public void RemovingCommentsKeepsValues()
    {
        // Arrange
        string input = Lines(
            "Wagon (",
            "\tcomment ( Gondola Car )",
            "\t_ORTSMaxTractiveForceCurves ( 2 0 ( 0 0 ) 1 ( 0 0 ) )",
            "\t#ORTSBrakeShoeFriction ( 0 0.5 )",
            "\tSkip ( \"a ) b\" )",
            "\tName ( skip )",
            "\tSound ( _engine.sms )",
            "\tComment ( #1 )",
            ")");
        string expected = Lines(
            "Wagon (",
            "\tName ( skip )",
            "\tSound ( _engine.sms )",
            ")");
        SimisDocument document = processor.Parse(input);

        // Act
        string withComments = processor.Save(document);
        string withoutComments = processor.Save(document, new WriterOptions { PreserveComments = false });

        // Assert
        Assert.Equal(input, withComments);
        Assert.Equal(expected, withoutComments);
    }

    [Fact]
    public void CommentRequiresBlock()
    {
        // Arrange
        SimisNode node = new("comment");

        // Act
        bool leafIsComment = node.IsComment;
        node.AddNode(" note ");

        // Assert
        Assert.False(leafIsComment);
        Assert.True(node.IsComment);
    }

    private static string Lines(params string[] lines)
    {
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static IEnumerable<SimisNode> Descendants(SimisNode node)
    {
        foreach (SimisNode subnode in node.GetNodes())
        {
            yield return subnode;

            foreach (SimisNode descendant in Descendants(subnode))
                yield return descendant;
        }
    }
}
