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

    private static string Lines(params string[] lines)
    {
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}
