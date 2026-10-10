namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Options that control how a <see cref="SimisDocument"/> is written to text. Configure indentation, comment/quote preservation, inline and table formatting.
/// </summary>
public class WriterOptions
{
    /// <summary>
    /// Number of characters per indent level.
    /// Default is 1.
    /// </summary>
    public int IndentSize { get; set; } = 1;

    /// <summary>
    /// Character used for indenting (for example '\t' or ' ').
    /// Default is tab ('\t').
    /// </summary>
    public char IndentCharacter { get; set; } = '\t';

    /// <summary>
    /// When true, writer preserves quotes on values that were originally quoted.
    /// Default is true.
    /// </summary>
    public bool PreserveQuotes { get; set; } = true;

    /// <summary>
    /// When true, writer preserves comment blocks (see <see cref="SimisNode.IsComment"/>) when writing.
    /// Default is true.
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// When true, preserves whether a comment was on the same line as the previous element.
    /// Default is true.
    /// </summary>
    public bool PreserveCommentPosition { get; set; } = true;

    /// <summary>
    /// When true, preserves empty lines recorded in node formatting.
    /// Default is true.
    /// </summary>
    public bool PreserveEmptyLines { get; set; } = true;

    /// <summary>
    /// When true, collapses multiple preserved empty lines into a single blank line.
    /// Default is true.
    /// </summary>
    public bool CollapseEmptyLines { get; set; } = true;

    /// <summary>
    /// When true, writer ignores case when matching paths for inline and table formatting.
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Collection of paths that should be written inline.
    /// Default is empty list.
    /// </summary>
    public List<string> Inline { get; set; } = [];

    /// <summary>
    /// Configuration for special-cased "table" style nodes where columns are aligned.
    /// Default is empty list.
    /// </summary>
    public List<TableConfig> Table { get; set; } = [];

    /// <summary>
    /// Configuration describing a table-like node: number of columns, separator, offset, fixed width.
    /// </summary>
    public class TableConfig
    {
        /// <summary>
        /// The path or literal number of columns. If a non-numeric string is provided, the parser will look up the value by path on the current node.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Either a numeric string for the number of columns or a path whose value resolves to the column count.
        /// </summary>
        public required string Columns { get; set; }

        /// <summary>
        /// Separator inserted between columns when formatting.
        /// Default is a single space.
        /// </summary>
        public string ColumnSeparator { get; set; } = " ";

        /// <summary>
        /// Number of initial values to skip before the table column layout applies.
        /// Default is 0 (no offset).
        /// </summary>
        public int Offset { get; set; } = 0;

        /// <summary>
        /// When true, column widths are computed and values are padded to match fixed column widths.
        /// Default is false.
        /// </summary>
        public bool FixedWidth { get; set; } = false;
    }
}
