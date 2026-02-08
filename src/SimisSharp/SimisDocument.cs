namespace KE.MSTS.SimisSharp;

/// <summary>
/// Root container for a Simis document. Holds the root <see cref="SimisNode"/> from which the tree is traversed.
/// </summary>
public class SimisDocument
{
    /// <summary>
    /// Dummy root node of the document. This node has no value and contains top-level nodes as children.
    /// </summary>
    public SimisNode Root { get; } = new();
}
