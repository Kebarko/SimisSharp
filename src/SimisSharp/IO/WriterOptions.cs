namespace KE.MSTS.SimisSharp.IO;

public class WriterOptions
{
    public int IndentSize { get; set; } = 1;

    public char IndentCharacter { get; set; } = '\t';

    public bool PreserveQuotes { get; set; } = true;

    public bool PreserveComments { get; set; } = true;

    public bool PreserveEmptyLines { get; set; } = true;

    public bool CollapseEmptyLines { get; set; } = true;

    public List<string> Inline { get; set; } = [];

    public List<TableConfig> Table { get; set; } = [];

    public class TableConfig
    {
        public required string Name { get; set; }

        public string Columns { get; set; } = "0";

        public string ColumnSeparator { get; set; } = " ";

        public bool FixedWidth { get; set; } = false;
    }
}
