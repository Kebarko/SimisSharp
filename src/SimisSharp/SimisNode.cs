using KE.MSTS.SimisSharp.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace KE.MSTS.SimisSharp;

[DebuggerDisplay("Value='{Value}' Parent='{Parent?.Value}' Subnodes={subnodes.Count}")]
/// <summary>
/// Represents a node in the Simis document tree. Nodes may be leaves (value) or containers (have child nodes). The root node is a dummy node where <see cref="Value"/> is null.
/// </summary>
public class SimisNode(string? value = null)
{
    private readonly List<SimisNode> subnodes = [];
    private ReadOnlyCollection<SimisNode>? nodesView;

    /// <summary>
    /// The value of the node: a token, a block name, or the verbatim content of a comment block.
    /// Null for dummy nodes (including the root and empty placeholders).
    /// The value is raw text, so escape sequences in quoted values, such as <c>\"</c>, are kept as written.
    /// Renaming a block can turn it into a comment block or back (see <see cref="IsComment"/>).
    /// </summary>
    /// <exception cref="ArgumentNullException">When setting null.</exception>
    [DisallowNull]
    public string? Value
    {
        get;
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    } = value;

    /// <summary>
    /// Parent node; null for the document root.
    /// </summary>
    public SimisNode? Parent { get; private set; }

    /// <summary>
    /// Formatting details for this node (leading/trailing blank lines, quoting, comment position).
    /// </summary>
    public SimisNodeFormatting Formatting { get; } = new SimisNodeFormatting();

    /// <summary>
    /// True when node has no child nodes.
    /// </summary>
    public bool IsLeaf => subnodes.Count == 0;

    /// <summary>
    /// True when node is a leaf and has no value (used to represent an empty element).
    /// </summary>
    public bool IsDummyLeaf => IsLeaf && Value == null;

    /// <summary>
    /// True when node is the dummy root (no parent and no value).
    /// </summary>
    public bool IsDummyRoot => Parent == null && Value == null;

    /// <summary>
    /// True when the node is a comment block, which MSTS and Open Rails ignore.
    /// A comment block is named <c>comment</c> or <c>skip</c> in any letter case, or has a name that begins with <c>#</c> or <c>_</c>.
    /// The name must not be quoted and the node must have child nodes, so a leaf such as <c>skip</c> is an ordinary value.
    /// A parsed comment block keeps its content in a single child node marked <see cref="SimisNodeFormatting.IsVerbatim"/>.
    /// Renaming a parsed comment block keeps that content verbatim, so its structure is available after the written text is parsed again.
    /// </summary>
    public bool IsComment => !IsLeaf && Value != null && !Formatting.IsQuoted && TokenConfiguration.IsCommentToken(Value);

    /// <summary>
    /// Child nodes in document order, as a read-only view that reflects later changes.
    /// Use <see cref="GetNodes()"/> for a copy that is safe to keep while the children change.
    /// </summary>
    public IReadOnlyList<SimisNode> Nodes => nodesView ??= subnodes.AsReadOnly();

    /// <summary>
    /// Returns the index of <paramref name="subnode"/> among the child nodes, or -1 when it is not a child of this node.
    /// </summary>
    public int IndexOf(SimisNode subnode)
    {
        return subnodes.IndexOf(subnode);
    }

    /// <summary>
    /// Returns a shallow copy list of child nodes.
    /// </summary>
    public IList<SimisNode> GetNodes()
    {
        return subnodes.ToList();
    }

    /// <summary>
    /// Returns a shallow copy list of child nodes filtered by <paramref name="predicate"/>.
    /// </summary>
    public IList<SimisNode> GetNodes(Func<SimisNode, bool> predicate)
    {
        return subnodes.Where(predicate).ToList();
    }

