namespace KE.MSTS.SimisSharp.IO;

internal class Parser : IParser
{
    public SimisDocument Parse(IEnumerable<string> tokens)
    {
        SimisDocument document = new();
        SimisNode parent = document.Root;
        SimisNode node = document.Root;
        byte emptyLines = 0;

        foreach (string token in tokens)
        {
            if (token is "(")
            {
                parent = node;
            }
            else if (token is ")")
            {
                if (ReferenceEquals(parent, node))
                    node.AddNode(new SimisNode());

                if (node.Parent is not null)
                {
                    node.Parent.Formatting.TrailingEmptyLines = emptyLines;
                }
                emptyLines = 0; 

                parent = parent.Parent ?? document.Root;
            }
            else if (token == Environment.NewLine)
            {
                emptyLines++;
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

                node.Formatting.LeadingEmptyLines = emptyLines;
                parent.AddNode(node);
                emptyLines = 0;
            }
        }

        return document;
    }
}
