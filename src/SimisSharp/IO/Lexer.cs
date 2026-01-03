namespace KE.MSTS.SimisSharp.IO;

internal class Lexer : ILexer
{
    private enum LexerState
    {
        Normal,
        Quoted,
        Unprocessed
    }

    public ICollection<string> Unprocessed => ["comment", "skip"];

    public IList<string> Lex(string text)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        List<string> tokens = [];
        ReadOnlySpan<char> span = text.AsSpan();

        int tokenStart = -1;
        byte carriageReturns = 0;
        byte lineFeeds = 0;
        byte unprocessedDepth = 0;
        LexerState state = LexerState.Normal;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            switch (state)
            {
                case LexerState.Normal:
                    if (c == '\r' || c == '\n')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        if (c == '\r')
                        {
                            carriageReturns++;
                        }
                        if (c == '\n')
                        {
                            lineFeeds++;
                        }
                        if (carriageReturns == 0 && lineFeeds >= 2 ||
                            lineFeeds == 0 && carriageReturns >= 2 ||
                            carriageReturns == lineFeeds && lineFeeds >= 2)
                        {
                            tokens.Add(Environment.NewLine);
                        }
                    }
                    else if (char.IsWhiteSpace(c))
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                    }
                    else if (c == '"')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        tokenStart = i + 1;
                        state = LexerState.Quoted;
                        carriageReturns = 0;
                        lineFeeds = 0;
                    }
                    else if (c == '(')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        tokens.Add(c.ToString());
                        carriageReturns = 0;
                        lineFeeds = 0;

                        if (state == LexerState.Unprocessed)
                        {
                            i--;
                        }
                    }
                    else if (c == ')')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
                        tokens.Add(c.ToString());
                        carriageReturns = 0;
                        lineFeeds = 0;
                    }
                    else if (tokenStart < 0)
                    {
                        tokenStart = i;
                        carriageReturns = 0;
                        lineFeeds = 0;
                    }
                    break;
                case LexerState.Quoted:
                    if (c == '"')
                    {
                        Emit(span, tokens, ref tokenStart, i - 1, ref state);
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
        }

        if (state == LexerState.Quoted)
            throw new FormatException("Unterminated quoted string.");

        if (state == LexerState.Unprocessed)
            throw new FormatException("Unterminated unprocessed block.");

        Emit(span, tokens, ref tokenStart, span.Length - 1, ref state);

        return tokens;
    }

    private void Emit(ReadOnlySpan<char> span, List<string> tokens, ref int start, int end, ref LexerState state)
    {
        if (start >= 0 && end < span.Length && end >= start)
        {
            string token = span.Slice(start, end - start + 1).ToString();
            tokens.Add(token);

            if (Unprocessed.Any(np => string.Equals(np, token, StringComparison.OrdinalIgnoreCase)))
            {
                state = LexerState.Unprocessed;
            }
        }

        start = -1;
    }
}
