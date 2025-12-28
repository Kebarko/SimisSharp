namespace KE.MSTS.SimisSharp.IO;

internal class Lexer : ILexer
{
    private enum LexerState
    {
        Normal,
        Quoted,
        Comment
    }

    public IList<string> Lex(string text)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        List<string> tokens = [];
        ReadOnlySpan<char> span = text.AsSpan();

        int tokenStart = -1;
        int commentDepth = 0;
        LexerState state = LexerState.Normal;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            switch (state)
            {
                case LexerState.Normal:
                    if (char.IsWhiteSpace(c))
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                    }
                    else if (c == '"')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        tokenStart = i + 1;
                        state = LexerState.Quoted;
                    }
                    else if (c == '(' || c == ')')
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
                    if (c == '"')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        state = LexerState.Normal;
                    }
                    break;
                case LexerState.Comment:
                    if (c == '(')
                    {
                        if (commentDepth == 0)
                        {
                            tokens.Add(c.ToString());
                            tokenStart = i + 1;
                        }
                        commentDepth++;
                    }
                    else if (c == ')')
                    {
                        commentDepth--;
                        if (commentDepth == 0)
                        {
                            Emit(span, tokens, ref tokenStart, i - 1, ref state);
                            tokens.Add(c.ToString());
                            state = LexerState.Normal;
                        }
                    }
                    break;
            }
        }

        if (state == LexerState.Quoted)
            throw new FormatException("Unterminated quoted string.");

        if (state == LexerState.Comment)
            throw new FormatException("Unterminated comment block.");

        Emit(span, tokens, ref tokenStart, span.Length - 1, ref state);

        return tokens;
    }

    private static void Emit(ReadOnlySpan<char> span, List<string> tokens, ref int start, int end, ref LexerState state)
    {
        if (start >= 0 && end < span.Length && end >= start)
        {
            string token = span.Slice(start, end - start + 1).ToString();
            tokens.Add(token);
            start = -1;

            if (string.Equals("comment", token, StringComparison.OrdinalIgnoreCase) ||
                string.Equals("skip", token, StringComparison.OrdinalIgnoreCase))
            {
                state = LexerState.Comment;
            }
        }
    }
}
