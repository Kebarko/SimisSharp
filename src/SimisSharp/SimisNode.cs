using KE.MSTS.SimisSharp.IO;
using System.Diagnostics;

namespace KE.MSTS.SimisSharp;

[DebuggerDisplay("Value='{Value}' Parent='{Parent?.Value}' Subnodes='{subnodes.Count}'")]
public class SimisNode(string? value = null)
{
    private readonly IList<SimisNode> subnodes = [];

    public string? Value { get; } = value;

    public SimisNode? Parent { get; private set; }

    public SimisNodeFormatting Formatting { get; } = new SimisNodeFormatting();

    public bool IsLeaf => subnodes.Count == 0;

    public bool IsDummyLeaf => IsLeaf && Value == null;

    public bool IsDummyRoot => Parent == null && Value == null;

    public bool IsComment => Value != null && TokenConfiguration.CommentTokens.Contains(Value);

    public SimisNode GetRoot()
    {
        SimisNode node = this;
        while (node.Parent != null)
        {
            node = node.Parent;
        }
        return node;
    }

    public IReadOnlyList<SimisNode> GetNodes()
    {
        return subnodes.ToList();
    }

    public IReadOnlyList<SimisNode> GetNodes(Func<SimisNode, bool> predicate)
    {
        return subnodes.Where(predicate).ToList();
    }

    public IReadOnlyList<SimisNode> GetNodes(string value)
    {
        return subnodes.Where(c => c.Value == value).ToList();
    }

    public SimisNode? GetNode(string value)
    {
        return subnodes.FirstOrDefault(c => c.Value == value);
    }

    public void AddNode(string value)
    {
        AddNode(new SimisNode(value));
    }

    public void AddNode(SimisNode subnode)
    {
        subnode.Parent = this;
        subnodes.Add(subnode);
    }

    public void RemoveNode(string value)
    {
        foreach (SimisNode subnode in subnodes.Where(c => c.Value == value))
        {
            RemoveNode(subnode);
        }
    }

    public void RemoveNode(SimisNode subnode)
    {
        if (subnodes.Remove(subnode))
        {
            subnode.Parent = null;
        }
    }

    public void RemoveAllNodes()
    {
        foreach (SimisNode subnode in subnodes)
        {
            subnode.Parent = null;
        }
        subnodes.Clear();
    }

    public bool IsMatchForPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = path.Replace('\\', '/').Trim();
        bool isAbsolutePath = path.StartsWith('/');

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        SimisNode? currentNode = this;

        for (int i = segments.Length - 1; i >= 0; i--)
        {
            if (currentNode == null)
                return false;

            if (currentNode.Value != segments[i])
                return false;

            currentNode = currentNode.Parent;
        }

        if (isAbsolutePath)
            return currentNode == null;

        return true;
    }

    public IReadOnlyList<SimisNode> FindNodesByPath(string path)
    {
        var result = new List<SimisNode>();

        if (string.IsNullOrWhiteSpace(path))
            return result;

        path = path.Replace('\\', '/').Trim();
        bool isAbsolutePath = path.StartsWith('/');

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            if (isAbsolutePath)
            {
                result.Add(GetRoot());
            }

            return result;
        }

        if (isAbsolutePath)
        {
            SimisNode root = GetRoot();

            if (root.IsDummyRoot)
            {
                foreach (SimisNode subnode in root.GetNodes())
                {
                    FindNodesByPathRecursive(subnode, segments, 0, result);
                }
            }
            else
            {
                FindNodesByPathRecursive(root, segments, 0, result);
            }
                
        }
        else
        {
            FindNodesByPathRecursive(this, segments, 0, result);
        }

        return result;
    }

    private static void FindNodesByPathRecursive(SimisNode node, string[] segments, int index, List<SimisNode> result)
    {
        if (node == null)
            return;

        if (index == segments.Length)
        {
            result.Add(node);
            return;
        }

        string segment = segments[index];

        if (segment == ".")
        {
            FindNodesByPathRecursive(node, segments, index + 1, result);
            return;
        }

        if (segment == "..")
        {
            if (node.Parent != null && !node.Parent.IsDummyRoot)
                FindNodesByPathRecursive(node.Parent, segments, index + 1, result);
            return;
        }

        ParseSegment(segment, out string? segmentName, out int? segmentIndex);

        IReadOnlyList<SimisNode> filtered;
        if (segmentName == null)
        {
            filtered = node.GetNodes();
        }
        else
        {
            filtered = node.GetNodes(segmentName);
        }

        if (segmentIndex.HasValue)
        {
            int zeroBasedIndex = segmentIndex.Value - 1;

            if (zeroBasedIndex >= 0 && zeroBasedIndex < filtered.Count)
            {
                FindNodesByPathRecursive(filtered[zeroBasedIndex], segments, index + 1, result);
            }

            return;
        }

        foreach (SimisNode child in filtered)
        {
            FindNodesByPathRecursive(child, segments, index + 1, result);
        }
    }

    private static void ParseSegment(string segment, out string? name, out int? index)
    {
        name = null;
        index = null;

        if (segment.StartsWith('[') && segment.EndsWith(']'))
        {
            // It's an index only
            if (int.TryParse(segment[1..^1], out int parsed))
                index = parsed;

            return;
        }

        int bracketStart = segment.IndexOf('[');
        if (bracketStart < 0)
        {
            // No index specified
            name = segment;
            return;
        }

        int bracketEnd = segment.IndexOf(']', bracketStart + 1);
        if (bracketEnd < 0)
        {
            // Malformed segment, treat whole as name
            name = segment;
            return;
        }

        // Extract name
        name = segment[..bracketStart];

        // Try to extract index
        if (int.TryParse(segment.AsSpan(bracketStart + 1, bracketEnd - bracketStart - 1), out int parsedIndex))
            index = parsedIndex;
    }

    public sealed class SimisNodeFormatting()
    {
        public bool IsOnSameLineAsPrevious { get; set; }

        public byte LeadingEmptyLines { get; set; }

        public byte TrailingEmptyLines { get; set; }

        public bool IsQuoted { get; set; }

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
