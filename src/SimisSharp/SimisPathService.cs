namespace KE.MSTS.SimisSharp;

/// <summary>
/// Utility service for matching and locating nodes by a path-like expression.
/// </summary>
/// <remarks>
/// <para>Paths separate segments with '/' or '\'. A name segment matches a node value. It may contain the wildcards '*' and '?'
/// (see <see cref="WildcardMatcher"/>), and it ignores letter case by default, as MSTS and Open Rails do.</para>
/// <para>The topmost ancestor of a node is the root of its paths. In a parsed document this is <see cref="SimisDocument.Root"/>,
/// so the first segment of an absolute path such as <c>/Wagon/Mass</c> names a top-level node.</para>
/// </remarks>
public static class SimisPathService
{
    /// <summary>
    /// Determines whether the specified <paramref name="node"/> matches the given <paramref name="path"/> pattern.
    /// The last segment must match the node and each preceding segment must match the next ancestor.
    /// A relative path matches at any depth, while an absolute path must lead from the root to the node.
    /// Only name segments are supported. The segments ".", ".." and indexes such as "[2]" are matched literally.
    /// </summary>
    /// <param name="node">Node to test against the path (typically the current node).</param>
    /// <param name="path">Path pattern. May be absolute (starting with '/') or relative.</param>
    /// <param name="ignoreCase">Whether to ignore case when matching node values to path segments.</param>
    /// <returns><c>true</c> when the node's ancestry matches the path segments; otherwise <c>false</c>.</returns>
    public static bool IsMatchForPath(SimisNode node, string path, bool ignoreCase = true)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string[] segments = SplitPath(path, out bool isAbsolutePath);
        SimisNode? currentNode = node;

        for (int i = segments.Length - 1; i >= 0; i--)
        {
            if (currentNode?.Value == null)
                return false;

            if (!WildcardMatcher.IsMatch(currentNode.Value, segments[i], ignoreCase))
                return false;

            currentNode = currentNode.Parent;
        }

        return !isAbsolutePath || currentNode is { Parent: null };
    }

    /// <summary>
    /// Finds nodes matching the supplied <paramref name="path"/> starting from the supplied <paramref name="node"/>.
    /// </summary>
    /// <param name="node">Starting node used to resolve relative paths. Absolute paths start at its root.</param>
    /// <param name="path">Path expression. Can be absolute (leading '/') or relative. Supports the special segments "." and "..",
    /// and a 1-based index that selects the n-th matching child, such as "Coupling[2]", or the n-th child, such as "[2]".</param>
    /// <param name="ignoreCase">Whether to ignore case when matching node values to path segments.</param>
    /// <returns>List of matching nodes in document order. Empty list when no matches are found or the path is empty/invalid.</returns>
    public static IList<SimisNode> FindNodesByPath(SimisNode node, string path, bool ignoreCase = true)
    {
        List<SimisNode> result = [];

        if (string.IsNullOrWhiteSpace(path))
            return result;

        string[] segments = SplitPath(path, out bool isAbsolutePath);
        SimisNode start = isAbsolutePath ? GetRoot(node) : node;

        FindNodesByPathRecursive(start, segments, 0, ignoreCase, result);

        return result;
    }

    /// <summary>
    /// Normalizes separators and splits <paramref name="path"/> into its segments.
    /// </summary>
    /// <param name="path">Path expression.</param>
    /// <param name="isAbsolutePath">True when the path starts with a separator.</param>
    /// <returns>Non-empty path segments.</returns>
    private static string[] SplitPath(string path, out bool isAbsolutePath)
    {
        path = path.Replace('\\', '/').Trim();
        isAbsolutePath = path.StartsWith('/');

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries);
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
    /// <param name="ignoreCase">Whether to ignore case when matching node values to path segments.</param>
    /// <param name="result">Accumulator for matched nodes.</param>
    private static void FindNodesByPathRecursive(SimisNode node, string[] segments, int index, bool ignoreCase, List<SimisNode> result)
    {
        if (index == segments.Length)
        {
            result.Add(node);
            return;
        }

        string segment = segments[index];

        if (segment == ".")
        {
            FindNodesByPathRecursive(node, segments, index + 1, ignoreCase, result);
            return;
        }

        if (segment == "..")
        {
            if (node.Parent != null)
                FindNodesByPathRecursive(node.Parent, segments, index + 1, ignoreCase, result);
            return;
        }

        ParseSegment(segment, out string? segmentName, out int? segmentIndex);

        if (segmentIndex.HasValue)
        {
            int remaining = segmentIndex.Value;
            if (remaining < 1)
                return;

            foreach (SimisNode child in node.GetNodes())
            {
                if (IsMatchForSegment(child, segmentName, ignoreCase) && --remaining == 0)
                {
                    FindNodesByPathRecursive(child, segments, index + 1, ignoreCase, result);
                    return;
                }
            }

            return;
        }

        foreach (SimisNode child in node.GetNodes())
        {
            if (IsMatchForSegment(child, segmentName, ignoreCase))
                FindNodesByPathRecursive(child, segments, index + 1, ignoreCase, result);
        }
    }

    /// <summary>
    /// Returns true when <paramref name="node"/> matches the segment name. A segment without a name matches any node.
    /// </summary>
    /// <param name="node">The node to test.</param>
    /// <param name="segmentName">The name part of the segment, which may contain wildcards, or null for an index-only segment.</param>
    /// <param name="ignoreCase">Whether to ignore case.</param>
    /// <returns>True when the node matches.</returns>
    private static bool IsMatchForSegment(SimisNode node, string? segmentName, bool ignoreCase)
    {
        if (segmentName == null)
            return true;

        return node.Value != null && WildcardMatcher.IsMatch(node.Value, segmentName, ignoreCase);
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
