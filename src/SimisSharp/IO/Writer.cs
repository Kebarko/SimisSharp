using System.Globalization;
using System.Text;
using static KE.MSTS.SimisSharp.IO.WriterOptions;

namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Writes a <see cref="SimisDocument"/> to a string honoring the settings in <see cref="WriterOptions"/>.
/// </summary>
internal static class Writer
{
    /// <summary>
    /// Write the provided document to text according to the options.
    /// </summary>
    public static string Write(SimisDocument document, WriterOptions? options = null)
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
        IList<SimisNode> subnodes = node.GetNodes(n => !n.IsDummyLeaf && (!n.IsComment || options.PreserveComments));
        for (int i = 0; i < subnodes.Count; i++)
        {
            WriteNode(sb, subnodes[i], options, 0, false, KeepQuotes(subnodes, i, options.PreserveQuotes));
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
    /// <param name="preserveQuotes">Whether the node value keeps its quotes (see <see cref="KeepQuotes"/>).</param>
    private static void WriteNode(StringBuilder sb, SimisNode node, WriterOptions options, int level, bool inline, bool preserveQuotes)
    {
        string indent = new(options.IndentCharacter, level * options.IndentSize);

        bool isCommentOnSameLine = node.IsComment && options.PreserveCommentPosition && node.Formatting.IsOnSameLineAsPrevious;

        if (sb.Length > 0 && level == 0 && !isCommentOnSameLine)
            sb.AppendLine();

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.LeadingEmptyLines, options.CollapseEmptyLines);

        if (inline || isCommentOnSameLine)
            sb.Append(' ');
        else
            sb.Append(indent);

        sb.Append(FormatValue(node, preserveQuotes));

        if (!inline && options.Inline.Any(path => SimisPathService.IsMatchForPath(node, path, options.IgnoreCase)))
        {
            inline = true;
        }

        if (!node.IsLeaf)
        {
            IList<SimisNode> subnodes = node.GetNodes(n => !n.IsDummyLeaf && (!n.IsComment || options.PreserveComments));

            bool closeAtNextLine = false;
            bool lastIsVerbatim = false;
            TableConfig? tableConfig = options.Table.FirstOrDefault(t => SimisPathService.IsMatchForPath(node, t.Name, options.IgnoreCase));
            int totalColumns = 0;
            int currentColumn = 0;
            int[] columnWidths = [];
            if (tableConfig != null)
            {
                totalColumns = Math.Clamp(GetColumns(tableConfig, node, options.IgnoreCase), 0, subnodes.Count);
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
                bool keepQuotes = KeepQuotes(subnodes, i, options.PreserveQuotes);

                if (subnode.IsLeaf)
                {
                    lastIsVerbatim = subnode.Formatting.IsVerbatim;
                    if (lastIsVerbatim)
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

                            string value = FormatValue(subnode, keepQuotes);
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
                            sb.Append(FormatValue(subnode, keepQuotes));
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

                    WriteNode(sb, subnode, options, level + 1, inline, keepQuotes);
                }
            }

            if (closeAtNextLine)
            {
                sb.AppendLine();
                sb.Append(indent);
            }
            else if (!lastIsVerbatim)
            {
                sb.Append(' ');
            }
            sb.Append(')');
        }

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.Formatting.TrailingEmptyLines, options.CollapseEmptyLines);
    }

    /// <summary>
    /// Resolve the configured column count. If <see cref="TableConfig.Columns"/> contains a number that is parsed, that value is returned; otherwise the value is looked up by path on the node.
    /// The count may come from the file, so the caller limits it to the number of values.
    /// </summary>
    private static int GetColumns(TableConfig tableConfig, SimisNode node, bool ignoreCase)
    {
        if (int.TryParse(tableConfig.Columns, NumberStyles.Integer, CultureInfo.InvariantCulture, out int columns))
            return columns;

        IList<SimisNode> sourceNodes = SimisPathService.FindNodesByPath(node, tableConfig.Columns, ignoreCase);
        if (sourceNodes.Count > 0 && int.TryParse(sourceNodes[0].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out columns))
            return columns;

        return 0;
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
                string value = FormatValue(nodes[nodeIdx], KeepQuotes(nodes, nodeIdx, preserveQuotes));

                if (value.Length > result[colIdx])
                    result[colIdx] = value.Length;
            }
        }

        return result;
    }

    /// <summary>
    /// Returns whether the node at <paramref name="index"/> keeps its quotes: always when <paramref name="preserveQuotes"/> is true,
    /// and otherwise when dropping them would change the meaning. A quoted value next to a <c>+</c> is part of a concatenated string,
    /// a quoted <c>+</c> would become the concatenation operator, and a quoted block name that is a comment token would make the block a comment.
    /// </summary>
    private static bool KeepQuotes(IList<SimisNode> nodes, int index, bool preserveQuotes)
    {
        if (preserveQuotes)
            return true;

        SimisNode node = nodes[index];
        if (!node.Formatting.IsQuoted || node.Value is null)
            return false;

        if (node.Value == "+")
            return true;

        if (!node.IsLeaf && TokenConfiguration.IsCommentToken(node.Value))
            return true;

        return IsConcatenationOperator(nodes, index - 1) || IsConcatenationOperator(nodes, index + 1);
    }

    /// <summary>
    /// Returns true when the node at <paramref name="index"/> is an unquoted <c>+</c> value, which joins the quoted strings around it.
    /// </summary>
    private static bool IsConcatenationOperator(IList<SimisNode> nodes, int index)
    {
        return index >= 0 && index < nodes.Count && nodes[index] is { IsLeaf: true, Value: "+" } node && !node.Formatting.IsQuoted;
    }

    /// <summary>
    /// Returns the text of a node value. A value that is unchanged since it was parsed keeps the text it was read from,
    /// unless its quotes are dropped. Any other value is quoted and escaped when it is quoted or needs quotes.
    /// </summary>
    private static string FormatValue(SimisNode node, bool preserveQuotes)
    {
        string? value = node.Value;
        if (value is null)
            return string.Empty;

        bool keepQuotes = preserveQuotes && node.Formatting.IsQuoted;
        bool quote = keepQuotes || SimisNode.SimisNodeFormatting.RequiresQuotes(value);

        string? original = node.OriginalText;
        if (original != null)
        {
            bool originalIsQuoted = original.Length > 0 && original[0] == '"';

            // A quoted token is reused while it stays quoted. An unquoted token reads back as the same value.
            if (originalIsQuoted ? quote : !keepQuotes)
                return original;
        }

        return quote ? SimisNode.SimisNodeFormatting.Quote(value) : value;
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
