using System.Text;

namespace KE.MSTS.SimisSharp.IO;

internal class Writer : IWriter
{
    private const char space = ' ';
    public string Write(SimisDocument document, WriterOptions? options = null)
    {
        options ??= new();

        StringBuilder sb = new();

        foreach(SimisNode node in document.Root.GetNodes(n => !n.IsDummy && (!n.IsComment || options.PreserveComments)))
        {
            WriteNode(sb, node, options);
        }

        if (sb.Length > 0 && !EndsWithNewLine(sb))
        {
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void WriteNode(StringBuilder sb, SimisNode node, WriterOptions options, int indentLevel = 0)
    {
        string indent = new(options.IndentCharacter, indentLevel * options.IndentSize);

        if (sb.Length > 0 && indentLevel == 0)
        {
            sb.AppendLine();
        }

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.LeadingEmptyLines, options.CollapseEmptyLines);

        sb.Append(indent);
        sb.Append(node.Formatting.GetQuotedValue(node.Value, options.PreserveQuotes));

        if (!node.IsLeaf)
        {
            bool closeAtNextLine = false;
            bool insideComment = false;
            bool valuesInColumns = options.ValuesInColumns.TryGetValue(node.Value!, out int totalColumns);
            int currentColumn = 1;

            sb.Append(space);
            sb.Append('(');
            foreach (SimisNode subnode in node.GetNodes(n => !n.IsDummy && (!n.IsComment || options.PreserveComments)))
            {
                if (subnode.IsLeaf)
                {
                    insideComment = subnode.Parent?.IsComment == true;
                    if (insideComment)
                    {
                        sb.Append(subnode.Value);
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

                            sb.Append(subnode.Formatting.GetQuotedValue(subnode.Value, options.PreserveQuotes));

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
                            sb.Append(subnode.Formatting.GetQuotedValue(subnode.Value, options.PreserveQuotes));
                        }
                    }
                }
                else
                {
                    closeAtNextLine = true;
                    sb.AppendLine();
                    WriteNode(sb, subnode, options, indentLevel + 1);
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

    private static bool EndsWithNewLine(StringBuilder sb)
    {
        string newLine = Environment.NewLine;

        if (sb.Length < newLine.Length)
            return false;

        for (int i = 0; i < newLine.Length; i++)
        {
            if (sb[sb.Length - newLine.Length + i] != newLine[i])
                return false;
        }

        return true;
    }
}
