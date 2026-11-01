namespace KE.MSTS.SimisSharp.Tests;

public class SimisReaderTest
{
    private const string Wagon = "SIMISA@@@@@@@@@@JINX0D0t______\r\n\r\nWagon ( Name\r\n\tMass ( 10t )\r\n\tSize ( 1 2 3 )\r\n)";

    [Fact]
    public void ReadsValuesAndBlocks()
    {
        // Act
        IList<string> tokens = Tokens(Wagon);

        // Assert
        Assert.Equal(["SIMISA@@@@@@@@@@JINX0D0t______", "Wagon (", "Name", "Mass (", "10t", ")", "Size (", "1", "2", "3", ")", ")"], tokens);
    }

    [Fact]
    public void ReportsDepthAndLine()
    {
        // Arrange
        SimisReader reader = new(Wagon);
        List<(SimisTokenType, int, int)> actual = [];

        // Act
        while (reader.Read())
            actual.Add((reader.TokenType, reader.Depth, reader.Line));

        // Assert
        Assert.Equal(
            [
                (SimisTokenType.Value, 0, 1),
                (SimisTokenType.StartBlock, 0, 3), (SimisTokenType.Value, 1, 3),
                (SimisTokenType.StartBlock, 1, 4), (SimisTokenType.Value, 2, 4), (SimisTokenType.EndBlock, 1, 4),
                (SimisTokenType.StartBlock, 1, 5), (SimisTokenType.Value, 2, 5), (SimisTokenType.Value, 2, 5), (SimisTokenType.Value, 2, 5), (SimisTokenType.EndBlock, 1, 5),
                (SimisTokenType.EndBlock, 0, 6),
            ],
            actual);
        Assert.Equal(SimisTokenType.None, reader.TokenType);
        Assert.False(reader.Read());
    }

    [Fact]
    public void CountsLinesInsideQuotedValuesAndComments()
    {
        // Arrange
        SimisReader reader = new("A ( \"two\nlines\" )\ncomment ( a\nb )\nB ( 1 )");
        List<int> lines = [];

        // Act
        while (reader.Read())
            lines.Add(reader.Line);

        // Assert
        Assert.Equal([1, 1, 2, 5, 5, 5], lines);
    }

    [Fact]
    public void ReadsQuotedValues()
    {
        // Arrange
        SimisReader reader = new("Name ( \"My \\\"big\\\" (loco)\" ) \"Quoted Block\" ( 1 )");

        // Act & Assert
        Assert.True(reader.Read());
        Assert.False(reader.IsQuoted);
        Assert.True(reader.Read());
        Assert.Equal(SimisTokenType.Value, reader.TokenType);
        Assert.True(reader.IsQuoted);
        Assert.Equal("My \\\"big\\\" (loco)", reader.ValueSpan.ToString());
        Assert.Equal("My \"big\" (loco)", reader.GetString());
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.Equal(SimisTokenType.StartBlock, reader.TokenType);
        Assert.True(reader.IsQuoted);
        Assert.Equal("Quoted Block", reader.GetString());
    }

    [Theory]
    [InlineData("a\\\"b c", new[] { "a\\\"b", "c" })]
    [InlineData("a\\\\\"b\" c", new[] { "a\\\\", "\"b\"", "c" })]
    [InlineData("\"a\"b", new[] { "\"a\"", "b" })]
    [InlineData("A(1)B(2)", new[] { "A (", "1", ")", "B (", "2", ")" })]
    [InlineData("A\r\n(\r\n1\r\n)", new[] { "A (", "1", ")" })]
    [InlineData("\"\" ( \"\" )", new[] { "\"\" (", "\"\"", ")" })]
    public void SplitsTokensAsTheParserDoes(string input, string[] expected)
    {
        Assert.Equal(expected, Tokens(input));
    }

