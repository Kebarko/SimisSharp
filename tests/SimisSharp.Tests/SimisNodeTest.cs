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
}
