namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Default parser implementation that builds a <see cref="SimisDocument"/> from a token stream.
/// </summary>
internal class Parser : IParser
{
    /// <summary>
    /// Parse the provided tokens into a document tree. Handles node quoting and leading/trailing empty line counts.
    /// The content of a comment block is kept verbatim as a single child node.
    /// </summary>
    /// <exception cref="SimisParseException">When the parentheses are unbalanced, or a block has no name.</exception>
    public SimisDocument Parse(IEnumerable<string> tokens)
    {
        SimisDocument document = new();
        SimisNode parent = document.Root;
        SimisNode node = document.Root;
        // Line breaks since the previous token. Two line breaks in a row make one blank line.
        byte newLines = 0;
        bool isCommentHead = false;
        bool isCommentBody = false;
        // True when the previous token, ignoring line breaks, is a value that can name a block.
        bool isAfterValue = false;
        // The 1-based line of the current token, and the lines where the open blocks start.
        int line = 1;
        Stack<int> openLines = new();

        foreach (string token in tokens)
        {
            int tokenLine = line;
            line += token.AsSpan().Count('\n');

            if (isCommentBody && token is not ")")
            {
                // The comment content stays verbatim, even when it looks like a quoted string or a newline token.
                node = new(token);
                node.Formatting.IsVerbatim = true;
                parent.AddNode(node);
                isCommentBody = false;
                continue;
            }

            isCommentBody = false;

            if (token is "(")
            {
                if (!isAfterValue)
                    throw new SimisParseException("Expected a block name before '('.", tokenLine);

                parent = node;
                openLines.Push(tokenLine);
                newLines = 0;
                isCommentBody = isCommentHead;
                isCommentHead = false;
                isAfterValue = false;
            }
            else if (token is ")")
            {
                if (openLines.Count == 0)
                    throw new SimisParseException("Unexpected ')' without an open block.", tokenLine);

                if (ReferenceEquals(parent, node))
                    node.AddNode(new SimisNode());

                if (newLines > 1)
                    node.Parent?.Formatting.TrailingEmptyLines = (byte)(newLines - 1);

                parent = parent.Parent ?? document.Root;
                openLines.Pop();
                newLines = 0;
                isCommentHead = false;
                isAfterValue = false;
            }
            else if (token == Environment.NewLine)
            {
                newLines++;
            }
            else
            {
                if (token.Length > 1 && token[0] == '"' && token[^1] == '"')
                {
                    node = new(SimisValue.Unescape(token.AsSpan(1, token.Length - 2)));
                    node.Formatting.IsQuoted = true;
                }
                else
                {
                    node = new(token);
                }

                node.OriginalText = token;

                isCommentHead = TokenConfiguration.IsCommentToken(token);
                node.Formatting.IsOnSameLineAsPrevious = !parent.IsLeaf && newLines == 0;

                if (newLines > 1)
                    node.Formatting.LeadingEmptyLines = (byte)(newLines - 1);

                parent.AddNode(node);
                newLines = 0;
                isAfterValue = true;
            }
        }

        if (openLines.Count > 0)
            throw new SimisParseException($"The block '{parent.Value}' is not closed.", openLines.Peek());

        return document;
    }
}
