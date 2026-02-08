namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Lexical analyzer that converts a textual Simis source into tokens.
/// </summary>
public interface ILexer
{
    /// <summary>
    /// Tokenize the provided text into a list of tokens.
    /// </summary>
    /// <param name="text">Input text to tokenize (must not be null).</param>
    /// <returns>List of tokens in the order they appear in the input.</returns>
    IList<string> Lex(string text);
}