    [Fact]
    public void SkipsCommentsByDefault()
    {
        // Arrange
        string input = "Wagon ( comment ( a ( b ) \"c)\" ) _Info ( x ) #ORTSMass ( 1t ) Skip(y) Name ( skip ) Sound ( _engine.sms ) )";

        // Act
        IList<string> tokens = Tokens(input);

        // Assert
        Assert.Equal(["Wagon (", "Name (", "skip", ")", "Sound (", "_engine.sms", ")", ")"], tokens);
    }

    [Fact]
    public void ReadsCommentsWhenAsked()
    {
        // Arrange
        SimisReader reader = new("Wagon (\r\n\tcomment ( a ( b ) \"c)\" )\r\n\t#ORTSMass (1t)\r\n)", readComments: true);

        // Act & Assert
        Assert.True(reader.Read());
        Assert.True(reader.Read());
        Assert.Equal(SimisTokenType.Comment, reader.TokenType);
        Assert.Equal(1, reader.Depth);
        Assert.Equal(2, reader.Line);
        Assert.Equal("comment", reader.ValueSpan.ToString());
        Assert.Equal(" a ( b ) \"c)\" ", reader.CommentText.ToString());
        Assert.True(reader.Read());
        Assert.Equal(SimisTokenType.Comment, reader.TokenType);
        Assert.Equal("#ORTSMass", reader.ValueSpan.ToString());
        Assert.Equal("1t", reader.CommentText.ToString());
        Assert.True(reader.Read());
        Assert.Equal(SimisTokenType.EndBlock, reader.TokenType);
        Assert.True(reader.CommentText.IsEmpty);
    }

    [Fact]
    public void SkipMovesToEndOfBlock()
    {
        // Arrange
        SimisReader reader = new("A ( B ( 1 C ( 2 ) ) D ( 3 ) )");
        reader.Read();
        reader.Read();

        // Act
        reader.Skip();

        // Assert
        Assert.Equal(SimisTokenType.EndBlock, reader.TokenType);
        Assert.Equal(1, reader.Depth);
        Assert.True(reader.Read());
        Assert.Equal("D", reader.GetString());
    }

    [Fact]
    public void SkipIgnoresOtherTokens()
    {
        // Arrange
        SimisReader reader = new("A ( 1 )");
        reader.Read();
        reader.Read();

        // Act
        reader.Skip();

        // Assert
        Assert.Equal(SimisTokenType.Value, reader.TokenType);
        Assert.Equal("1", reader.GetString());
    }

    [Fact]
    public void ReadsTypedValues()
    {
        // Arrange
        SimisReader reader = new("3000kW ff00ff00 true \"12\" 0.5, -7 4294967295 abc");

        // Act & Assert
        reader.Read();
        Assert.True(reader.TryGetNumber(out double power, out string unit));
        Assert.Equal(3000, power);
        Assert.Equal("kW", unit);
        Assert.False(reader.TryGetDouble(out _));
        reader.Read();
        Assert.True(reader.TryGetHex(out uint hex));
        Assert.Equal(0xff00ff00u, hex);
        reader.Read();
        Assert.True(reader.TryGetBoolean(out bool flag));
        Assert.True(flag);
        reader.Read();
        Assert.True(reader.TryGetInt32(out int quoted));
        Assert.Equal(12, quoted);
        reader.Read();
        Assert.True(reader.TryGetDouble(out double half));
        Assert.Equal(0.5, half);
        reader.Read();
        Assert.True(reader.TryGetInt32(out int negative));
        Assert.Equal(-7, negative);
        reader.Read();
        Assert.True(reader.TryGetUInt32(out uint max));
        Assert.Equal(uint.MaxValue, max);
        reader.Read();
        Assert.False(reader.TryGetNumber(out _, out _));
    }

    [Fact]
    public void ValueEqualsIgnoresCaseByDefault()
    {
        // Arrange
        SimisReader reader = new("MaxPower \"a\\\\b\"");

        // Act & Assert
        reader.Read();
        Assert.True(reader.ValueEquals("maxpower"));
        Assert.False(reader.ValueEquals("maxpower", StringComparison.Ordinal));
        Assert.True(reader.ValueEquals("MaxPower", StringComparison.Ordinal));
        reader.Read();
        Assert.True(reader.ValueEquals("a\\b"));
    }

