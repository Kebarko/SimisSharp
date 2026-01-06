using System.Diagnostics;

namespace KE.MSTS.SimisSharp;

[DebuggerDisplay("Value='{Value}' Parent='{Parent?.Value}' Subnodes='{subnodes.Count}'")]
public class SimisNode(string value)
{
    private readonly IList<SimisNode> subnodes = [];

    public string Value { get; } = value;

    public SimisNode? Parent { get; private set; }

    public SimisNodeFormatting Formatting { get; } = new SimisNodeFormatting();

    public bool IsLeaf => subnodes.Count == 0;

    public bool IsEmptyLeaf => IsLeaf && string.IsNullOrEmpty(Value);

    public bool IsComment => string.Equals("comment", Value, StringComparison.OrdinalIgnoreCase) || string.Equals("skip", Value, StringComparison.OrdinalIgnoreCase);

    public IList<SimisNode> GetNodes()
    {
        return subnodes.ToList();
    }

    public IList<SimisNode> GetNodes(Func<SimisNode, bool> predicate)
    {
        return subnodes.Where(predicate).ToList();
    }

    public IList<SimisNode> GetNodes(string value)
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

    public sealed class SimisNodeFormatting()
    {
        public byte LeadingEmptyLines { get; set; }

        public byte TrailingEmptyLines { get; set; }

        public bool IsQuoted { get; set; }

        public string GetQuotedValue(string value, bool preserveQoutes) => preserveQoutes && IsQuoted || value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
