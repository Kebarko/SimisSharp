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
    public SimisDocument Parse(IEnumerable<string> tokens)
    {
        SimisDocument document = new();
        SimisNode parent = document.Root;
        SimisNode node = document.Root;
        // Line breaks since the previous token. Two line breaks in a row make one blank line.
        byte newLines = 0;
        bool isCommentHead = false;
        bool isCommentBody = false;

        foreach (string token in tokens)
        {
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
                parent = node;
                newLines = 0;
                isCommentBody = isCommentHead;
                isCommentHead = false;
            }
            else if (token is ")")
            {
                if (ReferenceEquals(parent, node))
                    node.AddNode(new SimisNode());

                if (newLines > 1)
                    node.Parent?.Formatting.TrailingEmptyLines = (byte)(newLines - 1);

                parent = parent.Parent ?? document.Root;
                newLines = 0;
                isCommentHead = false;
            }
            else if (token == Environment.NewLine)
            {
                newLines++;
            }
            else
            {
                if (token.Length > 1 && token[0] == '"' && token[^1] == '"')
                {
                    node = new(token[1..^1]);
                    node.Formatting.IsQuoted = true;
                }
                else
                {
                    node = new(token);
                }

                isCommentHead = TokenConfiguration.IsCommentToken(token);
                node.Formatting.IsOnSameLineAsPrevious = !parent.IsLeaf && newLines == 0;

                if (newLines > 1)
                    node.Formatting.LeadingEmptyLines = (byte)(newLines - 1);

                parent.AddNode(node);
                newLines = 0;
            }
        }

        return document;
    }
}
