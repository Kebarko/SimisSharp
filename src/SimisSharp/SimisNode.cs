using System.Diagnostics;

namespace KE.MSTS.SimisSharp;

[DebuggerDisplay("{Value}")]
public class SimisNode(string value)
{
    private readonly IList<SimisNode> subNodes = [];

    public string Value { get; } = value;

    public string QuotedValue => Value.Contains(' ') ? $"\"{Value}\"" : Value;

    public SimisNode? Parent { get; private set; }

    public int EmptyLinesBefore { get; set; }

    public int EmptyLinesAfter { get; set; }

    public bool IsLeaf => subNodes.Count == 0;

    public bool IsEmptyLeaf => IsLeaf && string.IsNullOrEmpty(Value);

    public bool HasEmptySubNodes => !IsLeaf && subNodes.All(ch => string.IsNullOrEmpty(ch.Value));

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

    public void AddNode(SimisNode child)
    {
        child.Parent = this;
        subNodes.Add(child);
    }

    public void RemoveNode(string value)
    {
        foreach (SimisNode child in subNodes.Where(c => c.Value == value))
        {
            RemoveNode(child);
        }
    }

    public void RemoveNode(SimisNode child)
    {
        if (subNodes.Remove(child))
        {
            child.Parent = null;
        }
    }

    public void RemoveAllNodes()
    {
        foreach (SimisNode child in subNodes)
        {
            child.Parent = null;
        }
        subNodes.Clear();
    }
}
