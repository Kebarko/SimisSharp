using KE.MSTS.SimisSharp.IO;

namespace KE.MSTS.SimisSharp;

/// <summary>
/// Reads Simis text one token at a time, without building a tree, for tools that only read.
/// It is much faster than <see cref="SimisProcessor.Parse"/> and allocates almost nothing, because values are returned as parts of the text.
/// Comment blocks are skipped the way Open Rails skips them, unless comments are read.
/// The SIMIS header, such as <c>SIMISA@@@@@@@@@@JINX0D0t______</c>, is the first value at depth 0.
/// </summary>
/// <example>
/// <code>
/// SimisReader reader = new(processor.LoadText(path));
/// while (reader.Read())
/// {
///     if (reader.TokenType == SimisTokenType.StartBlock &amp;&amp; reader.ValueEquals("MaxPower")
///         &amp;&amp; reader.Read() &amp;&amp; reader.TryGetNumber(out double power, out string unit))
///     {
///         Console.WriteLine($"{power} {unit}");
///     }
/// }
/// </code>
/// </example>
public ref struct SimisReader
{
    /// <summary>
    /// The text being read.
    /// </summary>
    private readonly ReadOnlySpan<char> text;

    /// <summary>
    /// True when comment blocks are reported as tokens instead of being skipped.
    /// </summary>
    private readonly bool readComments;

    /// <summary>
    /// The index of the next character to read.
    /// </summary>
    private int position;

    /// <summary>
    /// The 1-based line of the character at <see cref="position"/>.
    /// </summary>
    private int line;

    /// <summary>
    /// The open blocks, three entries each: start and length of the name as written, and the line of the opening parenthesis.
    /// </summary>
    private int[]? openBlocks;

    /// <summary>
    /// The number of open blocks recorded in <see cref="openBlocks"/>.
    /// </summary>
    private int openBlockCount;

    /// <summary>
    /// The start of the current value or name in <see cref="text"/>, at the opening quote when it is quoted.
    /// </summary>
    private int tokenStart;

    /// <summary>
    /// The length of the current value or name as written, including its quotes.
    /// </summary>
    private int tokenLength;

    /// <summary>
    /// The start of the content of the current comment block in <see cref="text"/>.
    /// </summary>
    private int commentStart;

    /// <summary>
    /// The length of the content of the current comment block.
    /// </summary>
    private int commentLength;

    /// <summary>
    /// Creates a reader positioned before the first token of <paramref name="text"/>.
    /// </summary>
    /// <param name="text">Simis text, such as the result of <see cref="SimisProcessor.LoadText(string)"/>.</param>
    /// <param name="readComments">When true, comment blocks are reported as <see cref="SimisTokenType.Comment"/> tokens instead of being skipped.</param>
    public SimisReader(ReadOnlySpan<char> text, bool readComments = false)
    {
        this.text = text;
        this.readComments = readComments;
        line = 1;
    }

    /// <summary>
    /// The kind of the current token.
    /// </summary>
    public SimisTokenType TokenType { get; private set; }

    /// <summary>
    /// The number of blocks around the current token. A block's <see cref="SimisTokenType.StartBlock"/> and <see cref="SimisTokenType.EndBlock"/>
    /// tokens have the depth of the block's name, and the tokens inside it are one level deeper.
    /// </summary>
    public int Depth { get; private set; }

    /// <summary>
    /// The 1-based line where the current token starts.
    /// </summary>
    public int Line { get; private set; }

    /// <summary>
    /// True when the current value or block name is written in double quotes.
    /// </summary>
    public bool IsQuoted { get; private set; }

    /// <summary>
    /// The number of line breaks between the previous token and the current one.
    /// </summary>
    internal int LineBreaksBefore { get; private set; }

    /// <summary>
    /// The text of the current value, or the name of the current block or comment block, as written.
    /// A quoted value is returned without its quotes, but its escape sequences are not decoded (see <see cref="GetString"/>).
    /// Empty for <see cref="SimisTokenType.EndBlock"/>.
    /// </summary>
    public readonly ReadOnlySpan<char> ValueSpan => IsQuoted ? text.Slice(tokenStart + 1, tokenLength - 2) : text.Slice(tokenStart, tokenLength);

    /// <summary>
    /// The content of the current comment block between its parentheses, exactly as written. Empty for other tokens.
    /// </summary>
    public readonly ReadOnlySpan<char> CommentText => text.Slice(commentStart, commentLength);

    /// <summary>
    /// The text of the current value with escape sequences decoded, so that it equals <see cref="SimisNode.Value"/>.
    /// Use it only where the value is needed as a string, because it allocates.
    /// </summary>
    /// <returns>The decoded value, the block or comment block name, or an empty string for other tokens.</returns>
    public readonly string GetString()
    {
        return IsQuoted ? SimisValue.Unescape(ValueSpan) : ValueSpan.ToString();
    }

    /// <summary>
    /// Compares the current value with <paramref name="value"/>, by default ignoring letter case as MSTS and Open Rails do.
    /// Escape sequences of a quoted value are decoded first.
    /// </summary>
    /// <param name="value">The text to compare with, such as a block name.</param>
    /// <param name="comparisonType">How the values are compared.</param>
    /// <returns>True when the current value equals <paramref name="value"/>.</returns>
    public readonly bool ValueEquals(ReadOnlySpan<char> value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        return DecodedSpan.Equals(value, comparisonType);
    }

    /// <summary>
    /// Reads the current value as a number without a unit (see <see cref="SimisValue.TryParseDouble(ReadOnlySpan{char}, out double)"/>).
    /// </summary>
    /// <param name="value">The number, or 0 when the value is not a number.</param>
    /// <returns>True when the value is a number.</returns>
    public readonly bool TryGetDouble(out double value) => SimisValue.TryParseDouble(DecodedSpan, out value);

    /// <summary>
    /// Reads the current value as a number with an optional unit, such as <c>10t</c> (see <see cref="SimisValue.TryParseNumber(string?, out double, out string)"/>).
    /// </summary>
    /// <param name="value">The number, or 0 when the value does not start with a number.</param>
    /// <param name="unit">The unit as written, or an empty string when there is none.</param>
    /// <returns>True when the value starts with a number.</returns>
    public readonly bool TryGetNumber(out double value, out string unit)
    {
        bool parsed = SimisValue.TryParseNumber(DecodedSpan, out value, out ReadOnlySpan<char> unitSpan);
        unit = unitSpan.ToString();
        return parsed;
    }

    /// <summary>
    /// Reads the current value as a signed 32-bit integer (see <see cref="SimisValue.TryParseInt32(ReadOnlySpan{char}, out int)"/>).
    /// </summary>
    /// <param name="value">The integer, or 0 when the value is not an integer in range.</param>
    /// <returns>True when the value is an integer in range.</returns>
    public readonly bool TryGetInt32(out int value) => SimisValue.TryParseInt32(DecodedSpan, out value);

    /// <summary>
    /// Reads the current value as an unsigned 32-bit integer (see <see cref="SimisValue.TryParseUInt32(ReadOnlySpan{char}, out uint)"/>).
    /// </summary>
    /// <param name="value">The integer, or 0 when the value is not an unsigned integer in range.</param>
    /// <returns>True when the value is an unsigned integer in range.</returns>
    public readonly bool TryGetUInt32(out uint value) => SimisValue.TryParseUInt32(DecodedSpan, out value);

    /// <summary>
    /// Reads the current value as a hexadecimal number without a prefix (see <see cref="SimisValue.TryParseHex(ReadOnlySpan{char}, out uint)"/>).
    /// </summary>
    /// <param name="value">The number, or 0 when the value is not a hexadecimal number in range.</param>
    /// <returns>True when the value is a hexadecimal number in range.</returns>
    public readonly bool TryGetHex(out uint value) => SimisValue.TryParseHex(DecodedSpan, out value);

    /// <summary>
    /// Reads the current value as a boolean (see <see cref="SimisValue.TryParseBoolean(ReadOnlySpan{char}, out bool)"/>).
    /// </summary>
    /// <param name="value">The boolean, or false when the value is not a boolean.</param>
    /// <returns>True when the value is a boolean.</returns>
    public readonly bool TryGetBoolean(out bool value) => SimisValue.TryParseBoolean(DecodedSpan, out value);

    /// <summary>
    /// The current value with escape sequences decoded. Only a quoted value with a backslash allocates.
    /// </summary>
    private readonly ReadOnlySpan<char> DecodedSpan => IsQuoted && ValueSpan.Contains('\\') ? SimisValue.Unescape(ValueSpan) : ValueSpan;

    /// <summary>
    /// Moves to the next token.
    /// </summary>
    /// <returns>True when a token was read, false at the end of the text.</returns>
    /// <exception cref="SimisParseException">When the text is not well-formed: an unterminated quoted string or comment block,
    /// unbalanced parentheses, or a block without a name. <see cref="SimisParseException.Line"/> tells where.</exception>
    public bool Read()
    {
        while (true)
        {
            int lineBreaks = SkipWhiteSpace();

            TokenType = SimisTokenType.None;
            IsQuoted = false;
            tokenStart = tokenLength = commentStart = commentLength = 0;
            LineBreaksBefore = lineBreaks;
            Line = line;

            if (position >= text.Length)
            {
                if (openBlockCount > 0)
                    throw NotClosed();

                Depth = 0;
                return false;
            }

            char c = text[position];
            if (c == ')')
            {
                if (openBlockCount == 0)
                    throw new SimisParseException("Unexpected ')' without an open block.", line);

                position++;
                openBlockCount--;
                TokenType = SimisTokenType.EndBlock;
                Depth = openBlockCount;
                return true;
            }

            if (c == '(')
                throw new SimisParseException("Expected a block name before '('.", line);

            ReadValueText();

            int next = SkipWhiteSpace(position);
            if (next >= text.Length || text[next] != '(')
            {
                TokenType = SimisTokenType.Value;
                Depth = openBlockCount;
                return true;
            }

            line += text[position..next].Count('\n');
            position = next + 1;

            if (!IsQuoted && TokenConfiguration.IsCommentToken(ValueSpan))
            {
                int end = FindCommentBlockEnd(position);
                commentStart = position;
                commentLength = end - position;
                line += CommentText.Count('\n');
                position = end + 1;

                if (!readComments)
                    continue;

                TokenType = SimisTokenType.Comment;
                Depth = openBlockCount;
                return true;
            }

            PushOpenBlock();
            TokenType = SimisTokenType.StartBlock;
            Depth = openBlockCount - 1;
            return true;
        }
    }

    /// <summary>
    /// Skips the content of the current block, so that the reader is positioned on its <see cref="SimisTokenType.EndBlock"/>.
    /// Does nothing unless the current token is a <see cref="SimisTokenType.StartBlock"/>.
    /// </summary>
    /// <exception cref="SimisParseException">When the skipped text is not well-formed (see <see cref="Read"/>).</exception>
    public void Skip()
    {
        if (TokenType != SimisTokenType.StartBlock)
            return;

        int depth = Depth;
        while (Read() && !(TokenType == SimisTokenType.EndBlock && Depth == depth))
        {
        }
    }

    /// <summary>
    /// Skips whitespace from the current position and returns the number of line breaks skipped.
    /// </summary>
    /// <returns>The number of line breaks skipped.</returns>
    private int SkipWhiteSpace()
    {
        int lineBreaks = 0;
        while (position < text.Length)
        {
            char c = text[position];
            if (c == '\n')
                lineBreaks++;
            else if (!char.IsWhiteSpace(c))
                break;

            position++;
        }

        line += lineBreaks;
        return lineBreaks;
    }

    /// <summary>
    /// Returns the index of the first character at or after <paramref name="index"/> that is not whitespace.
    /// </summary>
    /// <param name="index">The index to start at.</param>
    /// <returns>The index, or the length of the text when only whitespace follows.</returns>
    private readonly int SkipWhiteSpace(int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index]))
            index++;

        return index;
    }

    /// <summary>
    /// Reads the value that starts at the current position and moves past it.
    /// A quoted value ends at the next double quote that is not escaped with a backslash.
    /// An unquoted value ends before whitespace, a parenthesis or a double quote, but a double quote escaped with a backslash is part of it.
    /// </summary>
    /// <exception cref="SimisParseException">When a quoted value is not terminated.</exception>
    private void ReadValueText()
    {
        int start = position;

        if (text[start] == '"')
        {
            int end = FindClosingQuote(start + 1);
            line += text[start..end].Count('\n');
            position = end + 1;
            IsQuoted = true;
        }
        else
        {
            position = FindValueEnd(start);
        }

        tokenStart = start;
        tokenLength = position - start;
    }

    /// <summary>
    /// Returns the index of the double quote that ends a quoted value whose content starts at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the first character after the opening quote.</param>
    /// <returns>The index of the closing quote.</returns>
    /// <exception cref="SimisParseException">When there is no such double quote.</exception>
    private readonly int FindClosingQuote(int index)
    {
        while (index < text.Length)
        {
            int found = text[index..].IndexOfAny('"', '\\');
            if (found < 0)
                break;

            index += found;
            if (text[index] == '"')
                return index;

            index += 2;
        }

        throw new SimisParseException("Unterminated quoted string.", line);
    }

    /// <summary>
    /// Returns the index after the end of an unquoted value that starts at <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The index of the first character of the value.</param>
    /// <returns>The index of the first character after the value.</returns>
    private readonly int FindValueEnd(int start)
    {
        int index = start;
        while (true)
        {
            int found = text[index..].IndexOfAny(TokenConfiguration.ValueEnds);
            if (found < 0)
                return text.Length;

            index += found;
            if (text[index] != '"' || !IsEscaped(start, index))
                return index;

            index++;
        }
    }

    /// <summary>
    /// Returns true when the character at <paramref name="index"/> follows an odd number of backslashes, counted back to <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The index of the first character of the value.</param>
    /// <param name="index">The index of the character to check.</param>
    /// <returns>True when the character is escaped.</returns>
    private readonly bool IsEscaped(int start, int index)
    {
        int backslash = index - 1;
        while (backslash >= start && text[backslash] == '\\')
            backslash--;

        return (index - backslash) % 2 == 0;
    }

    /// <summary>
    /// Records the block whose name is the current token and whose opening parenthesis is on the current line.
    /// </summary>
    private void PushOpenBlock()
    {
        int index = openBlockCount * 3;
        if (openBlocks is null || index == openBlocks.Length)
            Array.Resize(ref openBlocks, Math.Max(48, index * 2));

        openBlocks[index] = tokenStart;
        openBlocks[index + 1] = tokenLength;
        openBlocks[index + 2] = line;
        openBlockCount++;
    }

    /// <summary>
    /// Creates the exception for the innermost open block at the end of the text, reported at the line of its opening parenthesis.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private readonly SimisParseException NotClosed()
    {
        int index = (openBlockCount - 1) * 3;
        ReadOnlySpan<char> name = text.Slice(openBlocks![index], openBlocks[index + 1]);
        string value = name[0] == '"' ? SimisValue.Unescape(name[1..^1]) : name.ToString();

        return new SimisParseException($"The block '{value}' is not closed.", openBlocks[index + 2]);
    }

    /// <summary>
    /// Returns the 1-based line of the character at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the character.</param>
    /// <returns>The 1-based line.</returns>
    private readonly int GetLine(int index)
    {
        return text[..index].Count('\n') + 1;
    }

    /// <summary>
    /// Finds the closing parenthesis of a comment block whose content starts at <paramref name="start"/>.
    /// The content is skipped item by item the same way Open Rails skips a comment block, so parentheses inside quoted strings,
    /// including strings joined with <c>+</c>, and characters escaped with a backslash do not open or close a block.
    /// </summary>
    /// <param name="start">The index of the first character after the opening parenthesis.</param>
    /// <returns>Index of the closing parenthesis.</returns>
    /// <exception cref="SimisParseException">When the block or a quoted string inside it is not terminated.</exception>
    private readonly int FindCommentBlockEnd(int start)
    {
        int depth = 1;
        int i = start;

        while (true)
        {
            i = SkipStfWhiteSpace(i);
            if (i >= text.Length)
                throw new SimisParseException("Unterminated comment block.", GetLine(start - 1));

            char c = text[i++];
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
                    i = SkipQuotedItem(i);
                    break;
                default:
                    i = SkipPlainItem(i);
                    break;
            }
        }
    }

    /// <summary>
    /// Skips the rest of a quoted item whose opening quote precedes <paramref name="index"/>, including any strings appended with <c>+</c>.
    /// </summary>
    /// <param name="index">The index of the first character after the opening quote.</param>
    /// <returns>Index of the first character after the item.</returns>
    /// <exception cref="SimisParseException">When the quoted string is not terminated.</exception>
    private readonly int SkipQuotedItem(int index)
    {
        int quoteStart = index - 1;

        while (true)
        {
            if (index >= text.Length)
                throw new SimisParseException("Unterminated quoted string in comment block.", GetLine(quoteStart));

            char c = text[index++];
            if (c == '\\')
            {
                index++;
            }
            else if (c == '"')
            {
                int next = SkipStfWhiteSpace(index);
                if (next >= text.Length || text[next] != '+')
                    return index;

                next = SkipStfWhiteSpace(next + 1);
                if (next >= text.Length || text[next] != '"')
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
    /// <param name="index">The index of the second character of the item.</param>
    /// <returns>Index of the first character after the item.</returns>
    private readonly int SkipPlainItem(int index)
    {
        while (index < text.Length)
        {
            char c = text[index];
            if (c is '(' or ')')
                break;

            index++;
            if (c == '"' || IsStfWhiteSpace(c))
                break;

            if (c == '\\' && index < text.Length)
                index++;
        }

        return index;
    }

    /// <summary>
    /// Returns the index of the first character at or after <paramref name="index"/> that is not whitespace as Open Rails defines it.
    /// </summary>
    /// <param name="index">The index to start at.</param>
    /// <returns>The index, or the length of the text when only whitespace follows.</returns>
    private readonly int SkipStfWhiteSpace(int index)
    {
        while (index < text.Length && IsStfWhiteSpace(text[index]))
            index++;

        return index;
    }

    /// <summary>
    /// Whitespace as defined by the Open Rails STF reader: any control character or space.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True when the character is whitespace.</returns>
    private static bool IsStfWhiteSpace(char c) => c <= ' ';
}
