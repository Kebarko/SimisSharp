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

    private static string Lines(params string[] lines)
    {
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
