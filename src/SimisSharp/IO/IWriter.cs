namespace KE.MSTS.SimisSharp.IO;

public interface IWriter
{
    string Write(SimisDocument document, SimisWriterOptions? options = null);
}
