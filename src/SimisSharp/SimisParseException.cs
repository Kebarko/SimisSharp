namespace KE.MSTS.SimisSharp;

/// <summary>
/// The exception thrown when Simis text is not well-formed, such as an unterminated quoted string or comment block,
/// or unbalanced parentheses. <see cref="Line"/> is the line where the problem was found.
/// It is a <see cref="FormatException"/>, as are the errors for text that cannot be decoded, so catching <see cref="FormatException"/> catches both.
/// </summary>
/// <param name="message">Description of the problem, without the line number.</param>
/// <param name="line">The 1-based line where the problem was found.</param>
public class SimisParseException(string message, int line)
    : FormatException($"Line {line}: {message}")
{
    /// <summary>
    /// The 1-based line where the problem was found.
    /// </summary>
    public int Line { get; } = line;
}
