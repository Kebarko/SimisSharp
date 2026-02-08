namespace KE.MSTS.SimisSharp;

/// <summary>
/// Utility service for matching and locating nodes by a path-like expression.
/// </summary>
public static class SimisPathService
{
    /// <summary>
    /// Determines whether the specified <paramref name="node"/> matches the given <paramref name="path"/>.
    /// </summary>
    /// <param name="node">Node to test against the path (typically the current node).</param>
    /// <param name="path">Path expression. May be absolute (starting with '/') or relative. Uses '/' separators.</param>
    /// <returns><c>true</c> when the node's ancestry matches the path segments; otherwise <c>false</c>.</returns>
    public static bool IsMatchForPath(SimisNode node, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        path = path.Replace('\\', '/').Trim();
        bool isAbsolutePath = path.StartsWith('/');

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        SimisNode? currentNode = node;

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

    /// <summary>
    /// Finds nodes matching the supplied <paramref name="path"/> starting from the supplied <paramref name="node"/>.
    /// </summary>
    /// <param name="node">Starting node used to resolve relative paths; for absolute paths the document root is used.</param>
    /// <param name="path">Path expression. Can be absolute (leading '/') or relative; supports indices and special segments "." and "..".</param>
    /// <returns>List of matching nodes. Empty list when no matches are found or the path is empty/invalid.</returns>
    public static IList<SimisNode> FindNodesByPath(SimisNode node, string path)
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
                result.Add(GetRoot(node));
            }

            return result;
        }

        if (isAbsolutePath)
        {
            SimisNode root = GetRoot(node);

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
            FindNodesByPathRecursive(node, segments, 0, result);
        }

        return result;
    }

    /// <summary>
    /// Walks up the parent chain to locate the root node for the given <paramref name="node"/>.
    /// </summary>
    /// <param name="node">Node from which to discover the document root.</param>
    /// <returns>The root <see cref="SimisNode"/> (the highest ancestor with no parent).</returns>
    private static SimisNode GetRoot(SimisNode node)
    {
        while (node.Parent != null)
        {
            node = node.Parent;
        }
        return node;
    }

    /// <summary>
    /// Recursive helper that resolves path segments and accumulates results.
    /// </summary>
    /// <param name="node">Current node under consideration.</param>
    /// <param name="segments">Array of path segments to match.</param>
    /// <param name="index">Current index within <paramref name="segments"/>.</param>
    /// <param name="result">Accumulator for matched nodes.</param>
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

        IList<SimisNode> filtered;
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

    /// <summary>
    /// Parses a single path segment extracting an optional name and optional 1-based index.
    /// </summary>
    /// <param name="segment">Segment to parse (examples: "name", "name[2]", "[3]").</param>
    /// <param name="name">Output name portion, or <c>null</c> when the segment is an index-only segment.</param>
    /// <param name="index">Output 1-based index when present; otherwise <c>null</c>.</param>
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
}
