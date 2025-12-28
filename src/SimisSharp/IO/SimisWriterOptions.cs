namespace KE.MSTS.SimisSharp.IO;

public class SimisWriterOptions
{
    public static SimisWriterOptions Default => new()
    {
        IndentSize = 2,
        IndentCharacter = ' ',
        PreserveComments = true,
        PreserveQuotes = true,
        PreserveBlankLines = true,
    };

    public int IndentSize { get; set; }

    public char IndentCharacter { get; set; }

    public bool PreserveComments { get; set; }

    public bool PreserveQuotes { get; set; }

    public bool PreserveBlankLines { get; set; }
}
