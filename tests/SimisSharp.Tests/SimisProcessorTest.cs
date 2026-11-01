using KE.MSTS.SimisSharp.IO;
using System.IO.Compression;
using System.Text;

namespace KE.MSTS.SimisSharp.Tests;

public class SimisProcessorTest
{
    private const string Text = "SIMISA@@@@@@@@@@JINX0D0t______\r\n\r\nWagon ( Mass ( 10t ) )\r\n";
    private const string NonAsciiText = "SIMISA@@@@@@@@@@JINX0D0t______\r\n\r\nWagon ( Name ( \"Lokomotíva Škoda\" ) )\r\n";

    private readonly SimisProcessor processor = new();

    /// <summary>
    /// Windows-1250, the ANSI code page of Central European Windows, which .NET provides after registering the code pages provider.
    /// </summary>
    private static Encoding CentralEuropeanAnsi
    {
        get
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1250);
        }
    }

    [Theory]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    [InlineData("utf-8")]
    [InlineData("utf-32")]
    [InlineData("utf-32BE")]
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

    [Theory]
    [InlineData("utf-16")]
    [InlineData("utf-16BE")]
    [InlineData("utf-8")]
    [InlineData("us-ascii")]
    public void LoadsFileWithoutByteOrderMark(string encodingName)
    {
        // Arrange
        byte[] content = Encoding.GetEncoding(encodingName).GetBytes(Text);

        // Act
        SimisDocument document = WithTempFile(content, processor.Load);

        // Assert
        AssertWagonDocument(document);
    }

    [Theory]
    [InlineData("utf-16", false)]
    [InlineData("utf-16", true)]
    [InlineData("utf-8", false)]
    [InlineData("utf-8", true)]
    public void LoadsNonAsciiText(string encodingName, bool withByteOrderMark)
    {
        // Arrange
        Encoding encoding = Encoding.GetEncoding(encodingName);
        byte[] content = [.. withByteOrderMark ? encoding.GetPreamble() : [], .. encoding.GetBytes(NonAsciiText)];

        // Act
        SimisDocument document = WithTempFile(content, processor.Load);

        // Assert
        AssertNonAsciiDocument(document);
    }

    [Fact]
    public void LoadsAnsiFileWithGivenEncoding()
    {
        // Arrange
        byte[] content = CentralEuropeanAnsi.GetBytes(NonAsciiText);

        // Act
        SimisDocument document = WithTempFile(content, path => processor.Load(path, CentralEuropeanAnsi));

        // Assert
        AssertNonAsciiDocument(document);
    }

    [Fact]
    public void ByteOrderMarkWinsOverGivenEncoding()
    {
        // Arrange
        byte[] content = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(NonAsciiText)];

        // Act
        SimisDocument document = WithTempFile(content, path => processor.Load(path, CentralEuropeanAnsi));

        // Assert
        AssertNonAsciiDocument(document);
    }

    [Fact]
    public void LoadRejectsAnsiFileWithoutGivenEncoding()
    {
        // Arrange
        byte[] content = CentralEuropeanAnsi.GetBytes(NonAsciiText);

        // Act & Assert
        FormatException exception = Assert.Throws<FormatException>(() => WithTempFile(content, processor.Load));
        Assert.Contains("utf-8", exception.Message);
    }

    [Fact]
    public void LoadRejectsTruncatedUtf16File()
    {
        // Arrange
        byte[] content = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(Text), 0x41];

        // Act & Assert
        Assert.Throws<FormatException>(() => WithTempFile(content, processor.Load));
    }

    [Theory]
    [InlineData("SIMISA@@@@@@@@@@JINX0s1b______")]
    [InlineData("SIMISA@@@@@@@@@@JINX0w2b______")]
    public void LoadRejectsBinaryFiles(string header)
    {
        // Arrange: the payload is plain ASCII, so only the header tells the file is not text.
        byte[] content = [.. Encoding.Latin1.GetBytes(header), .. Encoding.ASCII.GetBytes("\r\nWagon ( Mass ( 10t ) )\r\n")];

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => WithTempFile(content, processor.Load));
    }

    [Theory]
    [InlineData("SIMISA@@@@@@@@@@JINX0s1t______")]
    [InlineData("SIMISA@@@@@@@@@@JINX0D0t______")]
    [InlineData("SIMISA@@@@@@@@@@JINX0b")]
    [InlineData("SIMISA@@@@@@@@@@")]
    public void LoadAcceptsTextHeaders(string header)
    {
        // Arrange
        byte[] content = Encoding.ASCII.GetBytes(header + "\r\n\r\nWagon ( Mass ( 10t ) )\r\n");

        // Act
        SimisDocument document = WithTempFile(content, processor.Load);

        // Assert
        Assert.Equal(header, document.Root.Nodes[0].Value);
        Assert.Equal("10t", Assert.Single(SimisPathService.FindNodesByPath(document.Root, "/Wagon/Mass/[1]")).Value);
    }

    [Theory]
    [InlineData("utf-8", false)]
    [InlineData("utf-8", true)]
    [InlineData("utf-16", false)]
    [InlineData("utf-16", true)]
    public void LoadsCompressedFile(string encodingName, bool withByteOrderMark)
    {
        // Arrange
        byte[] content = CompressedFile(NonAsciiText, Encoding.GetEncoding(encodingName), withByteOrderMark);

        // Act
        string text = WithTempFile(content, processor.LoadText);

        // Assert: the text is that of the uncompressed file, header included.
        Assert.Equal(NonAsciiText, text);
        AssertNonAsciiDocument(processor.Parse(text));
    }

    [Fact]
    public void LoadsCompressedAnsiFileWithGivenEncoding()
    {
        // Arrange
        byte[] content = CompressedFile(NonAsciiText, CentralEuropeanAnsi, withByteOrderMark: false);

        // Act
        SimisDocument document = WithTempFile(content, path => processor.Load(path, CentralEuropeanAnsi));

        // Assert
        Assert.Equal("SIMISA@@@@@@@@@@JINX0D0t______", document.Root.Nodes[0].Value);
        AssertNonAsciiDocument(document);
    }

    [Theory]
    [InlineData("JINX0s1b______\r\n")]
    [InlineData("JINX0w2b______\r\n")]
    [InlineData("\u0001\0\0\0\u0001\0\0\0\u0010\0\0\0\u0010\0\0\0")]
    public void LoadRejectsCompressedFilesWithoutText(string subheader)
    {
        // Arrange: the second case starts like a texture.
        byte[] content = Compress([.. Encoding.Latin1.GetBytes(subheader), .. Encoding.ASCII.GetBytes("\r\nWagon ( Mass ( 10t ) )\r\n")]);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => WithTempFile(content, processor.Load));
    }

    [Theory]
    [InlineData("SIMISA@F")]
    [InlineData("SIMISA@F\u0010\0\0\0@@@@")]
    [InlineData("SIMISA@F\u0010\0\0\0@@@@\r\nWagon ( Mass ( 10t ) )\r\n")]
    public void LoadRejectsDamagedCompressedFiles(string file)
    {
        // Arrange: the files are truncated, or the data after the header is not compressed.
        byte[] content = Encoding.Latin1.GetBytes(file);

        // Act & Assert
        Assert.Throws<FormatException>(() => WithTempFile(content, processor.Load));
    }

    [Fact]
    public void LoadRejectsCompressedFileWithWrongChecksum()
    {
        // Arrange
        byte[] content = CompressedFile(Text, Encoding.ASCII, withByteOrderMark: false);
        content[^1] ^= 0xFF;

        // Act & Assert
        Assert.Throws<FormatException>(() => WithTempFile(content, processor.Load));
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

    [Fact]
    public void SaveCreatesAndReplacesFilesWithoutLeftovers()
    {
        WithTempDirectory(directory =>
        {
            // Arrange
            string path = Path.Combine(directory, "wagon.wag");
            SimisDocument first = processor.Parse(Text);
            SimisDocument second = processor.Parse("Wagon ( Mass ( 20t ) )");

            // Act
            processor.Save(first, path);
            string created = File.ReadAllText(path, Encoding.Unicode);
            processor.Save(second, path);
            string replaced = File.ReadAllText(path, Encoding.Unicode);

            // Assert
            Assert.Equal(processor.Save(first), created);
            Assert.Equal(processor.Save(second), replaced);
            Assert.Equal([path], Directory.GetFiles(directory));
        });
    }

    [Fact]
    public void SaveKeepsAttributesOfReplacedFile()
    {
        WithTempDirectory(directory =>
        {
            // Arrange
            string path = Path.Combine(directory, "wagon.wag");
            File.WriteAllText(path, "old", Encoding.Unicode);
            File.SetAttributes(path, FileAttributes.Archive | FileAttributes.Hidden);

            // Act
            processor.Save(processor.Parse(Text), path);

            // Assert
            Assert.True(File.GetAttributes(path).HasFlag(FileAttributes.Hidden));
            Assert.Equal(processor.Save(processor.Parse(Text)), File.ReadAllText(path, Encoding.Unicode));
        });
    }

    [Fact]
    public void FailedSaveKeepsOriginalFileAndRemovesTemporaryFile()
    {
        WithTempDirectory(directory =>
        {
            // Arrange
            string path = Path.Combine(directory, "wagon.wag");
            File.WriteAllText(path, "original", Encoding.Unicode);

            // Act: another program keeps the file open, so it cannot be replaced.
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Assert.ThrowsAny<IOException>(() => processor.Save(processor.Parse(Text), path));
            }

            // Assert
            Assert.Equal("original", File.ReadAllText(path, Encoding.Unicode));
            Assert.Equal([path], Directory.GetFiles(directory));
        });
    }

    [Fact]
    public void SaveToMissingFolderThrowsWithoutLeftovers()
    {
        WithTempDirectory(directory =>
        {
            // Arrange
            string path = Path.Combine(directory, "missing", "wagon.wag");

            // Act & Assert
            Assert.Throws<DirectoryNotFoundException>(() => processor.Save(processor.Parse(Text), path));
            Assert.Empty(Directory.GetFileSystemEntries(directory));
        });
    }

    [Theory]
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

    [Fact]
    public void BlankLinesInsideBlocksAreKept()
    {
        // Arrange
        string input = Lines(
            "SIMISA@@@@@@@@@@JINX0D0t______",
            "",
            "",
            "Wagon (",
            "",
            "",
            "\tMass ( 10t )",
            "",
            "\tSize ( 1 2 3 )",
            "",
            "",
            ")",
            "",
            "",
            "Engine ( 1 )");
        string collapsed = Lines(
            "SIMISA@@@@@@@@@@JINX0D0t______",
            "",
            "Wagon (",
            "",
            "\tMass ( 10t )",
            "",
            "\tSize ( 1 2 3 )",
            "",
            ")",
            "",
            "Engine ( 1 )");
        SimisDocument document = processor.Parse(input);
        SimisNode wagon = document.Root.Nodes[1];

        // Act
        string kept = processor.Save(document, new WriterOptions { CollapseEmptyLines = false });
        string collapsedActual = processor.Save(document);
        string removed = processor.Save(document, new WriterOptions { PreserveEmptyLines = false });

        // Assert
        Assert.Equal(2, wagon.Formatting.LeadingEmptyLines);
        Assert.Equal(2, wagon.Nodes[0].Formatting.LeadingEmptyLines);
        Assert.Equal(1, wagon.Nodes[1].Formatting.LeadingEmptyLines);
        Assert.Equal(2, wagon.Nodes[1].Formatting.TrailingEmptyLines);
        Assert.Equal(2, document.Root.Nodes[2].Formatting.LeadingEmptyLines);
        Assert.Equal(input, kept);
        Assert.Equal(collapsed, collapsedActual);
        Assert.Equal(Lines("SIMISA@@@@@@@@@@JINX0D0t______", "Wagon (", "\tMass ( 10t )", "\tSize ( 1 2 3 )", ")", "Engine ( 1 )"), removed);
    }

    [Theory]
    [InlineData("A ( B ( 1 )", 1, "'A' is not closed")]
    [InlineData("A ( 1 )\nB (\n\tC ( 2\n)\n", 2, "'B' is not closed")]
    [InlineData("Wagon (\n\tMass (\n\t\t10t\n", 2, "'Mass' is not closed")]
    [InlineData("\"My Wagon\" (\n", 1, "'My Wagon' is not closed")]
    [InlineData("A ( 1 ) ) B ( 2 )", 1, "Unexpected ')'")]
    [InlineData("A ( 1 )\n)\n", 2, "Unexpected ')'")]
    [InlineData(")", 1, "Unexpected ')'")]
    [InlineData("A ( \"multi\nline\" )\n)", 3, "Unexpected ')'")]
    [InlineData("comment ( a\nb )\n)", 3, "Unexpected ')'")]
    [InlineData("(", 1, "block name before '('")]
    [InlineData("\n\n( 1 )", 3, "block name before '('")]
    [InlineData("A ( 1 ) ( 2 )", 1, "block name before '('")]
    [InlineData("A ( ( 1 ) )", 1, "block name before '('")]
    [InlineData("A (\n\tB ( 1 )\n\t( 2 )\n)", 3, "block name before '('")]
    [InlineData("A ( 1 ) )\nB ( \"abc )", 1, "Unexpected ')'")]
    [InlineData("A ( \"abc )\n)", 1, "Unterminated quoted string")]
    public void MalformedBlocksThrowWithLine(string input, int line, string message)
    {
        // Act
        SimisParseException exception = Assert.Throws<SimisParseException>(() => processor.Parse(input));

        // Assert
        Assert.Equal(line, exception.Line);
        Assert.StartsWith($"Line {line}: ", exception.Message);
        Assert.Contains(message, exception.Message);
    }

    [Theory]
    [InlineData("A ( \"abc )", 1)]
    [InlineData("A ( 1 )\nB ( \"abc )\n", 2)]
    [InlineData("A ( \"ok\nstill ok\" )\nB ( \"abc )", 3)]
    public void UnterminatedQuotedStringThrowsWithLine(string input, int line)
    {
        SimisParseException exception = Assert.Throws<SimisParseException>(() => processor.Parse(input));
        Assert.Equal(line, exception.Line);
    }

    [Fact]
    public void LoadReportsLineOfMalformedFile()
    {
        // Arrange
        byte[] content = [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(Lines("SIMISA@@@@@@@@@@JINX0D0t______", "", "Wagon (", "\tMass ( 10t )"))];

        // Act & Assert
        SimisParseException exception = Assert.Throws<SimisParseException>(() => WithTempFile(content, processor.Load));
        Assert.Equal(3, exception.Line);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("A ( )")]
    [InlineData("\"A\" ( 1 )")]
    [InlineData("A ( B ( C ( 1 ) ) D ( ) )\nE ( 2 )")]
    public void WellFormedBlocksParse(string input)
    {
        _ = processor.Parse(input);
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

    private static void AssertNonAsciiDocument(SimisDocument document)
    {
        Assert.Equal("Lokomotíva Škoda", Assert.Single(SimisPathService.FindNodesByPath(document.Root, "/Wagon/Name/[1]")).Value);
    }

    /// <summary>
    /// Returns the compressed file MSTS reads in place of the uncompressed <paramref name="text"/>.
    /// The ASCII subheader that follows the <c>SIMISA@@@@@@@@@@</c> header is kept, and the rest is encoded with <paramref name="encoding"/>.
    /// </summary>
    private static byte[] CompressedFile(string text, Encoding encoding, bool withByteOrderMark)
    {
        const int HeaderLength = 16;
        const int SubheaderLength = 16;

        string subheader = text.Substring(HeaderLength, SubheaderLength);
        string rest = text[(HeaderLength + SubheaderLength)..];

        return Compress([.. Encoding.ASCII.GetBytes(subheader), .. withByteOrderMark ? encoding.GetPreamble() : [], .. encoding.GetBytes(rest)]);
    }

    /// <summary>
    /// Returns a compressed file holding <paramref name="data"/>: the header with the uncompressed length, followed by a zlib stream.
    /// </summary>
    private static byte[] Compress(byte[] data)
    {
        MemoryStream compressed = new();
        using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(data);
        }

        return [.. "SIMISA@F"u8, .. BitConverter.GetBytes(data.Length), .. "@@@@"u8, .. compressed.ToArray()];
    }

    private static void WithTempDirectory(Action<string> action)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"SimisSharp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            action(directory);
        }
        finally
        {
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);

            Directory.Delete(directory, recursive: true);
        }
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
