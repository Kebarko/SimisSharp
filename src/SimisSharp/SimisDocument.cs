namespace KE.MSTS.SimisSharp;

public class SimisDocument
{
    public string? Header { get; set; }

    public SimisNode Root { get; } = new("ROOT");
}
