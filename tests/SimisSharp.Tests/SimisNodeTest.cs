namespace KE.MSTS.SimisSharp.Tests;

public class SimisNodeTest
{
    [Theory]
    [ClassData(typeof(SimisNodeTestData))]
    public void FindNodesByPathTest(SimisNode startNode, string path, IEnumerable<SimisNode> expected)
    {
        // Arrange
        SimisNode node = startNode;

        // Act
        IList<SimisNode> result = SimisPathService.FindNodesByPath(node, path);

        // Assert
        IList<SimisNode> expectedList = [.. expected];
        Assert.Equal(expectedList.Count, result.Count);
        Assert.Equal(expectedList, result);
    }

    [Theory]
    [InlineData("B", StringComparison.Ordinal, new[] { "A", "C", "b" })]
    [InlineData("b", StringComparison.OrdinalIgnoreCase, new[] { "A", "C" })]
    [InlineData("X", StringComparison.Ordinal, new[] { "A", "B", "C", "B", "b" })]
    public void RemoveNodeByValueTest(string value, StringComparison comparisonType, string[] expected)
    {
        // Arrange
        SimisNode parent = new("Parent");
        SimisNode[] children = [new("A"), new("B"), new("C"), new("B"), new("b")];
        foreach (SimisNode child in children)
            parent.AddNode(child);

        // Act
        parent.RemoveNode(value, comparisonType);

        // Assert
        Assert.Equal(expected, parent.GetNodes().Select(n => n.Value));
        Assert.All(children, c => Assert.Equal(expected.Contains(c.Value) ? parent : null, c.Parent));
    }

    [Fact]
    public void LookupsIgnoreCaseByDefault()
    {
        // Arrange
        SimisNode wagon = new SimisProcessor().Parse("Wagon ( mass ( 10t ) MASS ( 12t ) Lights ( 1 ) )").Root.Nodes[0];

        // Act
        SimisNode? first = wagon.GetNode("Mass");
        IList<SimisNode> all = wagon.GetNodes("Mass");
        wagon.RemoveNode("mASS");

        // Assert
        Assert.Equal("mass", first?.Value);
        Assert.Equal(["mass", "MASS"], all.Select(n => n.Value));
        Assert.Equal(["Lights"], wagon.Nodes.Select(n => n.Value));
    }

    [Fact]
    public void LookupsRespectGivenComparison()
    {
        // Arrange
        SimisNode wagon = new SimisProcessor().Parse("Wagon ( mass ( 10t ) Mass ( 12t ) )").Root.Nodes[0];

        // Act
        SimisNode? exact = wagon.GetNode("Mass", StringComparison.Ordinal);
        int count = wagon.GetNodes("Mass", StringComparison.Ordinal).Count;
        wagon.RemoveNode("Mass", StringComparison.Ordinal);

        // Assert
        Assert.Equal("Mass", exact?.Value);
        Assert.Equal(1, count);
        Assert.Equal(["mass"], wagon.Nodes.Select(n => n.Value));
    }

    [Fact]
    public void FormattingIsAViewOfTheNode()
    {
        // Arrange
        SimisNode value = new SimisProcessor().Parse("A ( \"b\" )").Root.Nodes[0].Nodes[0];
        SimisNode.SimisNodeFormatting formatting = value.Formatting;

        // Act
        formatting.IsQuoted = false;
        formatting.LeadingEmptyLines = 2;

        // Assert
        Assert.False(value.Formatting.IsQuoted);
        Assert.Equal(2, value.Formatting.LeadingEmptyLines);
    }

    [Fact]
    public void NodesOfLeafShowsLaterChildren()
    {
        // Arrange
        SimisNode node = new("A");
        IReadOnlyList<SimisNode> nodes = node.Nodes;
        bool wasLeaf = node.IsLeaf;

        // Act
        node.AddNode("1");

        // Assert
        Assert.True(wasLeaf);
        Assert.False(node.IsLeaf);
        Assert.Equal(["1"], nodes.Select(n => n.Value));
    }
}
