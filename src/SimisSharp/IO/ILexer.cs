namespace KE.MSTS.SimisSharp.IO;

public interface ILexer
{
    ICollection<string> Unprocessed { get; }

    IList<string> Lex(string text);
}
