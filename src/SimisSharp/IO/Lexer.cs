namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Lexer for the Simis text format.
/// Produces tokens such as words, quoted strings, parentheses and newline tokens.
/// The content of a comment block is emitted verbatim as a single token between its parentheses.
/// </summary>
internal static class Lexer
{
    /// <summary>
    /// Tokenize the provided <paramref name="text"/>. Throws for unterminated quoted strings and comment blocks.
    /// </summary>
    /// <exception cref="ArgumentNullException">When <paramref name="text"/> is null.</exception>
    /// <exception cref="SimisParseException">When a quoted string or a comment block is not terminated.</exception>
    public static IList<string> Lex(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        List<string> tokens = [];
        ReadOnlySpan<char> span = text.AsSpan();

        int tokenStart = -1;
        bool escaped = false;
        bool quoted = false;
        bool commentPending = false;

        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];

            if (quoted)
            {
                if (c == '"' && !escaped)
                {
                    Emit(span, tokens, ref tokenStart, i, ref commentPending);
                    quoted = false;
                }
            }
            else if (c == '\n')
            {
                Emit(span, tokens, ref tokenStart, i - 1, ref commentPending);
                tokens.Add(Environment.NewLine);
            }
            else if (char.IsWhiteSpace(c))
            {
                Emit(span, tokens, ref tokenStart, i - 1, ref commentPending);
            }
            else if (c == '"' && !escaped)
            {
                Emit(span, tokens, ref tokenStart, i - 1, ref commentPending);
                tokenStart = i;
                quoted = true;
            }
            else if (c == '(')
            {
                Emit(span, tokens, ref tokenStart, i - 1, ref commentPending);
                tokens.Add("(");

                if (commentPending)
                {
                    int end = FindCommentBlockEnd(span, i + 1);
                    if (end > i + 1)
                        tokens.Add(span[(i + 1)..end].ToString());

                    tokens.Add(")");
                    commentPending = false;
                    i = end;
                }
            }
            else if (c == ')')
            {
                Emit(span, tokens, ref tokenStart, i - 1, ref commentPending);
                tokens.Add(")");
            }
            else if (tokenStart < 0)
            {
                tokenStart = i;
            }

            escaped = c == '\\' && !escaped;
        }

        if (quoted)
            throw new SimisParseException("Unterminated quoted string.", GetLine(span, tokenStart));

        Emit(span, tokens, ref tokenStart, span.Length - 1, ref commentPending);

        return tokens;
    }

    /// <summary>
    /// Emit a token from <paramref name="span"/> between indexes <paramref name="start"/> and <paramref name="end"/> into <paramref name="tokens"/>.
    /// Sets <paramref name="commentPending"/> when the token starts a comment block, which is when it is a comment token followed by a block.
    /// </summary>
    private static void Emit(ReadOnlySpan<char> span, List<string> tokens, ref int start, int end, ref bool commentPending)
    {
        if (start >= 0 && end >= start)
        {
            ReadOnlySpan<char> token = span[start..(end + 1)];
            tokens.Add(token.ToString());
            commentPending = TokenConfiguration.IsCommentToken(token) && IsFollowedByBlock(span, end + 1);
        }

        start = -1;
    }

    /// <summary>
    /// Returns the 1-based line of the character at <paramref name="index"/>.
    /// </summary>
    private static int GetLine(ReadOnlySpan<char> span, int index)
    {
        return span[..index].Count('\n') + 1;
    }

    /// <summary>
    /// Returns true when the first character at or after <paramref name="index"/> that is not whitespace opens a block.
    /// </summary>
    private static bool IsFollowedByBlock(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length && char.IsWhiteSpace(span[index]))
            index++;

        return index < span.Length && span[index] == '(';
    }

    /// <summary>
    /// Finds the closing parenthesis of a comment block whose content starts at <paramref name="start"/>.
    /// The content is skipped item by item the same way Open Rails skips a comment block, so parentheses inside quoted strings,
    /// including strings joined with <c>+</c>, and characters escaped with a backslash do not open or close a block.
    /// </summary>
    /// <returns>Index of the closing parenthesis.</returns>
    /// <exception cref="SimisParseException">When the block or a quoted string inside it is not terminated.</exception>
    private static int FindCommentBlockEnd(ReadOnlySpan<char> span, int start)
    {
        int depth = 1;
        int i = start;

        while (true)
        {
            i = SkipStfWhiteSpace(span, i);
            if (i >= span.Length)
                throw new SimisParseException("Unterminated comment block.", GetLine(span, start - 1));

            char c = span[i++];
            switch (c)
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    if (--depth == 0)
                        return i - 1;
                    break;
                case '"':
                    i = SkipQuotedItem(span, i);
                    break;
                default:
                    i = SkipPlainItem(span, i);
                    break;
            }
        }
    }

    /// <summary>
    /// Skips the rest of a quoted item whose opening quote precedes <paramref name="index"/>, including any strings appended with <c>+</c>.
    /// </summary>
    /// <returns>Index of the first character after the item.</returns>
    /// <exception cref="SimisParseException">When the quoted string is not terminated.</exception>
    private static int SkipQuotedItem(ReadOnlySpan<char> span, int index)
    {
        int quoteStart = index - 1;

        while (true)
        {
            if (index >= span.Length)
                throw new SimisParseException("Unterminated quoted string in comment block.", GetLine(span, quoteStart));

            char c = span[index++];
            if (c == '\\')
            {
                index++;
            }
            else if (c == '"')
            {
                int next = SkipStfWhiteSpace(span, index);
                if (next >= span.Length || span[next] != '+')
                    return index;

                next = SkipStfWhiteSpace(span, next + 1);
                if (next >= span.Length || span[next] != '"')
                    return next;

                quoteStart = next;
                index = next + 1;
            }
        }
    }

    /// <summary>
    /// Skips the rest of an unquoted item whose first character precedes <paramref name="index"/>.
    /// The item ends before a parenthesis, or after whitespace or a double quote. A backslash escapes the next character.
    /// </summary>
    /// <returns>Index of the first character after the item.</returns>
    private static int SkipPlainItem(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length)
        {
            char c = span[index];
            if (c is '(' or ')')
                break;

            index++;
            if (c == '"' || IsStfWhiteSpace(c))
                break;

            if (c == '\\' && index < span.Length)
                index++;
        }

        return index;
    }

    /// <summary>
    /// Returns the index of the first character at or after <paramref name="index"/> that is not whitespace.
    /// </summary>
    private static int SkipStfWhiteSpace(ReadOnlySpan<char> span, int index)
    {
        while (index < span.Length && IsStfWhiteSpace(span[index]))
            index++;

        return index;
    }

    /// <summary>
    /// Whitespace as defined by the Open Rails STF reader: any control character or space.
    /// </summary>
    private static bool IsStfWhiteSpace(char c) => c <= ' ';
}
