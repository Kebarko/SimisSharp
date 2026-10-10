using KE.MSTS.SimisSharp.IO;
using System.Diagnostics;

namespace KE.MSTS.SimisSharp;

[DebuggerDisplay("Value='{Value}' Parent='{Parent?.Value}' Subnodes={subnodes.Count}")]
/// <summary>
/// Represents a node in the Simis document tree. Nodes may be leaves (value) or containers (have child nodes). The root node is a dummy node where <see cref="Value"/> is null.
/// </summary>
public class SimisNode(string? value = null)
{
    private readonly IList<SimisNode> subnodes = [];

    /// <summary>
    /// The value of the node. Null for dummy nodes (including the root and empty placeholders).
    /// </summary>
    public string? Value { get; } = value;

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
    /// A parsed comment block keeps its content verbatim in a single child node.
    /// </summary>
    public bool IsComment => !IsLeaf && Value != null && !Formatting.IsQuoted && TokenConfiguration.IsCommentToken(Value);

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
    /// Adds a new leaf child with the specified <paramref name="value"/>.
    /// </summary>
    public void AddNode(string value)
    {
        AddNode(new SimisNode(value));
    }

    /// <summary>
    /// Adds the given <paramref name="subnode"/> as a child and sets its parent reference.
    /// </summary>
    public void AddNode(SimisNode subnode)
    {
        subnode.Parent = this;
        subnodes.Add(subnode);
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
    /// Holds formatting metadata for a <see cref="SimisNode"/> such as quoting and blank line counts.
    /// </summary>
    [DebuggerDisplay("OnSameLine={IsOnSameLineAsPrevious}, Lead={LeadingEmptyLines}, Trail={TrailingEmptyLines}, Quoted={IsQuoted}")]
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
        /// Returns the node value optionally wrapped in quotes based on <paramref name="preserveQoutes"/> and whether the value contains whitespace.
        /// </summary>
        /// <param name="value">The raw node value.</param>
        /// <param name="preserveQoutes">When true, respect the original quoting state.</param>
        /// <returns>Value wrapped with quotes if necessary, otherwise the original value, or empty string for null.</returns>
        public string GetQuotedValue(string? value, bool preserveQoutes)
        {
            if (value is null)
            {
                return string.Empty;
            }
            return (preserveQoutes && IsQuoted) || value.Contains(' ') ? $"\"{value}\"" : value;
        }
    }
}