    /// <summary>
    /// Find all child nodes with the specified <paramref name="value"/> using the given string comparison.
    /// </summary>
    public IList<SimisNode> GetNodes(string value, StringComparison comparisonType = StringComparison.Ordinal)
    {
        return subnodes.Where(c => string.Equals(c.Value, value, comparisonType)).ToList();
    }

    /// <summary>
    /// Find the first child with the specified <paramref name="value"/> using the given string comparison, or null if none found.
    /// </summary>
    public SimisNode? GetNode(string value, StringComparison comparisonType = StringComparison.Ordinal)
    {
        return subnodes.FirstOrDefault(c => string.Equals(c.Value, value, comparisonType));
    }

    /// <summary>
    /// Adds a new child with the specified <paramref name="value"/> after the existing child nodes.
    /// </summary>
    /// <returns>The new child node.</returns>
    public SimisNode AddNode(string value)
    {
        return AddNode(new SimisNode(value));
    }

    /// <summary>
    /// Adds the given <paramref name="subnode"/> after the existing child nodes and sets its parent reference.
    /// A node that already has a parent is moved: it is removed from its current parent first.
    /// </summary>
    /// <returns>The added node.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="subnode"/> is null.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="subnode"/> is this node or one of its ancestors.</exception>
    public SimisNode AddNode(SimisNode subnode)
    {
        Attach(subnode);
        subnodes.Add(subnode);
        return subnode;
    }

    /// <summary>
    /// Inserts a new child with the specified <paramref name="value"/> at <paramref name="index"/>.
    /// </summary>
    /// <returns>The new child node.</returns>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is negative or greater than the number of child nodes.</exception>
    public SimisNode InsertNode(int index, string value)
    {
        return InsertNode(index, new SimisNode(value));
    }

    /// <summary>
    /// Inserts the given <paramref name="subnode"/> at <paramref name="index"/> and sets its parent reference.
    /// A node that already has a parent is moved: it is removed from its current parent first.
    /// When it is already a child of this node, <paramref name="index"/> refers to the child nodes without it.
    /// </summary>
    /// <returns>The inserted node.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="subnode"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is negative or greater than the number of child nodes.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="subnode"/> is this node or one of its ancestors.</exception>
    public SimisNode InsertNode(int index, SimisNode subnode)
    {
        ArgumentNullException.ThrowIfNull(subnode);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, ReferenceEquals(subnode.Parent, this) ? subnodes.Count - 1 : subnodes.Count);

