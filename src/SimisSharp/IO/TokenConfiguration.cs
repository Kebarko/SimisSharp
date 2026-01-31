namespace KE.MSTS.SimisSharp.IO
{
    internal static class TokenConfiguration
    {
        public static readonly ISet<string> CommentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "comment",
            "skip",
            "_skip",
            "_info",
        };
    }
}
