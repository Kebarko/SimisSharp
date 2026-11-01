namespace KE.MSTS.SimisSharp;

/// <summary>
/// Root container for a Simis document. Holds the root <see cref="SimisNode"/> from which the tree is traversed.
/// </summary>
public class SimisDocument
{
    /// <summary>
    /// Dummy root node of the document. This node has no value and contains top-level nodes as children.
    /// In a parsed file the first child is usually the SIMIS header, such as <c>SIMISA@@@@@@@@@@JINX0D0t______</c>.
    /// </summary>
    public SimisNode Root { get; } = new();
}
