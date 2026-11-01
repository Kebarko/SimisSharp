namespace KE.MSTS.SimisSharp.Tests;

public class SimisNodeEditingTest
{
    private readonly SimisProcessor processor = new();

    [Fact]
    public void SetValueRenamesBlockAndChangesLeaf()
    {
        // Arrange
        SimisDocument document = processor.Parse("Wagon ( Mass ( 10t ) Name ( Loco ) )");
        SimisNode wagon = document.Root.GetNodes()[0];

        // Act
        wagon.GetNodes()[0].Value = "ORTSMass";
        wagon.GetNodes()[0].GetNodes()[0].Value = "12.5t";
        wagon.GetNodes()[1].GetNodes()[0].Value = "My Loco";

        // Assert
        Assert.Equal(Lines("Wagon (", "\tORTSMass ( 12.5t )", "\tName ( \"My Loco\" )", ")"), processor.Save(document));
    }

    [Fact]
    public void RenamingBlockToCommentKeepsItsValues()
    {
        // Arrange
        SimisDocument document = processor.Parse("Wagon ( Size ( 1 2 3 ) )");
        SimisNode size = document.Root.GetNodes()[0].GetNodes()[0];

        // Act
        size.Value = "_Size";
        string text = processor.Save(document);
        SimisNode reparsed = processor.Parse(text).Root.GetNodes()[0].GetNodes()[0];

        // Assert
        Assert.True(size.IsComment);
        Assert.Equal(Lines("Wagon (", "\t_Size ( 1 2 3 )", ")"), text);
        Assert.True(reparsed.IsComment);
        Assert.Equal(" 1 2 3 ", reparsed.GetNodes()[0].Value);
    }

    [Fact]
    public void RenamingCommentToBlockRestoresItsValuesAfterReparse()
    {
        // Arrange
        SimisDocument document = processor.Parse("Wagon ( _Size ( 1 \"2 3\" ) )");
        SimisNode size = document.Root.GetNodes()[0].GetNodes()[0];

        // Act
        size.Value = "Size";
        string text = processor.Save(document);
        SimisNode reparsed = processor.Parse(text).Root.GetNodes()[0].GetNodes()[0];

        // Assert
        Assert.False(size.IsComment);
        Assert.Equal(Lines("Wagon (", "\tSize ( 1 \"2 3\" )", ")"), text);
        Assert.Equal(["1", "2 3"], reparsed.GetNodes().Select(n => n.Value));
    }

    [Fact]
    public void EditingCommentContentKeepsItVerbatim()
    {
        // Arrange
        SimisDocument document = processor.Parse("comment ( old note )");
        SimisNode content = document.Root.GetNodes()[0].GetNodes()[0];

        // Act
        content.Value = " new  note ";

        // Assert
        Assert.True(content.Formatting.IsVerbatim);
        Assert.Equal(Lines("comment ( new  note )"), processor.Save(document));
    }

