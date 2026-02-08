using System.Text;
using static KE.MSTS.SimisSharp.IO.WriterOptions;

namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Default implementation of <see cref="IWriter"/>. Writes a <see cref="SimisDocument"/> to a string honoring the settings in <see cref="WriterOptions"/>.
/// </summary>
internal class Writer : IWriter
{
    /// <summary>
    /// Write the provided document to text according to the options.
    /// </summary>
    public string Write(SimisDocument document, WriterOptions? options = null)
    {
        options ??= new();

        StringBuilder sb = new();

        WriteRootNode(sb, document.Root, options);
        
        if (sb.Length > 0 && !EndsWithNewLine(sb))
            sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Writes nodes at the document root level.
    /// </summary>
    private static void WriteRootNode(StringBuilder sb, SimisNode node, WriterOptions options)
    {
        foreach (SimisNode subnode in node.GetNodes(n => !n.IsDummyLeaf && (!n.IsComment || options.PreserveComments)))
        {
            WriteNode(sb, subnode, options, 0, false);
        }
    }

    /// <summary>
    /// Writes a single node and its children recursively.
    /// </summary>
    /// <param name="sb">Destination StringBuilder to append to.</param>
    /// <param name="node">Node to write.</param>
    /// <param name="options">Writer options controlling formatting.</param>
    /// <param name="level">Current indent level.</param>
    /// <param name="inline">Whether this node should be written inline with its parent.</param>
    private static void WriteNode(StringBuilder sb, SimisNode node, WriterOptions options, int level, bool inline)
    {
        string indent = new(options.IndentCharacter, level * options.IndentSize);

        if (sb.Length > 0 && level == 0)
            sb.AppendLine();

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.LeadingEmptyLines, options.CollapseEmptyLines);

        if (inline || (node.IsComment && options.PreserveCommentPosition && node.Formatting.IsOnSameLineAsPrevious))
            sb.Append(' ');
        else
            sb.Append(indent);

        sb.Append(node.Formatting.GetQuotedValue(node.Value, options.PreserveQuotes));

        if (!inline && options.Inline.Any(path => SimisPathService.IsMatchForPath(node, path)))
        {
            inline = true;
        }

        if (!node.IsLeaf)
        {
            IList<SimisNode> subnodes = node.GetNodes(n => !n.IsDummyLeaf && (!n.IsComment || options.PreserveComments));

            bool closeAtNextLine = false;
            bool insideComment = false;
            TableConfig? tableConfig = options.Table.SingleOrDefault(t => SimisPathService.IsMatchForPath(node, t.Name));
            int totalColumns = 0;
            int currentColumn = 0;
            int[] columnWidths = [];
            if (tableConfig != null)
            {
                totalColumns = GetColumns(tableConfig, node);
                if (tableConfig.FixedWidth)
                {
                    columnWidths = GetColumnWidths(subnodes, totalColumns, tableConfig.Offset, options.PreserveQuotes);
                }
            }

            sb.Append(' ');
            sb.Append('(');
            for (int i = 0; i < subnodes.Count; i++)
            {
                SimisNode subnode = subnodes[i];

                if (subnode.IsLeaf)
                {
                    insideComment = subnode.Parent?.IsComment == true;
                    if (insideComment)
                    {
                        sb.Append(subnode.Value);
                    }
                    else
                    {
                        if (tableConfig != null && totalColumns > 0 && i >= tableConfig.Offset)
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
                    if (!inline && !(subnode.IsComment && options.PreserveCommentPosition && subnode.Formatting.IsOnSameLineAsPrevious))
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

    /// <summary>
    /// Resolve the configured column count. If <see cref="TableConfig.Columns"/> contains a number that is parsed, that value is returned; otherwise the value is looked up by path on the node.
    /// </summary>
    private static int GetColumns(TableConfig tableConfig, SimisNode node)
    {
        int columns = 0;
        if (!int.TryParse(tableConfig.Columns, out columns))
        {
            IList<SimisNode> sourceNodes = SimisPathService.FindNodesByPath(node, tableConfig.Columns);
            if (sourceNodes.Count > 0)
            {
                _ = int.TryParse(sourceNodes[0].Value, out columns);
            }
        }

        return columns;
    }

    /// <summary>
    /// Compute maximum width for each column for fixed-width table output.
    /// </summary>
    /// <param name="nodes">Flat list of child nodes to measure.</param>
    /// <param name="totalColumns">Number of columns in the table.</param>
    /// <param name="offset">Index offset before the table columns start.</param>
    /// <param name="preserveQuotes">Whether quoted strings should keep their quotes when measuring.</param>
    /// <returns>Array of widths with length equal to <paramref name="totalColumns"/>.</returns>
    private static int[] GetColumnWidths(IList<SimisNode> nodes, int totalColumns, int offset, bool preserveQuotes)
    {
        var result = new int[totalColumns];

        for (int colIdx = 0; colIdx < totalColumns; colIdx++)
        {
            for (int nodeIdx = colIdx + offset; nodeIdx < nodes.Count; nodeIdx += totalColumns)
            {
                SimisNode node = nodes[nodeIdx];
                string value = node.Formatting.GetQuotedValue(node.Value, preserveQuotes);

                if (value.Length > result[colIdx])
                    result[colIdx] = value.Length;
            }
        }

        return result;
    }

    /// <summary>
    /// Append the requested number of empty lines to the builder, respecting collapse rules.
    /// </summary>
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

    /// <summary>
    /// Returns true if the current StringBuilder ends with the environment newline sequence.
    /// </summary>
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
