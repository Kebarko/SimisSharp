using KE.MSTS.SimisSharp.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace KE.MSTS.SimisSharp;

/// <summary>
/// Represents a node in the Simis document tree: a value, such as <c>10t</c>, or a block, whose name is its <see cref="Value"/>
/// and whose content is its child nodes, such as <c>Mass ( 10t )</c>. The root of a tree, such as <see cref="SimisDocument.Root"/>, has no value.
/// </summary>
/// <remarks>
/// Nodes are not thread-safe. Synchronize access when several threads use the same tree.
/// </remarks>
/// <param name="value">The value of the node, or null for the root of a tree.</param>
[DebuggerDisplay("Value='{Value}' Parent='{Parent?.Value}' Subnodes={subnodes == null ? 0 : subnodes.Count}")]
public class SimisNode(string? value = null)
{
    /// <summary>
    /// The child nodes. The list is created with the first child, so that values, which are most nodes of a tree, have none.
    /// </summary>
    private List<SimisNode>? subnodes;

    /// <summary>
    /// The read-only view <see cref="Nodes"/> returns, created on first use.
    /// </summary>
    private ReadOnlyCollection<SimisNode>? nodesView;

    /// <summary>
    /// The value of the node: a value, a block name, or the verbatim content of a comment block.
    /// Null for the root of a tree and for the placeholder of an empty block (see <see cref="IsDummyLeaf"/>).
    /// The escape sequences of a quoted value are decoded (see <see cref="SimisValue.Unescape"/>), so <c>"C:\\Routes"</c> has the value <c>C:\Routes</c>.
    /// An unquoted value is kept as written. The writer quotes and escapes a value when it needs to.
    /// Renaming a block can turn it into a comment block or back (see <see cref="IsComment"/>).
    /// </summary>
    /// <exception cref="ArgumentNullException">When setting null.</exception>
    [DisallowNull]
    public string? Value
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!string.Equals(field, value, StringComparison.Ordinal))
            {
                field = value;
                OriginalText = null;
            }
        }
    } = value;

    /// <summary>
    /// The value as it was written in the parsed text, including quotes, kept while <see cref="Value"/> is unchanged.
    /// The parser keeps it only where the writer would write the value differently: a quoted value with an escape sequence, a line feed or a tab,
    /// and an unquoted value with a backslash or an escaped double quote. Otherwise it is null.
    /// The writer reuses it so that unchanged values are written exactly as they were read.
    /// </summary>
    internal string? OriginalText { get; set; }

    /// <summary>
    /// The parent node. Null for the root of a tree, and for a node that has not been added to a parent or has been removed from it.
    /// </summary>
    public SimisNode? Parent { get; private set; }

    /// <summary>
    /// Formatting details for this node (leading/trailing blank lines, quoting, comment position).
    /// It is a view of the node: changes made through it change the node.
    /// </summary>
    public SimisNodeFormatting Formatting => new(this);

    // The formatting is kept in the node rather than in a separate object, so that parsing creates one object per node.

    /// <inheritdoc cref="SimisNodeFormatting.IsOnSameLineAsPrevious"/>
    internal bool IsOnSameLineAsPrevious { get; set; }

    /// <inheritdoc cref="SimisNodeFormatting.LeadingEmptyLines"/>
    internal byte LeadingEmptyLines { get; set; }

    /// <inheritdoc cref="SimisNodeFormatting.TrailingEmptyLines"/>
    internal byte TrailingEmptyLines { get; set; }

    /// <inheritdoc cref="SimisNodeFormatting.IsQuoted"/>
    internal bool IsQuoted { get; set; }

    /// <inheritdoc cref="SimisNodeFormatting.IsVerbatim"/>
    internal bool IsVerbatim { get; set; }

    /// <summary>
    /// True when node has no child nodes. An empty block, such as <c>Lights ( )</c>, is not a leaf: it has a placeholder child (see <see cref="IsDummyLeaf"/>).
    /// </summary>
    public bool IsLeaf => subnodes is not { Count: > 0 };

    /// <summary>
    /// True when node is a leaf and has no value. It is the placeholder child of an empty block, such as <c>Lights ( )</c>,
    /// which keeps the block written with its parentheses. Adding a child to an empty block replaces the placeholder.
    /// </summary>
    public bool IsDummyLeaf => IsLeaf && Value == null;

    /// <summary>
    /// True when the only child is the placeholder of an empty block.
    /// </summary>
    private bool HasOnlyPlaceholder => subnodes is { Count: 1 } && subnodes[0].IsDummyLeaf;

    /// <summary>
    /// The child nodes, creating the list when there is none yet.
    /// </summary>
    private List<SimisNode> Subnodes => subnodes ??= [];

    /// <summary>
    /// True when node has no parent and no value, as <see cref="SimisDocument.Root"/> has.
    /// </summary>
    public bool IsDummyRoot => Parent == null && Value == null;

    /// <summary>
    /// True when the node is a comment block, which MSTS and Open Rails ignore.
    /// A comment block is named <c>comment</c> or <c>skip</c> in any letter case, or has a name that begins with <c>#</c> or <c>_</c>.
    /// The name must not be quoted and the node must have child nodes, so a leaf such as <c>skip</c> is an ordinary value.
    /// A parsed comment block keeps its content in a single child node marked <see cref="SimisNodeFormatting.IsVerbatim"/>.
    /// Renaming a parsed comment block keeps that content verbatim, so its structure is available after the written text is parsed again.
    /// </summary>
    public bool IsComment => !IsLeaf && Value != null && !IsQuoted && TokenConfiguration.IsCommentToken(Value);

    /// <summary>
    /// Reads the value as a number without a unit (see <see cref="SimisValue.TryParseDouble(string?, out double)"/>).
    /// </summary>
    /// <param name="value">The number, or 0 when the value is not a number.</param>
    /// <returns>True when the value is a number.</returns>
    public bool TryGetDouble(out double value) => SimisValue.TryParseDouble(Value, out value);

    /// <summary>
    /// Reads the value as a number with an optional unit, such as <c>10t</c> (see <see cref="SimisValue.TryParseNumber(string?, out double, out string)"/>).
    /// </summary>
    /// <param name="value">The number, or 0 when the value does not start with a number.</param>
    /// <param name="unit">The unit as written, or an empty string when there is none.</param>
    /// <returns>True when the value starts with a number.</returns>
    public bool TryGetNumber(out double value, out string unit) => SimisValue.TryParseNumber(Value, out value, out unit);

    /// <summary>
    /// Reads the value as a signed 32-bit integer (see <see cref="SimisValue.TryParseInt32(string?, out int)"/>).
    /// </summary>
    /// <param name="value">The integer, or 0 when the value is not an integer in range.</param>
    /// <returns>True when the value is an integer in range.</returns>
    public bool TryGetInt32(out int value) => SimisValue.TryParseInt32(Value, out value);

    /// <summary>
    /// Reads the value as an unsigned 32-bit integer (see <see cref="SimisValue.TryParseUInt32(string?, out uint)"/>).
    /// </summary>
    /// <param name="value">The integer, or 0 when the value is not an unsigned integer in range.</param>
    /// <returns>True when the value is an unsigned integer in range.</returns>
    public bool TryGetUInt32(out uint value) => SimisValue.TryParseUInt32(Value, out value);

    /// <summary>
    /// Reads the value as a hexadecimal number without a prefix (see <see cref="SimisValue.TryParseHex(string?, out uint)"/>).
    /// </summary>
    /// <param name="value">The number, or 0 when the value is not a hexadecimal number in range.</param>
    /// <returns>True when the value is a hexadecimal number in range.</returns>
    public bool TryGetHex(out uint value) => SimisValue.TryParseHex(Value, out value);

    /// <summary>
    /// Reads the value as a boolean (see <see cref="SimisValue.TryParseBoolean(string?, out bool)"/>).
    /// </summary>
    /// <param name="value">The boolean, or false when the value is not a boolean.</param>
    /// <returns>True when the value is a boolean.</returns>
    public bool TryGetBoolean(out bool value) => SimisValue.TryParseBoolean(Value, out value);

    /// <summary>
    /// Sets the value to a number with an optional unit, independent of the current culture (see <see cref="SimisValue.Format(double, string?)"/>).
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="unit">The unit written after the number, such as <c>t</c> or <c>kN</c>, or null for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="value"/> is not finite.</exception>
    /// <exception cref="ArgumentException">When <paramref name="unit"/> would not be read back as the same unit.</exception>
    public void SetValue(double value, string? unit = null) => Value = SimisValue.Format(value, unit);

    /// <summary>
    /// Sets the value to a single precision number with an optional unit, independent of the current culture (see <see cref="SimisValue.Format(float, string?)"/>).
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="unit">The unit written after the number, such as <c>t</c> or <c>kN</c>, or null for none.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="value"/> is not finite.</exception>
    /// <exception cref="ArgumentException">When <paramref name="unit"/> would not be read back as the same unit.</exception>
    public void SetValue(float value, string? unit = null) => Value = SimisValue.Format(value, unit);

    /// <summary>
    /// Sets the value to an integer with an optional unit (see <see cref="SimisValue.Format(long, string?)"/>).
    /// </summary>
    /// <param name="value">The integer.</param>
    /// <param name="unit">The unit written after the integer, such as <c>kW</c>, or null for none.</param>
    /// <exception cref="ArgumentException">When <paramref name="unit"/> would not be read back as the same unit.</exception>
    public void SetValue(long value, string? unit = null) => Value = SimisValue.Format(value, unit);

    /// <summary>
    /// Sets the value to a hexadecimal number without a prefix (see <see cref="SimisValue.FormatHex"/>).
    /// </summary>
    /// <param name="value">The number.</param>
    /// <param name="digits">The minimum number of digits, padded with zeros.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="digits"/> is negative.</exception>
    public void SetHexValue(uint value, int digits = 8) => Value = SimisValue.FormatHex(value, digits);

    /// <summary>
    /// Child nodes in document order, as a read-only view that reflects later changes.
    /// Use <see cref="GetNodes()"/> for a copy that is safe to keep while the children change.
    /// </summary>
    public IReadOnlyList<SimisNode> Nodes => nodesView ??= Subnodes.AsReadOnly();

    /// <summary>
    /// Returns the index of <paramref name="subnode"/> among the child nodes, or -1 when it is not a child of this node.
    /// </summary>
    /// <param name="subnode">The node to look for.</param>
    /// <returns>The 0-based index, or -1.</returns>
    public int IndexOf(SimisNode subnode)
    {
        return subnodes?.IndexOf(subnode) ?? -1;
    }

    /// <summary>
    /// Returns a shallow copy list of child nodes.
    /// </summary>
    /// <returns>A new list of the child nodes in document order.</returns>
    public IList<SimisNode> GetNodes()
    {
        return subnodes is null ? [] : [.. subnodes];
    }

    /// <summary>
    /// Returns a shallow copy list of child nodes filtered by <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The condition a child node must meet.</param>
    /// <returns>A new list of the matching child nodes in document order.</returns>
    public IList<SimisNode> GetNodes(Func<SimisNode, bool> predicate)
    {
        List<SimisNode> result = [];
        if (subnodes is null)
            return result;

        foreach (SimisNode subnode in subnodes)
        {
            if (predicate(subnode))
                result.Add(subnode);
        }

        return result;
    }

    /// <summary>
    /// Find all child nodes with the specified <paramref name="value"/> using the given string comparison.
    /// By default letter case is ignored, as MSTS and Open Rails do.
    /// </summary>
    /// <param name="value">The value or block name to look for.</param>
    /// <param name="comparisonType">How the values are compared.</param>
    /// <returns>A new list of the matching child nodes in document order.</returns>
    public IList<SimisNode> GetNodes(string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        return GetNodes(c => string.Equals(c.Value, value, comparisonType));
    }

    /// <summary>
    /// Find the first child with the specified <paramref name="value"/> using the given string comparison, or null if none found.
    /// By default letter case is ignored, as MSTS and Open Rails do.
    /// </summary>
    /// <param name="value">The value or block name to look for.</param>
    /// <param name="comparisonType">How the values are compared.</param>
    /// <returns>The first matching child node, or null.</returns>
    public SimisNode? GetNode(string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        if (subnodes is null)
            return null;

        foreach (SimisNode subnode in subnodes)
        {
            if (string.Equals(subnode.Value, value, comparisonType))
                return subnode;
        }

        return null;
    }

    /// <summary>
    /// Adds a new child with the specified <paramref name="value"/> after the existing child nodes.
    /// Adding a child to an empty block replaces its placeholder (see <see cref="IsDummyLeaf"/>).
    /// </summary>
    /// <param name="value">The value of the new child node.</param>
    /// <returns>The new child node.</returns>
    /// <exception cref="InvalidOperationException">When this node has no value and is not the root of a tree, so the result would be a block without a name.</exception>
    public SimisNode AddNode(string value)
    {
        return AddNode(new SimisNode(value));
    }

    /// <summary>
    /// Adds the given <paramref name="subnode"/> after the existing child nodes and sets its parent reference.
    /// A node that already has a parent is moved: it is removed from its current parent first.
    /// Adding a child to an empty block replaces its placeholder (see <see cref="IsDummyLeaf"/>).
    /// </summary>
    /// <param name="subnode">The node to add.</param>
    /// <returns>The added node.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="subnode"/> is null.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="subnode"/> is this node or one of its ancestors,
    /// or when the result would be a block without a value, which cannot be written: this node has no value and is not the root of a tree,
    /// or <paramref name="subnode"/> has no value but has child nodes.</exception>
    public SimisNode AddNode(SimisNode subnode)
    {
        Attach(subnode);
        RemovePlaceholder(subnode);
        Subnodes.Add(subnode);
        return subnode;
    }

    /// <summary>
    /// Adds a node the parser has just created after the existing child nodes, without the checks of <see cref="AddNode(SimisNode)"/>.
    /// The parser only adds new nodes that have a value, and the placeholder of an empty block.
    /// </summary>
    /// <param name="subnode">The new node to add.</param>
    internal void AddParsedNode(SimisNode subnode)
    {
        subnode.Parent = this;
        Subnodes.Add(subnode);
    }

    /// <summary>
    /// Inserts a new child with the specified <paramref name="value"/> at <paramref name="index"/>.
    /// Inserting into an empty block replaces its placeholder (see <see cref="IsDummyLeaf"/>), so the only valid index is 0.
    /// </summary>
    /// <param name="index">The 0-based position of the new child node.</param>
    /// <param name="value">The value of the new child node.</param>
    /// <returns>The new child node.</returns>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is negative or greater than the number of child nodes.</exception>
    /// <exception cref="InvalidOperationException">When this node has no value and is not the root of a tree, so the result would be a block without a name.</exception>
    public SimisNode InsertNode(int index, string value)
    {
        return InsertNode(index, new SimisNode(value));
    }

    /// <summary>
    /// Inserts the given <paramref name="subnode"/> at <paramref name="index"/> and sets its parent reference.
    /// A node that already has a parent is moved: it is removed from its current parent first.
    /// When it is already a child of this node, <paramref name="index"/> refers to the child nodes without it.
    /// Inserting into an empty block replaces its placeholder (see <see cref="IsDummyLeaf"/>), so the only valid index is 0.
    /// </summary>
    /// <param name="index">The 0-based position of the inserted node.</param>
    /// <param name="subnode">The node to insert.</param>
    /// <returns>The inserted node.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="subnode"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is negative or greater than the number of child nodes.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="subnode"/> is this node or one of its ancestors,
    /// or when the result would be a block without a value, which cannot be written: this node has no value and is not the root of a tree,
    /// or <paramref name="subnode"/> has no value but has child nodes.</exception>
    public SimisNode InsertNode(int index, SimisNode subnode)
    {
        ArgumentNullException.ThrowIfNull(subnode);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        int count = HasOnlyPlaceholder ? 0 : (subnodes?.Count ?? 0) - (ReferenceEquals(subnode.Parent, this) ? 1 : 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, count);

        Attach(subnode);
        RemovePlaceholder(subnode);
        Subnodes.Insert(index, subnode);
        return subnode;
    }

    /// <summary>
    /// Replaces the child <paramref name="oldNode"/> with <paramref name="newNode"/> at the same position.
    /// The old node loses its parent reference. A new node that already has a parent is moved: it is removed from its current parent first.
    /// </summary>
    /// <param name="oldNode">The child node to replace.</param>
    /// <param name="newNode">The node to put in its place.</param>
    /// <returns>The new node.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="oldNode"/> or <paramref name="newNode"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="oldNode"/> is not a child of this node.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="newNode"/> is this node or one of its ancestors,
    /// or when <paramref name="newNode"/> has no value but has child nodes, which cannot be written.</exception>
    public SimisNode ReplaceNode(SimisNode oldNode, SimisNode newNode)
    {
        ArgumentNullException.ThrowIfNull(oldNode);
        ArgumentNullException.ThrowIfNull(newNode);

        if (!ReferenceEquals(oldNode.Parent, this))
            throw new ArgumentException("The node to replace is not a child of this node.", nameof(oldNode));

        if (ReferenceEquals(oldNode, newNode))
            return newNode;

        Attach(newNode);
        Subnodes[Subnodes.IndexOf(oldNode)] = newNode;
        oldNode.Parent = null;
        return newNode;
    }

    /// <summary>
    /// Removes all child nodes that have the specified value using the given string comparison.
    /// By default letter case is ignored, as MSTS and Open Rails do.
    /// A block that loses its last child stays an empty block (see <see cref="IsDummyLeaf"/>).
    /// </summary>
    /// <param name="value">The value or block name of the child nodes to remove.</param>
    /// <param name="comparisonType">How the values are compared.</param>
    public void RemoveNode(string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        if (subnodes is null)
            return;

        bool removed = false;
        for (int i = subnodes.Count - 1; i >= 0; i--)
        {
            SimisNode subnode = subnodes[i];
            if (string.Equals(subnode.Value, value, comparisonType))
            {
                subnodes.RemoveAt(i);
                subnode.Parent = null;
                removed = true;
            }
        }

        if (removed)
            AddPlaceholderIfEmpty();
    }

    /// <summary>
    /// Removes a specific child node and clears its parent reference. Does nothing when it is not a child of this node.
    /// A block that loses its last child stays an empty block (see <see cref="IsDummyLeaf"/>).
    /// </summary>
    /// <param name="subnode">The child node to remove.</param>
    public void RemoveNode(SimisNode subnode)
    {
        if (subnodes is not null && subnodes.Remove(subnode))
        {
            subnode.Parent = null;
            AddPlaceholderIfEmpty();
        }
    }

    /// <summary>
    /// Remove all child nodes and clear each child's parent reference.
    /// A block stays an empty block (see <see cref="IsDummyLeaf"/>), so <c>Lights ( 1 )</c> becomes <c>Lights ( )</c>, not <c>Lights</c>.
    /// </summary>
    public void RemoveAllNodes()
    {
        if (subnodes is not { Count: > 0 })
            return;

        foreach (SimisNode subnode in subnodes)
        {
            subnode.Parent = null;
        }
        subnodes.Clear();
        AddPlaceholderIfEmpty();
    }

    /// <summary>
    /// Adds the placeholder of an empty block when the last child has been removed, so the node stays a block.
    /// </summary>
    private void AddPlaceholderIfEmpty()
    {
        if (subnodes is { Count: > 0 })
            return;

        SimisNode placeholder = new() { Parent = this };
        Subnodes.Add(placeholder);
    }

    /// <summary>
    /// Removes the placeholder of an empty block before <paramref name="subnode"/> is added to it.
    /// </summary>
    /// <param name="subnode">The node about to be added.</param>
    private void RemovePlaceholder(SimisNode subnode)
    {
        if (HasOnlyPlaceholder && !ReferenceEquals(subnodes![0], subnode))
        {
            subnodes[0].Parent = null;
            subnodes.Clear();
        }
    }

    /// <summary>
    /// Detaches <paramref name="subnode"/> from its current parent and makes this node its parent.
    /// The caller adds it to the child nodes.
    /// </summary>
    /// <param name="subnode">The node about to be added.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="subnode"/> is null.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="subnode"/> is this node or one of its ancestors,
    /// or when the result would be a block without a name, which cannot be written.</exception>
    private void Attach(SimisNode subnode)
    {
        ArgumentNullException.ThrowIfNull(subnode);

        if (Value is null && Parent is not null)
            throw new InvalidOperationException("A node without a value cannot have child nodes unless it is the root of a tree. Set its Value to name the block.");

        if (subnode.Value is null && !subnode.IsLeaf)
            throw new InvalidOperationException("A node without a value that has child nodes can only be the root of a tree. Set its Value to name the block.");

        for (SimisNode? ancestor = this; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, subnode))
                throw new InvalidOperationException("A node cannot be added to itself or to one of its descendants.");
        }

        subnode.Parent?.RemoveNode(subnode);
        subnode.Parent = this;
    }

    /// <summary>
    /// Formatting metadata of a <see cref="SimisNode"/> such as quoting and blank line counts.
    /// It is a view of the node: the values are kept in the node, and setting them changes the node.
    /// </summary>
    [DebuggerDisplay("OnSameLine={IsOnSameLineAsPrevious}, Lead={LeadingEmptyLines}, Trail={TrailingEmptyLines}, Quoted={IsQuoted}, Verbatim={IsVerbatim}")]
    public sealed class SimisNodeFormatting
    {
        /// <summary>
        /// The node whose formatting this is.
        /// </summary>
        private readonly SimisNode node;

        /// <summary>
        /// Creates a view of the formatting of <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node whose formatting this is.</param>
        internal SimisNodeFormatting(SimisNode node)
        {
            this.node = node;
        }

        /// <summary>
        /// True when the node was parsed on the same line as the node before it. The parser never sets it on the first child of a block,
        /// even when it is on the line of the block name. The writer uses it to keep a comment block on the line of the node before it
        /// (see <see cref="WriterOptions.PreserveCommentPosition"/>).
        /// </summary>
        public bool IsOnSameLineAsPrevious
        {
            get => node.IsOnSameLineAsPrevious;
            set => node.IsOnSameLineAsPrevious = value;
        }

        /// <summary>
        /// Number of blank lines written before the node (see <see cref="WriterOptions.PreserveEmptyLines"/>).
        /// The parser sets it to the number of blank lines before the value or block name.
        /// </summary>
        public byte LeadingEmptyLines
        {
            get => node.LeadingEmptyLines;
            set => node.LeadingEmptyLines = value;
        }

        /// <summary>
        /// Number of blank lines written after the node, after its closing parenthesis when it is a block (see <see cref="WriterOptions.PreserveEmptyLines"/>).
        /// The parser stores the blank lines found before a closing parenthesis on the block that contains the last value or block name read before it,
        /// which may be a block nested in the block being closed.
        /// </summary>
        public byte TrailingEmptyLines
        {
            get => node.TrailingEmptyLines;
            set => node.TrailingEmptyLines = value;
        }

        /// <summary>
        /// True when the value was quoted in the parsed text. The writer keeps the quotes when <see cref="WriterOptions.PreserveQuotes"/> is true,
        /// and quotes a value that needs quotes either way (see <see cref="RequiresQuotes"/>). A quoted block name never makes a comment block (see <see cref="IsComment"/>).
        /// </summary>
        public bool IsQuoted
        {
            get => node.IsQuoted;
            set => node.IsQuoted = value;
        }

        /// <summary>
        /// True when the value is written exactly as it is, without quotes and without a separating space.
        /// The parser sets it on the content of a comment block. A verbatim value must keep its parentheses balanced, or the written text will not parse.
        /// </summary>
        public bool IsVerbatim
        {
            get => node.IsVerbatim;
            set => node.IsVerbatim = value;
        }

        /// <summary>
        /// Returns <paramref name="value"/> quoted and escaped when <paramref name="preserveQuotes"/> is true and the value was quoted,
        /// or when the value cannot be written without quotes (see <see cref="RequiresQuotes"/>).
        /// Unlike the writer, it neither reuses the text the value was read with nor keeps the quotes that strings joined with <c>+</c> need.
        /// </summary>
        /// <param name="value">The node value.</param>
        /// <param name="preserveQuotes">When true, respect the original quoting state.</param>
        /// <returns>Value wrapped with quotes if necessary, otherwise the original value, or empty string for null.</returns>
        public string GetQuotedValue(string? value, bool preserveQuotes)
        {
            if (value is null)
            {
                return string.Empty;
            }
            return (preserveQuotes && IsQuoted) || RequiresQuotes(value) ? Quote(value) : value;
        }

        /// <summary>
        /// Returns true when <paramref name="value"/> must be quoted to be read back as the same single value by SimisSharp and Open Rails:
        /// it is empty, or it contains whitespace, a parenthesis, a double quote or a backslash.
        /// </summary>
        /// <param name="value">The node value.</param>
        /// <returns>True when the value must be quoted.</returns>
        public static bool RequiresQuotes(string value)
        {
            return value.Length == 0 || value.AsSpan().ContainsAny(TokenConfiguration.CharactersRequiringQuotes);
        }

        /// <summary>
        /// Wraps <paramref name="value"/> in quotes, escaping it (see <see cref="SimisValue.Escape"/>).
        /// </summary>
        /// <param name="value">The value to quote.</param>
        /// <returns>The quoted and escaped value.</returns>
        internal static string Quote(string value)
        {
            return $"\"{SimisValue.Escape(value)}\"";
        }
    }
}
