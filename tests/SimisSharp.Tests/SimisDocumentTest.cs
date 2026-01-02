using KE.MSTS.SimisSharp.IO;

namespace KE.MSTS.SimisSharp.Tests;

public class SimisDocumentTest
{
    [Theory]
    [ClassData(typeof(SimisDocumentTestData))]
    public void ParseAndSaveTest(string input, string expected, WriterOptions? options = null)
    {
        // Arrange
        SimisProcessor processor = new();

        // Act
        SimisDocument doc = processor.Parse(input);
        string actual = processor.Save(doc, options);

        // Assert
        Assert.Equal(expected, actual);
    }
}
