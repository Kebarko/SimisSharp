using System.Text;
using static KE.MSTS.SimisSharp.IO.WriterOptions;

namespace KE.MSTS.SimisSharp.IO;

internal class Writer : IWriter
{
    public string Write(SimisDocument document, WriterOptions? options = null)
    {
        options ??= new();

        StringBuilder sb = new();

        WriteRootNode(sb, document.Root, options);
        
        if (sb.Length > 0 && !EndsWithNewLine(sb))
            sb.AppendLine();

        return sb.ToString();
    }

    private static void WriteRootNode(StringBuilder sb, SimisNode node, WriterOptions options)
    {
        foreach (SimisNode subnode in node.GetNodes(n => !n.IsDummyLeaf && (!n.IsComment || options.PreserveComments)))
        {
            WriteNode(sb, subnode, options, 0, false);
        }
    }

    private static void WriteNode(StringBuilder sb, SimisNode node, WriterOptions options, int level, bool inline)
    {
        string indent = new(options.IndentCharacter, level * options.IndentSize);

        if (sb.Length > 0 && level == 0)
            sb.AppendLine();

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.LeadingEmptyLines, options.CollapseEmptyLines);

        if (inline)
            sb.Append(' ');
        else
            sb.Append(indent);

        sb.Append(node.Formatting.GetQuotedValue(node.Value, options.PreserveQuotes));

        if (!inline && options.Inline.Any(node.IsMatchForPath))
        {
            inline = true;
        }

        if (!node.IsLeaf)
        {
            IReadOnlyList<SimisNode> subnodes = node.GetNodes(n => !n.IsDummyLeaf && (!n.IsComment || options.PreserveComments));

            bool closeAtNextLine = false;
            bool insideComment = false;
            TableConfig? tableConfig = options.Table.SingleOrDefault(t => node.IsMatchForPath(t.Name));
            int totalColumns = 0;
            int currentColumn = 0;
            int[] columnWidths = [];
            if (tableConfig != null)
            {
                totalColumns = GetColumns(tableConfig, node);
                if (tableConfig.FixedWidth)
                {
                    columnWidths = GetColumnWidths(subnodes, totalColumns, options);
                }
            }

            sb.Append(' ');
            sb.Append('(');
            foreach (SimisNode subnode in subnodes)
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
                        if (tableConfig != null && totalColumns > 0 && !inline)
                        {
                            if (currentColumn == 0)
                            {
                                sb.AppendLine();
                                sb.Append(indent);
                                sb.Append(new string(options.IndentCharacter, options.IndentSize));
                            }
                            else
                            {
                                sb.Append(tableConfig.ColumnSeparator);
                            }

                            string value = subnode.Formatting.GetQuotedValue(subnode.Value, options.PreserveQuotes);
                            if (columnWidths.Length > currentColumn && value.Length < columnWidths[currentColumn])
                            {
                                value = value.PadLeft(columnWidths[currentColumn]);
                            }

                            sb.Append(value);

                            currentColumn++;
                            if (currentColumn >= totalColumns)
                            {
                                currentColumn = 0;
                            }

                            closeAtNextLine = true;
                        }
                        else
                        {
                            sb.Append(' ');
                            sb.Append(subnode.Formatting.GetQuotedValue(subnode.Value, options.PreserveQuotes));
                        }
                    }
                }
                else
                {
                    if (!inline)
                    {
                        closeAtNextLine = true;
                        sb.AppendLine(); 
                    }

                    WriteNode(sb, subnode, options, level + 1, inline);
                }
            }

            if (closeAtNextLine)
            {
                sb.AppendLine();
                sb.Append(indent);
            }
            else if (!insideComment)
            {
                sb.Append(' ');
            }
            sb.Append(')');
        }

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.TrailingEmptyLines, options.CollapseEmptyLines);
    }

    private static int GetColumns(TableConfig tableConfig, SimisNode node)
    {
        int columns = 0;
        if (!int.TryParse(tableConfig.Columns, out columns))
        {
            IReadOnlyList<SimisNode> sourceNodes = node.FindNodesByPath(tableConfig.Columns);
            if (sourceNodes.Count > 0)
            {
                _ = int.TryParse(sourceNodes[0].Value, out columns);
            }
        }

        return columns;
    }

    private static int[] GetColumnWidths(IReadOnlyList<SimisNode> nodes, int totalColumns, WriterOptions options)
    {
        var result = new int[totalColumns];

        for (int colIdx = 0; colIdx < totalColumns; colIdx++)
        {
            for (int nodeIdx = colIdx; nodeIdx < nodes.Count; nodeIdx += totalColumns)
            {
                SimisNode node = nodes[nodeIdx];
                string value = node.Formatting.GetQuotedValue(node.Value, options.PreserveQuotes);

                if (value.Length > result[colIdx])
                    result[colIdx] = value.Length;
            }
        }

        return result;
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
