namespace KE.MSTS.SimisSharp.IO
{
    /// <summary>
    /// Holds token configuration used by the IO components (lexer/parser/writer).
    /// </summary>
    internal static class TokenConfiguration
    {
        /// <summary>
        /// Set of tokens that indicate the start of an unprocessed/comment block.
        /// </summary>
        public static readonly ISet<string> CommentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "comment",
            "skip",
            "_skip",
            "_info",
        };
    }
}
