using KE.MSTS.SimisSharp.IO;
using System.Text;

namespace KE.MSTS.SimisSharp;

/// <summary>
/// High-level facade that combines lexer, parser and writer to load/parse/save Simis documents. Allows dependency injection of custom lexer/parser/writer implementations for testing or extension.
/// </summary>
public class SimisProcessor(ILexer? lexer = null, IParser? parser = null, IWriter? writer = null)
{
    private readonly ILexer lexer = lexer ?? new Lexer();
    private readonly IParser parser = parser ?? new Parser();
    private readonly IWriter writer = writer ?? new Writer();

    /// <summary>
    /// Serialize the document into a string using the provided writer <paramref name="options"/>.
    /// </summary>
    /// <param name="document">Document to save.</param>
    /// <param name="options">Optional writer options.</param>
    /// <returns>Textual representation of the document.</returns>
    public string Save(SimisDocument document, WriterOptions? options = null)
    {
        return writer.Write(document, options);
    }

    /// <summary>
    /// Serialize the document and write it to <paramref name="filePath"/> as UTF-16LE with a byte order mark, the encoding MSTS reads.
    /// </summary>
    /// <param name="document">Document to save.</param>
    /// <param name="filePath">Target file path to create/overwrite.</param>
    /// <param name="options">Optional writer options.</param>
    public void Save(SimisDocument document, string filePath, WriterOptions? options = null)
    {
        string content = Save(document, options);

        using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using StreamWriter streamWriter = new StreamWriter(stream, Encoding.Unicode, leaveOpen: false);
        streamWriter.Write(content);
    }

    /// <summary>
    /// Parse a SIMIS formatted string into a <see cref="SimisDocument"/>.
    /// </summary>
    /// <param name="content">Text content to parse.</param>
    /// <returns>Parsed document AST.</returns>
    public SimisDocument Parse(string content)
    {
        IList<string> tokens = lexer.Lex(content);
        return parser.Parse(tokens);
    }

    /// <summary>
    /// Load and parse a text document from disk, detecting its encoding.
    /// A byte order mark selects UTF-8, UTF-16 or UTF-32. A file without one is read as UTF-16 when its first or second byte is zero,
    /// and as UTF-8 otherwise, which also covers ASCII.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <returns>Parsed document AST.</returns>
    /// <exception cref="NotSupportedException">When the file is a binary or compressed SIMIS file.</exception>
    /// <exception cref="FormatException">When the file is not valid text in the detected encoding.</exception>
    public SimisDocument Load(string filePath)
    {
        return Load(filePath, null);
    }

    /// <summary>
    /// Load and parse a text document from disk, detecting its encoding as <see cref="Load(string)"/> does,
    /// but reading a file that has no byte order mark and is not UTF-16 with <paramref name="encoding"/>.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <param name="encoding">Encoding of a file without a byte order mark that is not UTF-16, such as an ANSI file. Null means UTF-8.
    /// ANSI code pages such as Windows-1250 require registering <c>System.Text.CodePagesEncodingProvider</c> first.</param>
    /// <returns>Parsed document AST.</returns>
    /// <exception cref="NotSupportedException">When the file is a binary or compressed SIMIS file.</exception>
    /// <exception cref="FormatException">When the file is not valid text in the detected encoding.</exception>
    public SimisDocument Load(string filePath, Encoding? encoding)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        return Parse(SimisTextDecoder.Decode(bytes, encoding));
    }
}
