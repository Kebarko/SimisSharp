namespace KE.MSTS.SimisSharp.Tests;

public class SimisPathServiceTest
{
    private const string Text = """
        Wagon (
        	Name ( Loco )
        	Mass ( 100t )
        	Coupling ( Type ( Chain ) )
        	Coupling ( Type ( Bar ) )
        )
        Engine (
        	Mass ( 90t )
        	Effects ( Mass ( 1 ) )
        )
        """;

    private readonly SimisDocument document = new SimisProcessor().Parse(Text);

    [Theory]
    [InlineData("", "/Wagon/Mass", "Wagon/Mass")]
    [InlineData("", "Wagon/Mass", "Wagon/Mass")]
    [InlineData("", "wagon/MASS", "Wagon/Mass")]
    [InlineData("", "/Mass")]
    [InlineData("", "/*/Mass", "Wagon/Mass", "Engine/Mass")]
    [InlineData("", "/W*n/M?ss", "Wagon/Mass")]
    [InlineData("", "/*/*/Mass", "Engine/Effects/Mass")]
    [InlineData("", "/Wagon/Coupling[2]/Type", "Wagon/Coupling[2]/Type")]
    [InlineData("", "/Wagon/coupling[2]/type", "Wagon/Coupling[2]/Type")]
    [InlineData("", "/Wagon/Coupling[3]")]
    [InlineData("", "/Wagon/[2]", "Wagon/Mass")]
    [InlineData("", "/Wagon/[0]")]
    [InlineData("Wagon", "../Engine/Mass", "Engine/Mass")]
    [InlineData("Wagon", "/Engine/Mass", "Engine/Mass")]
    [InlineData("Engine/Effects/Mass", "/Wagon", "Wagon")]
    [InlineData("Wagon", ".", "Wagon")]
    [InlineData("Wagon/Mass", "../../Engine", "Engine")]
    public void FindNodesByPathInDocument(string start, string path, params string[] expected)
    {
        // Act
        IList<SimisNode> result = SimisPathService.FindNodesByPath(Node(start), path);

        // Assert
        Assert.Equal(expected.Select(Node), result);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("..")]
    public void FindNodesByPathReachesDocumentRoot(string path)
    {
        // Act
        IList<SimisNode> result = SimisPathService.FindNodesByPath(Node("Wagon"), path);

        // Assert
        Assert.Same(document.Root, Assert.Single(result));
    }

    [Fact]
    public void FindNodesByPathRespectsCase()
    {
        Assert.Empty(SimisPathService.FindNodesByPath(document.Root, "wagon/MASS", ignoreCase: false));
        Assert.Single(SimisPathService.FindNodesByPath(document.Root, "Wagon/Mass", ignoreCase: false));
    }

    [Theory]
    [InlineData("Wagon/Mass", "/Wagon/Mass", true)]
    [InlineData("Wagon/Mass", "Wagon/Mass", true)]
    [InlineData("Wagon/Mass", "Mass", true)]
    [InlineData("Wagon/Mass", "/Mass", false)]
    [InlineData("Wagon/Mass", "/wagon/MASS", true)]
    [InlineData("Wagon/Mass", "/*/Mass", true)]
    [InlineData("Wagon/Mass", "\\Wagon\\Mass", true)]
    [InlineData("Wagon/Mass", "/X/Wagon/Mass", false)]
    [InlineData("Wagon", "/Wagon", true)]
    [InlineData("Engine/Effects/Mass", "/Engine/Mass", false)]
    [InlineData("Engine/Effects/Mass", "Effects/Mass", true)]
    [InlineData("Engine/Effects/Mass", "/Engine/*/Mass", true)]
    [InlineData("", "/", true)]
    [InlineData("Wagon", "/", false)]
    [InlineData("Wagon", "", false)]
    public void IsMatchForPathInDocument(string node, string path, bool expected)
    {
        Assert.Equal(expected, SimisPathService.IsMatchForPath(Node(node), path, ignoreCase: true));
    }

    [Fact]
    public void IsMatchForPathRespectsCase()
    {
        Assert.False(SimisPathService.IsMatchForPath(Node("Wagon/Mass"), "/wagon/MASS", ignoreCase: false));
        Assert.True(SimisPathService.IsMatchForPath(Node("Wagon/Mass"), "/Wagon/Mass", ignoreCase: false));
    }

    [Theory]
    [InlineData("/Wagon/Mass")]
    [InlineData("/*/Mass")]
    [InlineData("/*/*/Mass")]
    [InlineData("/*")]
    [InlineData("/*/*")]
    [InlineData("*/Mass")]
    [InlineData("*/Type")]
    public void FindAndMatchAgree(string path)
    {
        // Act
        IList<SimisNode> found = SimisPathService.FindNodesByPath(document.Root, path);
        IEnumerable<SimisNode> matched = Descendants(document.Root).Where(n => SimisPathService.IsMatchForPath(n, path));

        // Assert
        if (path.StartsWith('/'))
            Assert.Equal(matched, found);
        else
            Assert.All(found, n => Assert.True(SimisPathService.IsMatchForPath(n, path)));
    }

    [Fact]
    public void DetachedTreeTopNodeIsRoot()
    {
        // Arrange
        SimisNode a = new("A");
        SimisNode b = new("B");
        SimisNode c = new("C");
        a.AddNode(b);
        b.AddNode(c);

        // Act & Assert
        Assert.Same(c, Assert.Single(SimisPathService.FindNodesByPath(c, "/B/C")));
        Assert.Empty(SimisPathService.FindNodesByPath(c, "/A/B/C"));
        Assert.True(SimisPathService.IsMatchForPath(c, "/B/C"));
        Assert.False(SimisPathService.IsMatchForPath(c, "/A/B/C"));
    }

    /// <summary>
    /// Resolves a test path such as "Wagon/Coupling[2]/Type" with exact, case-sensitive names. An empty path is the document root.
    /// </summary>
    private SimisNode Node(string path)
    {
        SimisNode node = document.Root;
        foreach (string segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            string name = segment.Split('[')[0];
            int index = segment.Contains('[') ? int.Parse(segment[(segment.IndexOf('[') + 1)..^1]) : 1;
            node = node.GetNodes(name)[index - 1];
        }
        return node;
    }

    private static IEnumerable<SimisNode> Descendants(SimisNode node)
    {
        foreach (SimisNode subnode in node.GetNodes())
        {
            yield return subnode;

            foreach (SimisNode descendant in Descendants(subnode))
                yield return descendant;
        }
    }
}
