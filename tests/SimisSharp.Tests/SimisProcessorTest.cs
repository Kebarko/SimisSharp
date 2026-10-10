using KE.MSTS.SimisSharp.IO;
using System.Text;

namespace KE.MSTS.SimisSharp.Tests;

public class SimisProcessorTest
{
    private const string Text = "SIMISA@@@@@@@@@@JINX0D0t______\r\n\r\nWagon ( Mass ( 10t ) )\r\n";

    private readonly SimisProcessor processor = new();

    [Theory]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    [InlineData("utf-8")]
    public void LoadsFileWithByteOrderMark(string encodingName)
    {
        // Arrange
        Encoding encoding = Encoding.GetEncoding(encodingName);
        byte[] content = [.. encoding.GetPreamble(), .. encoding.GetBytes(Text)];

        // Act
        SimisDocument document = WithTempFile(content, processor.Load);

        // Assert
        AssertWagonDocument(document);
    }

    [Fact(Skip = "Known bug: a file without a byte order mark is read as UTF-16 and parsed as garbage.")]
    public void LoadsUtf8FileWithoutByteOrderMark()
    {
        // Arrange
        byte[] content = new UTF8Encoding(false).GetBytes(Text);

        // Act
        SimisDocument document = WithTempFile(content, processor.Load);

        // Assert
        AssertWagonDocument(document);
    }

    [Theory(Skip = "Known bug: binary and compressed SIMIS files are read as text without an error.")]
    [InlineData("SIMISA@@@@@@@@@@JINX0s1b______")]
    [InlineData("SIMISA@F\u0010\0\0\0@@@@")]
    public void LoadRejectsBinaryAndCompressedFiles(string header)
    {
        // Arrange
        byte[] content = [.. Encoding.Latin1.GetBytes(header), 0x78, 0x9C, 0x01, 0x02, 0x03, 0x04];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => WithTempFile(content, processor.Load));
    }

    [Fact]
    public void SaveWritesUtf16WithByteOrderMark()
    {
        // Arrange
        SimisDocument document = processor.Parse(Text);
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.eng");

        try
        {
            // Act
            processor.Save(document, path);
            byte[] content = File.ReadAllBytes(path);

            // Assert
            Assert.Equal([0xFF, 0xFE], content[..2]);
            Assert.Equal(processor.Save(document), Encoding.Unicode.GetString(content[2..]));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory(Skip = "Known bug: without collapsing, every run of blank lines is written with one blank line too many.")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void BlankLinesAreKeptWithoutCollapsing(int blankLines)
    {
        // Arrange
        string input = Lines("A ( 1 )") + string.Concat(Enumerable.Repeat(Environment.NewLine, blankLines)) + Lines("B ( 2 )");

        // Act
        string actual = processor.Save(processor.Parse(input), new WriterOptions { CollapseEmptyLines = false });

        // Assert
        Assert.Equal(input, actual);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void BlankLinesAreCollapsed(int blankLines)
    {
        // Arrange
        string input = Lines("A ( 1 )") + string.Concat(Enumerable.Repeat(Environment.NewLine, blankLines)) + Lines("B ( 2 )");

        // Act
        string actual = processor.Save(processor.Parse(input));

        // Assert
        Assert.Equal(Lines("A ( 1 )", "", "B ( 2 )"), actual);
    }

    [Theory(Skip = "Known bug: unbalanced parentheses are accepted silently.")]
    [InlineData("A ( B ( 1 )")]
    [InlineData("A ( 1 ) ) B ( 2 )")]
    [InlineData(")")]
    [InlineData("(")]
    public void UnbalancedParenthesesThrow(string input)
    {
        Assert.Throws<FormatException>(() => processor.Parse(input));
    }

    /// <summary>
    /// MSTS expects CRLF line endings. The writer uses <see cref="Environment.NewLine"/>,
    /// so this test only fails on platforms whose newline is not CRLF.
    /// </summary>
    [Fact]
    public void WriterUsesCrLfLineEndings()
    {
        // Act
        string actual = processor.Save(processor.Parse("A ( 1 )\nB (\n\tC ( 2 )\n)\n"));

        // Assert
        Assert.Equal("A ( 1 )\r\nB (\r\n\tC ( 2 )\r\n)\r\n", actual);
    }

    private static void AssertWagonDocument(SimisDocument document)
    {
        IList<SimisNode> nodes = document.Root.GetNodes();
        Assert.Equal(["SIMISA@@@@@@@@@@JINX0D0t______", "Wagon"], nodes.Select(n => n.Value));
        Assert.Equal("10t", Assert.Single(SimisPathService.FindNodesByPath(document.Root, "/Wagon/Mass/[1]")).Value);
    }

    private static T WithTempFile<T>(byte[] content, Func<string, T> action)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.eng");

        try
        {
            File.WriteAllBytes(path, content);
            return action(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string Lines(params string[] lines)
    {
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
