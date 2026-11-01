namespace KE.MSTS.SimisSharp;

/// <summary>
/// The kind of token a <see cref="SimisReader"/> is positioned on.
/// </summary>
public enum SimisTokenType
{
    /// <summary>
    /// No token: before the first <see cref="SimisReader.Read"/> or after the end of the text.
    /// </summary>
    None,

    /// <summary>
    /// A value that does not name a block, such as <c>10t</c> or <c>"Bench locomotive"</c>.
    /// </summary>
    Value,

    /// <summary>
    /// The name of a block together with its opening parenthesis, such as <c>Mass (</c>.
    /// </summary>
    StartBlock,

    /// <summary>
    /// The closing parenthesis of a block.
    /// </summary>
    EndBlock,

    /// <summary>
    /// A whole comment block, such as <c>comment ( text )</c>. Reported only when comments are read (see <see cref="SimisReader(ReadOnlySpan{char}, bool)"/>).
    /// </summary>
    Comment,
}