    [Fact]
    public void SetValueRejectsNull()
    {
        // Arrange
        SimisNode node = new("Mass");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => node.Value = null!);
        Assert.Equal("Mass", node.Value);
    }

    [Fact]
    public void AddNodeReturnsAddedNodeForBuilding()
    {
        // Arrange
        SimisDocument document = new();

        // Act
        SimisNode wagon = document.Root.AddNode("Wagon");
        wagon.AddNode("Mass").AddNode("10t");
        SimisNode size = wagon.AddNode(new SimisNode("Size"));
        size.AddNode("1");
        size.AddNode("2");

        // Assert
        Assert.Equal(Lines("Wagon (", "\tMass ( 10t )", "\tSize ( 1 2 )", ")"), processor.Save(document));
    }

    [Theory]
    [InlineData(0, new[] { "X", "A", "B", "C" })]
    [InlineData(2, new[] { "A", "B", "X", "C" })]
    [InlineData(3, new[] { "A", "B", "C", "X" })]
    public void InsertNodeAtIndex(int index, string[] expected)
    {
        // Arrange
        SimisNode parent = Parent("A", "B", "C");

        // Act
        SimisNode inserted = parent.InsertNode(index, "X");

        // Assert
        Assert.Equal(expected, Values(parent));
        Assert.Same(parent, inserted.Parent);
        Assert.Equal(index, parent.IndexOf(inserted));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void InsertNodeRejectsIndexOutOfRange(int index)
    {
        // Arrange
        SimisNode parent = Parent("A", "B", "C");
        SimisNode node = new("X");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => parent.InsertNode(index, node));
        Assert.Equal(["A", "B", "C"], Values(parent));
        Assert.Null(node.Parent);
    }

    [Theory]
    [InlineData("C", 0, new[] { "C", "A", "B" })]
    [InlineData("A", 2, new[] { "B", "C", "A" })]
    [InlineData("B", 1, new[] { "A", "B", "C" })]
    public void InsertNodeMovesNodeWithinParent(string value, int index, string[] expected)
    {
        // Arrange
        SimisNode parent = Parent("A", "B", "C");
        SimisNode node = parent.GetNode(value)!;

        // Act
        parent.InsertNode(index, node);

        // Assert
        Assert.Equal(expected, Values(parent));
        Assert.Same(parent, node.Parent);
    }

    [Fact]
    public void InsertNodeWithinParentRejectsIndexPastEnd()
    {
        // Arrange
        SimisNode parent = Parent("A", "B", "C");

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => parent.InsertNode(3, parent.GetNode("A")!));
        Assert.Equal(["A", "B", "C"], Values(parent));
    }

    [Fact]
    public void AddAndInsertMoveNodeFromOtherParent()
    {
        // Arrange
        SimisNode source = Parent("A", "B");
        SimisNode target = Parent("X");
        SimisNode a = source.GetNode("A")!;
        SimisNode b = source.GetNode("B")!;

        // Act
        target.AddNode(a);
        target.InsertNode(0, b);

        // Assert
        Assert.True(Assert.Single(source.Nodes).IsDummyLeaf);
        Assert.Equal(["B", "X", "A"], Values(target));
        Assert.Same(target, a.Parent);
        Assert.Same(target, b.Parent);
    }

    [Fact]
    public void EmptiedBlocksStayBlocks()
    {
        // Arrange
        SimisDocument document = processor.Parse("Wagon ( Lights ( 1 ) Sound ( a b ) Coupling ( Chain ) Brake ( x ) )");
        SimisNode wagon = document.Root.Nodes[0];
        SimisNode other = new("Other");

        // Act
        wagon.GetNode("Lights")!.RemoveAllNodes();
        wagon.GetNode("Sound")!.RemoveNode("a");
        wagon.GetNode("Sound")!.RemoveNode("b");
        SimisNode coupling = wagon.GetNode("Coupling")!;
        coupling.RemoveNode(coupling.Nodes[0]);
        other.AddNode(wagon.GetNode("Brake")!.Nodes[0]);

        // Assert
        Assert.Equal(Lines("Wagon (", "\tLights ( )", "\tSound ( )", "\tCoupling ( )", "\tBrake ( )", ")"), processor.Save(document));
        Assert.All(wagon.Nodes, n => Assert.True(Assert.Single(n.Nodes).IsDummyLeaf));
    }

    [Fact]
    public void AddingToEmptyBlockReplacesPlaceholder()
    {
        // Arrange
        SimisDocument document = processor.Parse("Wagon ( Lights ( 1 ) Empty ( ) )");
        SimisNode lights = document.Root.Nodes[0].GetNode("Lights")!;
        SimisNode empty = document.Root.Nodes[0].GetNode("Empty")!;
        SimisNode placeholder = empty.Nodes[0];

        // Act
        lights.RemoveAllNodes();
        lights.AddNode("2");
        empty.InsertNode(0, "x");

        // Assert
        Assert.Equal(["2"], Values(lights));
        Assert.Equal(["x"], Values(empty));
        Assert.Null(placeholder.Parent);
        Assert.Equal(Lines("Wagon (", "\tLights ( 2 )", "\tEmpty ( x )", ")"), processor.Save(document));
    }

    [Fact]
    public void InsertingIntoEmptyBlockAcceptsOnlyFirstIndex()
    {
        // Arrange
        SimisNode block = processor.Parse("Empty ( )").Root.Nodes[0];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => block.InsertNode(1, "x"));
        Assert.True(Assert.Single(block.Nodes).IsDummyLeaf);
    }

    [Fact]
    public void RemovingAllNodesOfLeafKeepsLeaf()
    {
        // Arrange
        SimisNode leaf = new("Lights");

        // Act
        leaf.RemoveAllNodes();

        // Assert
        Assert.True(leaf.IsLeaf);
    }

    [Fact]
    public void MovingOnlyChildWithinBlockKeepsIt()
    {
        // Arrange
        SimisNode block = Parent("A");
        SimisNode a = block.Nodes[0];

        // Act
        block.AddNode(a);
        block.InsertNode(0, a);

        // Assert
        Assert.Same(a, Assert.Single(block.Nodes));
    }

    [Fact]
    public void ReplaceNodeKeepsPosition()
    {
        // Arrange
        SimisNode parent = Parent("A", "B", "C");
        SimisNode b = parent.GetNode("B")!;
        SimisNode x = new("X");

        // Act
        SimisNode result = parent.ReplaceNode(b, x);

        // Assert
        Assert.Same(x, result);
        Assert.Equal(["A", "X", "C"], Values(parent));
        Assert.Null(b.Parent);
        Assert.Same(parent, x.Parent);
    }

    [Fact]
    public void ReplaceNodeWithSiblingMovesSibling()
    {
        // Arrange
        SimisNode parent = Parent("A", "B", "C");

        // Act
        parent.ReplaceNode(parent.GetNode("A")!, parent.GetNode("C")!);

        // Assert
        Assert.Equal(["C", "B"], Values(parent));
    }

    [Fact]
    public void ReplaceNodeRejectsNodeOfOtherParent()
    {
        // Arrange
        SimisNode parent = Parent("A");
        SimisNode other = Parent("B");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => parent.ReplaceNode(other.GetNode("B")!, new SimisNode("X")));
        Assert.Equal(["A"], Values(parent));
        Assert.Equal(["B"], Values(other));
    }

    [Fact]
    public void AddingNodeToItselfOrDescendantThrows()
    {
        // Arrange
        SimisNode root = new("Root");
        SimisNode child = root.AddNode("Child");
        SimisNode grandchild = child.AddNode("Grandchild");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => root.AddNode(root));
        Assert.Throws<InvalidOperationException>(() => grandchild.AddNode(root));
        Assert.Throws<InvalidOperationException>(() => grandchild.InsertNode(0, child));
        Assert.Throws<InvalidOperationException>(() => child.ReplaceNode(grandchild, root));
        Assert.Same(root, child.Parent);
        Assert.Same(child, grandchild.Parent);
        Assert.Equal(["Grandchild"], Values(child));
    }

    [Fact]
    public void NodesIsReadOnlyLiveView()
    {
        // Arrange
        SimisNode parent = Parent("A");
        IReadOnlyList<SimisNode> nodes = parent.Nodes;

        // Act
        parent.AddNode("B");

        // Assert
        Assert.Equal(["A", "B"], nodes.Select(n => n.Value));
        Assert.Throws<NotSupportedException>(() => ((IList<SimisNode>)nodes).Add(new SimisNode("C")));
    }

    [Fact]
    public void InsertedParameterIsWrittenOnItsOwnLine()
    {
        // Arrange
        SimisDocument document = processor.Parse(Lines("Engine (", "\tMaxPower ( 800kW )", "\tMaxForce ( 100kN )", ")"));
        SimisNode engine = document.Root.Nodes[0];
        SimisNode power = new("ORTSMaxPower");
        power.AddNode("1000kW");

        // Act
        engine.InsertNode(engine.IndexOf(engine.GetNode("MaxPower")!) + 1, power);

        // Assert
        Assert.Equal(Lines("Engine (", "\tMaxPower ( 800kW )", "\tORTSMaxPower ( 1000kW )", "\tMaxForce ( 100kN )", ")"), processor.Save(document));
    }

    private static SimisNode Parent(params string[] values)
    {
        SimisNode parent = new("Parent");
        foreach (string value in values)
            parent.AddNode(value);
        return parent;
    }

    private static IEnumerable<string?> Values(SimisNode node)
    {
        return node.Nodes.Select(n => n.Value);
    }

    private static string Lines(params string[] lines)
    {
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
