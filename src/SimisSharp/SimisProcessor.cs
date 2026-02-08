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
    /// Serialize the document and write it to <paramref name="filePath"/> using UTF-16 encoding.
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
    /// Load and parse a document from disk using UTF-16 encoding.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <returns>Parsed document AST.</returns>
    public SimisDocument Load(string filePath)
    {
        using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using StreamReader streamReader = new StreamReader(stream, Encoding.Unicode, leaveOpen: false);
        string content = streamReader.ReadToEnd();
        return Parse(content);
    }
}
