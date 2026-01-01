using System.Text;

namespace KE.MSTS.SimisSharp.IO;

internal class Writer : IWriter
{
    private const char space = ' ';
    public string Write(SimisDocument document, SimisWriterOptions? options = null)
    {
        options ??= SimisWriterOptions.Default;

        StringBuilder sb = new();

        if (!string.IsNullOrEmpty(document.Header))
        {
            sb.AppendLine(document.Header);
        }

        foreach(SimisNode node in document.Root.GetNodes(n => !n.IsComment || options.PreserveComments))
        {
            WriteNode(sb, node, options);
        }

        return sb.ToString();
    }

    private static void WriteNode(StringBuilder sb, SimisNode node, SimisWriterOptions options, int indentLevel = 0)
    {
        string indent = new string(options.IndentCharacter, indentLevel * options.IndentSize);

        if (indentLevel == 0)
        {
            sb.AppendLine();
        }

        sb.Append(indent);
        sb.Append(node.QuotedValue);

        if (!node.IsLeaf)
        {
            bool closeAtNextLine = false;
            bool insideComment = false;

            sb.Append(space);
            sb.Append('(');
            foreach (SimisNode subNode in node.GetNodes(n => (!n.IsComment || options.PreserveComments) && !n.IsEmptyLeaf))
            {
                if (subNode.IsLeaf)
                {
                    insideComment = subNode.Parent?.IsComment == true;
                    if (insideComment)
                    {
                        sb.Append(subNode.Value);
                    }
                    else
                    {
                        sb.Append(space);
                        sb.Append(subNode.QuotedValue);
                    }
                }
                else
                {
                    closeAtNextLine = true;
                    sb.AppendLine();
                    WriteNode(sb, subNode, options, indentLevel + 1);
                }
            }

            if (closeAtNextLine)
            {
                sb.AppendLine();
                sb.Append(indent);
            }
            else if (!node.HasEmptySubNodes && !insideComment)
            {
                sb.Append(space);
            }
            sb.Append(')'); 
        }
    }
}
