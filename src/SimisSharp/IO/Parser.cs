namespace KE.MSTS.SimisSharp.IO;

internal class Parser : IParser
{
    public SimisDocument Parse(IEnumerable<string> tokens)
    {
        SimisDocument document = new();
        SimisNode parent = document.Root;
        SimisNode node = document.Root;

        foreach (string token in tokens)
        {
            if (token is "(")
            {
                parent = node;
            }
            else if (token is ")")
            {
                if (ReferenceEquals(parent, node))
                    node.AddNode(new SimisNode(string.Empty));

                parent = parent.Parent ?? document.Root;
            }
            else if (token.StartsWith("SIMISA", StringComparison.Ordinal))
            {
                document.Header = token;
            }
            else
            {
                node = new(token);
                parent.AddNode(node);
            }
        }

        return document;
    }
}
