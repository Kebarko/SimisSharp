namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Converts a token stream produced by <see cref="ILexer"/> into a <see cref="SimisDocument"/> AST.
/// </summary>
public interface IParser
{
    /// <summary>
    /// Parse an enumerable of tokens into a <see cref="SimisDocument"/>.
    /// </summary>
    /// <param name="tokens">Token stream (as produced by <see cref="ILexer"/>).</param>
    /// <returns>Parsed <see cref="SimisDocument"/> instance.</returns>
    SimisDocument Parse(IEnumerable<string> tokens);
}
