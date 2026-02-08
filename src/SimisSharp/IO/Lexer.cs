namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Default lexer implementation for Simis text format.
/// Produces tokens such as words, quoted strings, parentheses and newline tokens.
/// </summary>
internal class Lexer : ILexer
{
    /// <summary>
    /// Internal lexer state machine states.
    /// </summary>
    private enum LexerState
    {
        /// <summary>Normal scanning mode (not inside quotes or special blocks).</summary>
        Normal,
        /// <summary>Inside a quoted string.</summary>
        Quoted,
        /// <summary>Inside an unprocessed/comment block triggered by configured comment tokens.</summary>
        Unprocessed
    }

    /// <summary>
    /// Tokenize the provided <paramref name="text"/>. Throws for unterminated quoted/unprocessed blocks.
    /// </summary>
    /// <exception cref="ArgumentNullException">When <paramref name="text"/> is null.</exception>
    /// <exception cref="FormatException">When a quoted string or an unprocessed block is not terminated.</exception>
    public IList<string> Lex(string text)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        List<string> tokens = [];
        ReadOnlySpan<char> span = text.AsSpan();

        int tokenStart = -1;
        bool escaped = false;
        byte unprocessedDepth = 0;
        LexerState state = LexerState.Normal;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            switch (state)
            {
                case LexerState.Normal:
                    if (c == '\n')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        tokens.Add(Environment.NewLine);
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                    }
                    else if (c == '"' && !escaped)
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        tokenStart = i;
                        state = LexerState.Quoted;
                    }
                    else if (c == '(')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        if (state == LexerState.Unprocessed)
                            i--;
                        else
                            tokens.Add(c.ToString());
                    }
                    else if (c == ')')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        tokens.Add(c.ToString());
                    }
                    else if (tokenStart < 0)
                    {
                        tokenStart = i;
                    }
                    break;
                case LexerState.Quoted:
                    if (c == '"' && !escaped)
                    {
                        Emit(span, tokens, ref tokenStart, i, ref state);
                        state = LexerState.Normal;
                    }
                    break;
                case LexerState.Unprocessed:
                    if (c == '(')
                    {
                        if (unprocessedDepth == 0)
                        {
                            tokens.Add(c.ToString());
                            tokenStart = i + 1;
                        }
                        unprocessedDepth++;
                    }
                    else if (c == ')')
                    {
                        unprocessedDepth--;
                        if (unprocessedDepth == 0)
                        {
                            Emit(span, tokens, ref tokenStart, i - 1, ref state);
                            tokens.Add(c.ToString());
                            state = LexerState.Normal;
                        }
                    }
                    break;
            }

            escaped = c == '\\' && !escaped;
        }

        if (state == LexerState.Quoted)
            throw new FormatException("Unterminated quoted string.");

        if (state == LexerState.Unprocessed)
            throw new FormatException("Unterminated unprocessed block.");

        Emit(span, tokens, ref tokenStart, span.Length - 1, ref state);

        return tokens;
    }

    /// <summary>
    /// Emit a token from <paramref name="span"/> between indexes <paramref name="start"/> and <paramref name="end"/> into <paramref name="tokens"/> and update <paramref name="state"/> if the token is a comment token.
    /// </summary>
    private static void Emit(ReadOnlySpan<char> span, List<string> tokens, ref int start, int end, ref LexerState state)
    {
        if (start >= 0 && end < span.Length && end >= start)
        {
            string token = span.Slice(start, end - start + 1).ToString();
            tokens.Add(token);

            if (TokenConfiguration.CommentTokens.Contains(token))
            {
                state = LexerState.Unprocessed;
            }
        }

        start = -1;
    }
}
