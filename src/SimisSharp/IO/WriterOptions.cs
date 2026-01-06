namespace KE.MSTS.SimisSharp.IO;

public class WriterOptions
{
    public int IndentSize { get; set; } = 1;

    public char IndentCharacter { get; set; } = '\t';

    public bool PreserveQuotes { get; set; } = true;

    public bool PreserveComments { get; set; } = true;

    public bool PreserveEmptyLines { get; set; } = true;

    public bool CollapseEmptyLines { get; set; } = true;

    public Dictionary<string, int> ValuesInColumns { get; set; } = new();
}
