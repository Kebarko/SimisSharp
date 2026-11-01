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
    /// When true, writer preserves quotes on values that were originally quoted (see <see cref="SimisNode.SimisNodeFormatting.IsQuoted"/>).
    /// When false, quotes are dropped except where a value needs them (see <see cref="SimisNode.SimisNodeFormatting.RequiresQuotes"/>)
    /// or where dropping them would change the meaning: strings joined with <c>+</c>, a quoted <c>+</c>,
    /// and a quoted block name that would make the block a comment block.
    /// Default is true.
    /// </summary>
    public bool PreserveQuotes { get; set; } = true;

    /// <summary>
    /// When true, writer preserves comment blocks (see <see cref="SimisNode.IsComment"/>) when writing. When false, they are left out.
    /// Default is true.
    /// </summary>
    public bool PreserveComments { get; set; } = true;

    /// <summary>
    /// When true, a comment block that was parsed on the same line as the node before it stays on that line
    /// (see <see cref="SimisNode.SimisNodeFormatting.IsOnSameLineAsPrevious"/>). When false, it starts a new line like any other block.
    /// Default is true.
    /// </summary>
    public bool PreserveCommentPosition { get; set; } = true;

    /// <summary>
    /// When true, writes the blank lines recorded in <see cref="SimisNode.SimisNodeFormatting.LeadingEmptyLines"/>
    /// and <see cref="SimisNode.SimisNodeFormatting.TrailingEmptyLines"/>. When false, no blank lines are written.
    /// Default is true.
    /// </summary>
    public bool PreserveEmptyLines { get; set; } = true;

    /// <summary>
    /// When true, collapses multiple preserved empty lines into a single blank line. Applies only when <see cref="PreserveEmptyLines"/> is true.
    /// Default is true.
    /// </summary>
    public bool CollapseEmptyLines { get; set; } = true;

    /// <summary>
    /// When true, writer ignores case when matching paths for inline and table formatting.
    /// Default is true.
    /// </summary>
    public bool IgnoreCase { get; set; } = true;

    /// <summary>
    /// Collection of path patterns of nodes that should be written inline: a matching block and everything in it are written on one line
    /// (see <see cref="SimisPathService.IsMatchForPath"/>).
    /// Default is empty list.
    /// </summary>
    public List<string> Inline { get; set; } = [];

    /// <summary>
    /// Configuration for special-cased "table" style nodes, whose values are written in rows with a fixed number of columns,
    /// each row on its own line. When several configurations match a node, the first one in the list is used.
    /// Default is empty list.
    /// </summary>
    public List<TableConfig> Table { get; set; } = [];

    /// <summary>
    /// Configuration describing a table-like node: number of columns, separator, offset, fixed width.
    /// </summary>
    public class TableConfig
    {
        /// <summary>
        /// Path pattern of the nodes written as a table (see <see cref="SimisPathService.IsMatchForPath"/>).
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Either a numeric string for the number of columns, or a path relative to the table node whose first match holds the column count,
        /// such as <c>../[2]</c> (see <see cref="SimisPathService.FindNodesByPath"/>).
        /// The count is limited to the number of child nodes, and a block whose count is 0 or less, or cannot be found, is written normally.
        /// </summary>
        public required string Columns { get; set; }

        /// <summary>
        /// Separator inserted between columns when formatting.
        /// Default is a single space.
        /// </summary>
        public string ColumnSeparator { get; set; } = " ";

        /// <summary>
        /// Number of child nodes written normally before the table starts, such as a leading row count.
        /// Default is 0 (no offset).
        /// </summary>
        public int Offset { get; set; } = 0;

        /// <summary>
        /// When true, values are padded on the left to the width of the widest value in their column, so that numbers line up on the right.
        /// Default is false.
        /// </summary>
        public bool FixedWidth { get; set; } = false;
    }
}
