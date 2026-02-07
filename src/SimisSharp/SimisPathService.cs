namespace KE.MSTS.SimisSharp;

public static class SimisPathService
{
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

    private static SimisNode GetRoot(SimisNode node)
    {
        while (node.Parent != null)
        {
            node = node.Parent;
        }
        return node;
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