        Attach(subnode);
        subnodes.Insert(index, subnode);
        return subnode;
    }

    /// <summary>
    /// Replaces the child <paramref name="oldNode"/> with <paramref name="newNode"/> at the same position.
    /// The old node loses its parent reference. A new node that already has a parent is moved: it is removed from its current parent first.
    /// </summary>
    /// <returns>The new node.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="oldNode"/> or <paramref name="newNode"/> is null.</exception>
    /// <exception cref="ArgumentException">When <paramref name="oldNode"/> is not a child of this node.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="newNode"/> is this node or one of its ancestors.</exception>
    public SimisNode ReplaceNode(SimisNode oldNode, SimisNode newNode)
    {
        ArgumentNullException.ThrowIfNull(oldNode);
        ArgumentNullException.ThrowIfNull(newNode);

        if (!ReferenceEquals(oldNode.Parent, this))
            throw new ArgumentException("The node to replace is not a child of this node.", nameof(oldNode));

        if (ReferenceEquals(oldNode, newNode))
            return newNode;

        Attach(newNode);
        subnodes[subnodes.IndexOf(oldNode)] = newNode;
        oldNode.Parent = null;
        return newNode;
    }

    /// <summary>
    /// Removes all child nodes that have the specified value using the given string comparison.
    /// </summary>
    public void RemoveNode(string value, StringComparison comparisonType = StringComparison.Ordinal)
    {
        for (int i = subnodes.Count - 1; i >= 0; i--)
        {
            SimisNode subnode = subnodes[i];
            if (string.Equals(subnode.Value, value, comparisonType))
            {
                subnodes.RemoveAt(i);
                subnode.Parent = null;
            }
        }
    }

    /// <summary>
    /// Removes a specific child node and clears its parent reference.
    /// </summary>
    public void RemoveNode(SimisNode subnode)
    {
        if (subnodes.Remove(subnode))
        {
            subnode.Parent = null;
        }
    }

    /// <summary>
    /// Remove all child nodes and clear each child's parent reference.
    /// </summary>
    public void RemoveAllNodes()
    {
        foreach (SimisNode subnode in subnodes)
        {
            subnode.Parent = null;
        }
        subnodes.Clear();
    }

    /// <summary>
    /// Detaches <paramref name="subnode"/> from its current parent and makes this node its parent.
    /// The caller adds it to the child nodes.
    /// </summary>
    /// <exception cref="ArgumentNullException">When <paramref name="subnode"/> is null.</exception>
    /// <exception cref="InvalidOperationException">When <paramref name="subnode"/> is this node or one of its ancestors.</exception>
    private void Attach(SimisNode subnode)
    {
        ArgumentNullException.ThrowIfNull(subnode);

        for (SimisNode? ancestor = this; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ReferenceEquals(ancestor, subnode))
                throw new InvalidOperationException("A node cannot be added to itself or to one of its descendants.");
        }

        subnode.Parent?.RemoveNode(subnode);
        subnode.Parent = this;
    }

    /// <summary>
    /// Holds formatting metadata for a <see cref="SimisNode"/> such as quoting and blank line counts.
    /// </summary>
    [DebuggerDisplay("OnSameLine={IsOnSameLineAsPrevious}, Lead={LeadingEmptyLines}, Trail={TrailingEmptyLines}, Quoted={IsQuoted}, Verbatim={IsVerbatim}")]
    public sealed class SimisNodeFormatting()
    {
        /// <summary>
        /// True when this node was parsed as being on the same line as the previous node.
        /// </summary>
        public bool IsOnSameLineAsPrevious { get; set; }

        /// <summary>
        /// Number of blank lines that preceded the node when parsed.
        /// </summary>
        public byte LeadingEmptyLines { get; set; }

        /// <summary>
        /// Number of blank lines that followed the node when parsed.
        /// </summary>
        public byte TrailingEmptyLines { get; set; }

        /// <summary>
        /// True when the node value was originally quoted in the input.
        /// </summary>
        public bool IsQuoted { get; set; }

        /// <summary>
        /// True when the value is written exactly as it is, without quotes and without a separating space.
        /// The parser sets it on the content of a comment block. A verbatim value must keep its parentheses balanced, or the written text will not parse.
        /// </summary>
        public bool IsVerbatim { get; set; }

        /// <summary>
        /// Returns the node value wrapped in quotes when <paramref name="preserveQuotes"/> is true and the value was quoted,
        /// or when the value cannot be written without quotes (see <see cref="RequiresQuotes"/>).
        /// </summary>
        /// <param name="value">The raw node value.</param>
        /// <param name="preserveQuotes">When true, respect the original quoting state.</param>
        /// <returns>Value wrapped with quotes if necessary, otherwise the original value, or empty string for null.</returns>
        public string GetQuotedValue(string? value, bool preserveQuotes)
        {
            if (value is null)
            {
                return string.Empty;
            }
            return (preserveQuotes && IsQuoted) || RequiresQuotes(value) ? $"\"{value}\"" : value;
        }

        /// <summary>
        /// Returns true when <paramref name="value"/> would not be read back as the same single token without quotes:
        /// it is empty, or it contains whitespace, a parenthesis or a double quote.
        /// </summary>
        /// <param name="value">The raw node value.</param>
        public static bool RequiresQuotes(string value)
        {
            if (value.Length == 0)
                return true;

            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c) || c is '(' or ')' or '"')
                    return true;
            }

            return false;
        }
    }
}
