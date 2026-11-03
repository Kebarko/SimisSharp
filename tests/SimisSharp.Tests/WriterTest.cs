using KE.MSTS.SimisSharp.IO;

namespace KE.MSTS.SimisSharp.Tests;

public class WriterTest
{
    private readonly SimisProcessor processor = new();

    [Theory]
    [InlineData("-2")]
    [InlineData("0")]
    [InlineData("abc")]
    [InlineData("99999999999")]
    public void TableWithoutValidColumnCountIsWrittenPlain(string count)
    {
        // Arrange
        string input = $"Curve ( {count} Table ( 1 2 3 4 ) )";
        WriterOptions options = new() { Table = { new WriterOptions.TableConfig { Name = "Table", Columns = "../[1]", FixedWidth = true } } };

        // Act
        string actual = processor.Save(processor.Parse(input), options);

        // Assert
        Assert.Equal(Lines($"Curve ( {count}", "\tTable ( 1 2 3 4 )", ")"), actual);
    }

    [Theory]
    [InlineData("-3")]
    [InlineData("0")]
    public void TableWithInvalidConfiguredColumnCountIsWrittenPlain(string columns)
    {
        // Arrange
        WriterOptions options = new() { Table = { new WriterOptions.TableConfig { Name = "Table", Columns = columns, FixedWidth = true } } };

        // Act
        string actual = processor.Save(processor.Parse("Table ( 1 2 3 4 )"), options);

        // Assert
        Assert.Equal(Lines("Table ( 1 2 3 4 )"), actual);
    }

    [Fact]
    public void TableColumnCountIsLimitedToNumberOfValues()
    {
        // Arrange
        string input = "Curve ( 100000000 Table ( 1 22 333 ) )";
        WriterOptions options = new() { Table = { new WriterOptions.TableConfig { Name = "Table", Columns = "../[1]", FixedWidth = true } } };

        // Act
        string actual = processor.Save(processor.Parse(input), options);

        // Assert
        Assert.Equal(Lines("Curve ( 100000000", "\tTable (", "\t\t1 22 333", "\t)", ")"), actual);
    }

    [Theory]
    [InlineData("Briefing ( \"One\" + \"Two\" )", "Briefing ( \"One\" + \"Two\" )")]
    [InlineData("Briefing ( \"One\" + \"Two\" + \"Three\" \"Alone\" )", "Briefing ( \"One\" + \"Two\" + \"Three\" Alone )")]
    [InlineData("A ( \"a b\" \"+\" \"c d\" )", "A ( \"a b\" \"+\" \"c d\" )")]
    [InlineData("A ( \"x\" \"+\" )", "A ( x \"+\" )")]
    [InlineData("Wagon ( \"_Mass\" ( 10t ) )", "Wagon (\r\n\t\"_Mass\" ( 10t )\r\n)")]
    [InlineData("Wagon ( \"#Mass\" ( 10t ) )", "Wagon (\r\n\t\"#Mass\" ( 10t )\r\n)")]
    [InlineData("Wagon ( \"comment\" ( x ) )", "Wagon (\r\n\t\"comment\" ( x )\r\n)")]
    [InlineData("Wagon ( \"Mass\" ( \"_x\" ) )", "Wagon (\r\n\tMass ( _x )\r\n)")]
    public void DroppingQuotesKeepsMeaning(string input, string expected)
    {
        // Act
        SimisDocument document = processor.Parse(input);
        string actual = processor.Save(document, new WriterOptions { PreserveQuotes = false });
        SimisDocument reparsed = processor.Parse(actual);

        // Assert
        Assert.Equal(expected + "\r\n", actual);
        Assert.Equal(Describe(document.Root), Describe(reparsed.Root));
    }

    [Fact]
    public void DroppingQuotesKeepsConcatenationAtRootAndInTables()
    {
        // Arrange
        WriterOptions options = new()
        {
            PreserveQuotes = false,
            Table = { new WriterOptions.TableConfig { Name = "Briefing", Columns = "2", ColumnSeparator = string.Empty } },
        };

        // Act
        string root = processor.Save(processor.Parse("\"One\" + \"Two\""), options);
        string table = processor.Save(processor.Parse("Briefing ( \"One\\n\" + \"Two\" )"), options);

        // Assert
        Assert.Equal(Lines("\"One\"", "+", "\"Two\""), root);
        Assert.Equal(Lines("Briefing (", "\t\"One\\n\"+", "\t\"Two\"", ")"), table);
    }

    /// <summary>
    /// Describes the values, comment flags and structure of a tree, ignoring quotes and formatting.
    /// </summary>
    private static string Describe(SimisNode node)
    {
        return string.Join(" ", node.Nodes.Select(n => n.IsLeaf ? $"[{n.Value}]" : $"[{n.Value}{(n.IsComment ? "#" : "")}]({Describe(n)})"));
    }

    private static string Lines(params string[] lines)
    {
        return string.Join("\r\n", lines) + "\r\n";
    }
}
