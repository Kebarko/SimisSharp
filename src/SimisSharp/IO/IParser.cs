namespace KE.MSTS.SimisSharp.IO;

public interface IParser
{
    SimisDocument Parse(IEnumerable<string> tokens);
}
