using KE.MSTS.SimisSharp.IO;
using System.Text;

namespace KE.MSTS.SimisSharp;

public class SimisProcessor(ILexer? lexer = null, IParser? parser = null, IWriter? writer = null)
{
    private readonly ILexer lexer = lexer ?? new Lexer();
    private readonly IParser parser = parser ?? new Parser();
    private readonly IWriter writer = writer ?? new Writer();

    public string Save(SimisDocument document, SimisWriterOptions? options = null)
    {
        return writer.Write(document, options);
    }

    public void Save(SimisDocument document, string filePath, SimisWriterOptions? options = null)
    {
        string content = Save(document, options);

        using FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        using StreamWriter streamWriter = new StreamWriter(stream, Encoding.Unicode, leaveOpen: false);
        streamWriter.Write(content);
    }

    public SimisDocument Parse(string content)
    {
        IList<string> tokens = lexer.Lex(content);
        return parser.Parse(tokens);
    }

    public SimisDocument Load(string filePath)
    {
        using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using StreamReader streamReader = new StreamReader(stream, Encoding.Unicode, leaveOpen: false);
        string content = streamReader.ReadToEnd();
        return Parse(content);
    }
}
