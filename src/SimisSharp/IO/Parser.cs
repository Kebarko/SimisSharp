namespace KE.MSTS.SimisSharp.IO;

internal class Parser : IParser
{
    public SimisDocument Parse(IEnumerable<string> tokens)
    {
        SimisDocument document = new();
        SimisNode parent = document.Root;
        SimisNode node = document.Root;
        byte newLines = 0;

        foreach (string token in tokens)
        {
            if (token is "(")
            {
                parent = node;
                newLines = 0;
            }
            else if (token is ")")
            {
                if (ReferenceEquals(parent, node))
                    node.AddNode(new SimisNode());

                if (newLines > 1)
                    node.Parent?.Formatting.TrailingEmptyLines = newLines;

                parent = parent.Parent ?? document.Root;
                newLines = 0;
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

                node.Formatting.IsOnSameLineAsPrevious = !parent.IsLeaf && newLines == 0;

                if (newLines > 1)
                    node.Formatting.LeadingEmptyLines = newLines;

                parent.AddNode(node);
                newLines = 0;
            }
        }

        return document;
    }
}
