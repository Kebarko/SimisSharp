namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Holds token configuration used by the reader, parser and writer.
/// </summary>
internal static class TokenConfiguration
{
    /// <summary>
    /// Keywords that start a comment block. They are compared case-insensitively.
    /// </summary>
    private static readonly string[] CommentKeywords = ["comment", "skip"];

    /// <summary>
    /// Determines whether <paramref name="token"/> starts a comment block when a block follows it.
    /// MSTS and Open Rails ignore the block after the keywords <c>comment</c> and <c>skip</c> in any letter case,
    /// and after any token that begins with <c>#</c> or <c>_</c>, such as <c>_skip</c>, <c>_info</c> or <c>#ORTSMaxPower</c>.
    /// </summary>
    /// <param name="token">Raw token text. A quoted token includes its quotes, so it never starts a comment.</param>
    /// <returns>True when the token starts a comment block if it is followed by a block.</returns>
    public static bool IsCommentToken(ReadOnlySpan<char> token)
    {
        if (token.IsEmpty)
            return false;

        if (token[0] is '#' or '_')
            return true;

        foreach (string keyword in CommentKeywords)
        {
            if (token.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
