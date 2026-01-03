using System.Text;

namespace KE.MSTS.SimisSharp.IO;

internal class Writer : IWriter
{
    private const char space = ' ';
    public string Write(SimisDocument document, WriterOptions? options = null)
    {
        options ??= new();

        StringBuilder sb = new();

        foreach(SimisNode node in document.Root.GetNodes(n => !n.IsComment || options.PreserveComments))
        {
            WriteNode(sb, node, options);
        }

        return sb.ToString();
    }

    private static void WriteNode(StringBuilder sb, SimisNode node, WriterOptions options, int indentLevel = 0)
    {
        string indent = new string(options.IndentCharacter, indentLevel * options.IndentSize);

        if (sb.Length > 0 && indentLevel == 0)
        {
            sb.AppendLine();
        }

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.LeadingEmptyLines, options.CollapseEmptyLines);

        sb.Append(indent);
        sb.Append(node.Formatting.GetQuotedValue(node.Value));

        if (!node.IsLeaf)
        {
            bool closeAtNextLine = false;
            bool insideComment = false;
            bool valuesInColumns = options.ValuesInColumns.TryGetValue(node.Value, out int totalColumns);
            int currentColumn = 1;

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
                        if (valuesInColumns)
                        {
                            if (currentColumn == 1)
                            {
                                sb.AppendLine();
                                sb.Append(indent);
                                sb.Append(new string(options.IndentCharacter, options.IndentSize));
                            }
                            else
                            {
                                sb.Append(space);
                            }

                            sb.Append(subNode.Formatting.GetQuotedValue(subNode.Value));

                            currentColumn++;
                            if (currentColumn > totalColumns)
                            {
                                currentColumn = 1;
                            }

                            closeAtNextLine = true;
                        }
                        else
                        {
                            sb.Append(space);
                            sb.Append(subNode.Formatting.GetQuotedValue(subNode.Value));
                        }
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
            else if (!insideComment)
            {
                sb.Append(space);
            }
            sb.Append(')');

            WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.TrailingEmptyLines, options.CollapseEmptyLines);
        }
    }

    private static void WriteEmptyLines(StringBuilder sb, bool preserveEmptyLines, int count, bool collapseEmptyLines)
    {
        if (preserveEmptyLines)
        {
            for (int i = 0; i < count; i++)
            {
                if (i > 0 && collapseEmptyLines)
                    break;

                sb.AppendLine();
            }
        }
    }
}
