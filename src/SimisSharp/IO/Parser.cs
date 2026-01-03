namespace KE.MSTS.SimisSharp.IO;

internal class Parser : IParser
{
    public SimisDocument Parse(IEnumerable<string> tokens)
    {
        SimisDocument document = new();
        SimisNode parent = document.Root;
        SimisNode node = document.Root;
        int emptyLines = 0;

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

                if (node.Parent != null)
                {
                    node.Parent.EmptyLinesAfter = emptyLines;
                }
                emptyLines = 0; 

                parent = parent.Parent ?? document.Root;
            }
            else if (token == "\n")
            {
                emptyLines++;
            }
            else
            {
                node = new(token);
                node.EmptyLinesBefore = emptyLines;
                parent.AddNode(node);
                emptyLines = 0;
            }
        }

        return document;
    }
}
