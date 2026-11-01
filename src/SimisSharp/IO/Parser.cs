namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Parser that builds a <see cref="SimisDocument"/> from Simis text in one pass, reading it with a <see cref="SimisReader"/>.
/// </summary>
internal static class Parser
{
    /// <summary>
    /// Parse <paramref name="text"/> into a document tree. Keeps node quoting and leading/trailing empty line counts.
    /// The content of a comment block is kept verbatim as a single child node.
    /// </summary>
    /// <exception cref="ArgumentNullException">When <paramref name="text"/> is null.</exception>
    /// <exception cref="SimisParseException">When the text is not well-formed (see <see cref="SimisReader.Read"/>).</exception>
    public static SimisDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        SimisDocument document = new();
        SimisNode parent = document.Root;
        // The node read last. Blank lines before a closing parenthesis are kept as trailing empty lines of its parent.
        SimisNode node = document.Root;
        SimisReader reader = new(text, readComments: true);

        while (reader.Read())
        {
            int lineBreaks = reader.LineBreaksBefore;

            switch (reader.TokenType)
            {
                case SimisTokenType.Value:
                    node = AddValue(parent, reader, lineBreaks);
                    break;

                case SimisTokenType.StartBlock:
                    node = AddValue(parent, reader, lineBreaks);
                    parent = node;
                    break;

                case SimisTokenType.Comment:
                    node = AddValue(parent, reader, lineBreaks);
                    if (reader.CommentText.IsEmpty)
                    {
                        node.AddParsedNode(new SimisNode());
                    }
                    else
                    {
                        // The comment content stays verbatim, even when it looks like quoted strings or blocks.
                        SimisNode content = new(reader.CommentText.ToString()) { IsVerbatim = true };
                        node.AddParsedNode(content);
                        node = content;
                    }
                    break;

                case SimisTokenType.EndBlock:
                    if (ReferenceEquals(parent, node))
                        node.AddParsedNode(new SimisNode());

                    if (lineBreaks > 1)
                        node.Parent?.TrailingEmptyLines = (byte)(lineBreaks - 1);

                    parent = parent.Parent ?? document.Root;
                    break;
            }
        }

        return document;
    }

    /// <summary>
    /// Adds a node for the current value, block name or comment block name of <paramref name="reader"/> to <paramref name="parent"/>.
    /// </summary>
    /// <param name="parent">The node to add to.</param>
    /// <param name="reader">The reader positioned on the value.</param>
    /// <param name="lineBreaks">The number of line breaks before the value.</param>
    /// <returns>The new node.</returns>
    private static SimisNode AddValue(SimisNode parent, in SimisReader reader, int lineBreaks)
    {
        string value = reader.GetString();
        SimisNode node = new(value)
        {
            IsQuoted = reader.IsQuoted,
            IsOnSameLineAsPrevious = !parent.IsLeaf && lineBreaks == 0,
            LeadingEmptyLines = lineBreaks > 1 ? (byte)(lineBreaks - 1) : (byte)0,
        };

        // The text as written is kept only where the writer would write the value differently: a quoted value with an escape sequence,
        // or a line feed or tab that the writer would escape, and an unquoted value with a backslash or an escaped double quote.
        if (reader.IsQuoted)
        {
            if (reader.ValueSpan.ContainsAny('\\', '\n', '\t'))
                node.OriginalText = string.Concat("\"", reader.ValueSpan, "\"");
        }
        else if (SimisNode.SimisNodeFormatting.RequiresQuotes(value))
        {
            node.OriginalText = value;
        }

        parent.AddParsedNode(node);
        return node;
    }
}
