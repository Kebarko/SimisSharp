namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Writes a <see cref="SimisDocument"/> to a string representation using provided <see cref="WriterOptions"/>.
/// </summary>
public interface IWriter
{
    /// <summary>
    /// Produces a textual representation of <paramref name="document"/> using the provided <paramref name="options"/>.
    /// </summary>
    /// <param name="document">The document to write. Must not be null.</param>
    /// <param name="options">Optional writer options. If null, defaults are used.</param>
    /// <returns>Textual representation of the document.</returns>
    string Write(SimisDocument document, WriterOptions? options = null);
}
