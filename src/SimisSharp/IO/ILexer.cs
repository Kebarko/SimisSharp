namespace KE.MSTS.SimisSharp.IO;

public interface ILexer
{
    ISet<string> UnprocessedTokens { get; }

    IList<string> Lex(string text);
}