    [Theory]
    [InlineData("A ( B ( 1 )", 1, "'A' is not closed")]
    [InlineData("A ( 1 )\nB (\n\tC ( 2\n)\n", 2, "'B' is not closed")]
    [InlineData("\"My Wagon\" (\n", 1, "'My Wagon' is not closed")]
    [InlineData("A ( 1 )\n)\n", 2, "Unexpected ')'")]
    [InlineData("A ( \"multi\nline\" )\n)", 3, "Unexpected ')'")]
    [InlineData("comment ( a\nb )\n)", 3, "Unexpected ')'")]
    [InlineData("\n\n( 1 )", 3, "block name before '('")]
    [InlineData("A ( 1 ) ( 2 )", 1, "block name before '('")]
    [InlineData("A ( 1 )\nB ( \"abc )\n", 2, "Unterminated quoted string")]
    [InlineData("A ( \"abc\\\" )", 1, "Unterminated quoted string")]
    [InlineData("A ( 1 )\r\n\r\ncomment (\r\n  note", 3, "Unterminated comment block")]
    [InlineData("comment ( \"a\" +\r\n  \"b ) c )", 2, "Unterminated quoted string in comment block")]
    public void MalformedTextThrowsWithLine(string input, int line, string message)
    {
        // Act
        SimisParseException exception = Assert.Throws<SimisParseException>(() => Tokens(input));

        // Assert
        Assert.Equal(line, exception.Line);
        Assert.Contains(message, exception.Message);
    }

    [Theory]
    [ClassData(typeof(SimisDocumentTestData))]
    public void ReaderMatchesParsedTree(string input, string expected, object? options = null)
    {
        // Arrange
        _ = expected;
        _ = options;
        List<string> fromTree = [];
        foreach (SimisNode node in new SimisProcessor().Parse(input).Root.Nodes)
            AddTreeTokens(node, fromTree);

        // Act
        IList<string> fromReader = Tokens(input, readComments: true);

        // Assert
        Assert.Equal(fromTree, fromReader);
    }

    /// <summary>
    /// Reads all tokens of <paramref name="input"/>: values as decoded text in quotes when quoted, block starts as the name and a parenthesis,
    /// and comment blocks as <c>#name(content)</c>.
    /// </summary>
    private static IList<string> Tokens(string input, bool readComments = false)
    {
        List<string> tokens = [];
        SimisReader reader = new(input, readComments);
        while (reader.Read())
        {
            string value = reader.IsQuoted ? $"\"{reader.GetString()}\"" : reader.GetString();
            tokens.Add(reader.TokenType switch
            {
                SimisTokenType.StartBlock => value + " (",
                SimisTokenType.EndBlock => ")",
                SimisTokenType.Comment => $"#{value}({reader.CommentText})",
                _ => value,
            });
        }

        return tokens;
    }

    /// <summary>
    /// Adds the tokens <see cref="Tokens"/> returns for the text of <paramref name="node"/>, taken from the parsed tree.
    /// </summary>
    private static void AddTreeTokens(SimisNode node, List<string> tokens)
    {
        if (node.IsDummyLeaf)
            return;

        string value = node.Formatting.IsQuoted ? $"\"{node.Value}\"" : node.Value!;
        if (node.IsLeaf)
        {
            tokens.Add(value);
        }
        else if (node.IsComment)
        {
            SimisNode content = node.Nodes[0];
            tokens.Add($"#{value}({(content.Formatting.IsVerbatim ? content.Value : string.Empty)})");
        }
        else
        {
            tokens.Add(value + " (");
            foreach (SimisNode subnode in node.Nodes)
                AddTreeTokens(subnode, tokens);
            tokens.Add(")");
        }
    }
}
