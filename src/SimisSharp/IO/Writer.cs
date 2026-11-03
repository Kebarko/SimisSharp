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
    /// The line break the writer writes: CRLF, which MSTS uses, on every operating system.
    /// </summary>
    private const string NewLine = "\r\n";

    /// <summary>
    /// Write the provided document to text according to the options.
    /// Lines end with CRLF on every operating system, and text that is not empty ends with a line break.
    /// Text copied from the parsed file, such as a comment block or a quoted string that spans lines, keeps its own line breaks.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="options">Writer options, or null for the defaults.</param>
    /// <returns>The text of the document.</returns>
    public static string Write(SimisDocument document, WriterOptions? options = null)
    {
        options ??= new();

        StringBuilder sb = new();

        WriteRootNode(sb, document.Root, options);
        
        if (sb.Length > 0 && !EndsWithNewLine(sb))
            sb.Append(NewLine);

        return sb.ToString();
    }

    /// <summary>
    /// Writes nodes at the document root level, leaving out placeholders, and comment blocks unless they are preserved.
    /// </summary>
    /// <param name="sb">Destination StringBuilder to append to.</param>
    /// <param name="node">The root node of the document.</param>
    /// <param name="options">Writer options controlling formatting.</param>
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

        bool isCommentOnSameLine = node.IsComment && options.PreserveCommentPosition && node.IsOnSameLineAsPrevious;

        if (sb.Length > 0 && level == 0 && !isCommentOnSameLine)
            sb.Append(NewLine);

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.LeadingEmptyLines, options.CollapseEmptyLines);

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
                    lastIsVerbatim = subnode.IsVerbatim;
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
                                sb.Append(NewLine);
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
                    if (!inline && !(subnode.IsComment && options.PreserveCommentPosition && subnode.IsOnSameLineAsPrevious))
                    {
                        closeAtNextLine = true;
                        sb.Append(NewLine);
                    }

                    WriteNode(sb, subnode, options, level + 1, inline, keepQuotes);
                }
            }

            if (closeAtNextLine)
            {
                sb.Append(NewLine);
                sb.Append(indent);
            }
            else if (!lastIsVerbatim)
            {
                sb.Append(' ');
            }
            sb.Append(')');
        }

        WriteEmptyLines(sb, options.PreserveEmptyLines, node.TrailingEmptyLines, options.CollapseEmptyLines);
    }

    /// <summary>
    /// Resolve the configured column count. If <see cref="TableConfig.Columns"/> contains a number that is parsed, that value is returned; otherwise the value is looked up by path on the node.
    /// The count may come from the file, so the caller limits it to the number of values.
    /// </summary>
    /// <param name="tableConfig">The table configuration of the node.</param>
    /// <param name="node">The node written as a table.</param>
    /// <param name="ignoreCase">Whether to ignore case when following a path.</param>
    /// <returns>The column count, or 0 when it cannot be found.</returns>
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
    /// <param name="nodes">The nodes written at one level, in document order.</param>
    /// <param name="index">The index of the node in <paramref name="nodes"/>.</param>
    /// <param name="preserveQuotes">Whether quotes are preserved (see <see cref="WriterOptions.PreserveQuotes"/>).</param>
    /// <returns>True when the node value keeps its quotes.</returns>
    private static bool KeepQuotes(IList<SimisNode> nodes, int index, bool preserveQuotes)
    {
        if (preserveQuotes)
            return true;

        SimisNode node = nodes[index];
        if (!node.IsQuoted || node.Value is null)
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
    /// <param name="nodes">The nodes written at one level, in document order.</param>
    /// <param name="index">The index to check, which may be outside <paramref name="nodes"/>.</param>
    /// <returns>True when the node is an unquoted <c>+</c> value.</returns>
    private static bool IsConcatenationOperator(IList<SimisNode> nodes, int index)
    {
        return index >= 0 && index < nodes.Count && nodes[index] is { IsLeaf: true, Value: "+" } node && !node.IsQuoted;
    }

    /// <summary>
    /// Returns the text of a node value. A value that is unchanged since it was parsed keeps the text it was read from,
    /// unless its quotes are dropped. Any other value is quoted and escaped when it is quoted or needs quotes.
    /// </summary>
    /// <param name="node">The node whose value is written.</param>
    /// <param name="preserveQuotes">Whether a quoted value keeps its quotes (see <see cref="KeepQuotes"/>).</param>
    /// <returns>The text to write, or an empty string for a node without a value.</returns>
    private static string FormatValue(SimisNode node, bool preserveQuotes)
    {
        string? value = node.Value;
        if (value is null)
            return string.Empty;

        bool keepQuotes = preserveQuotes && node.IsQuoted;
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
    /// <param name="sb">Destination StringBuilder to append to.</param>
    /// <param name="preserveEmptyLines">Whether blank lines are written at all (see <see cref="WriterOptions.PreserveEmptyLines"/>).</param>
    /// <param name="count">The number of blank lines recorded for the node.</param>
    /// <param name="collapseEmptyLines">Whether to write at most one blank line (see <see cref="WriterOptions.CollapseEmptyLines"/>).</param>
    private static void WriteEmptyLines(StringBuilder sb, bool preserveEmptyLines, int count, bool collapseEmptyLines)
    {
        if (preserveEmptyLines)
        {
            for (int i = 0; i < count; i++)
            {
                if (i > 0 && collapseEmptyLines)
                    break;

                sb.Append(NewLine);
            }
        }
    }

    /// <summary>
    /// Returns true if the current StringBuilder ends with the line break the writer writes.
    /// </summary>
    /// <param name="sb">The text written so far.</param>
    /// <returns>True when the text ends with <see cref="NewLine"/>.</returns>
    private static bool EndsWithNewLine(StringBuilder sb)
    {
        if (sb.Length < NewLine.Length)
            return false;

        for (int i = 0; i < NewLine.Length; i++)
        {
            if (sb[sb.Length - NewLine.Length + i] != NewLine[i])
                return false;
        }

        return true;
    }
}
