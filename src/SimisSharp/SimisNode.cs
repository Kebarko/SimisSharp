using System.Diagnostics;

namespace KE.MSTS.SimisSharp;

[DebuggerDisplay("{Value}")]
public class SimisNode(string value)
{
    private readonly IList<SimisNode> subNodes = [];

    public string Value { get; } = value;

    public SimisNode? Parent { get; private set; }

    public SimisNodeFormatting Formatting { get; } = new SimisNodeFormatting();

    public bool IsLeaf => subNodes.Count == 0;

    public bool IsEmptyLeaf => IsLeaf && string.IsNullOrEmpty(Value);

    public bool HasEmptyLeafs => !IsLeaf && subNodes.All(subNode => subNode.IsEmptyLeaf);

    public bool IsComment => string.Equals("comment", Value, StringComparison.OrdinalIgnoreCase) || string.Equals("skip", Value, StringComparison.OrdinalIgnoreCase);

    public IList<SimisNode> GetNodes()
    {
        return subNodes.ToList();
    }

    public IList<SimisNode> GetNodes(Func<SimisNode, bool> predicate)
    {
        return subNodes.Where(predicate).ToList();
    }

    public IList<SimisNode> GetNodes(string value)
    {
        return subNodes.Where(c => c.Value == value).ToList();
    }

    public SimisNode? GetNode(string value)
    {
        return subNodes.FirstOrDefault(c => c.Value == value);
    }

    public void AddNode(string value)
    {
        AddNode(new SimisNode(value));
    }

    public void AddNode(SimisNode subNode)
    {
        subNode.Parent = this;
        subNodes.Add(subNode);
    }

    public void RemoveNode(string value)
    {
        foreach (SimisNode subNode in subNodes.Where(c => c.Value == value))
        {
            RemoveNode(subNode);
        }
    }

    public void RemoveNode(SimisNode subNode)
    {
        if (subNodes.Remove(subNode))
        {
            subNode.Parent = null;
        }
    }

    public void RemoveAllNodes()
    {
        foreach (SimisNode subNode in subNodes)
        {
            subNode.Parent = null;
        }
        subNodes.Clear();
    }

    public sealed class SimisNodeFormatting()
    {
        public byte LeadingEmptyLines { get; set; }

        public byte TrailingEmptyLines { get; set; }

        public string GetQuotedValue(string value) => value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
