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
}
