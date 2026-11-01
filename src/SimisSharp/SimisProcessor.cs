using KE.MSTS.SimisSharp.IO;
using System.Text;

namespace KE.MSTS.SimisSharp;

/// <summary>
/// High-level facade that loads, parses and saves Simis documents.
/// It holds no state, so one instance can be shared, also by several threads.
/// </summary>
public class SimisProcessor
{
    /// <summary>
    /// Serialize the document into a string using the provided writer <paramref name="options"/>.
    /// </summary>
    /// <param name="document">Document to save.</param>
    /// <param name="options">Optional writer options.</param>
    /// <returns>Textual representation of the document.</returns>
    public string Save(SimisDocument document, WriterOptions? options = null)
    {
        return Writer.Write(document, options);
    }

    /// <summary>
    /// Serialize the document and write it to <paramref name="filePath"/> as UTF-16LE with a byte order mark, the encoding MSTS reads.
    /// The text is written to a temporary file in the same folder first, which then replaces the target in one step,
    /// so a failure while writing leaves an existing file unchanged. Replacing an existing file keeps its attributes.
    /// </summary>
    /// <param name="document">Document to save.</param>
    /// <param name="filePath">Target file path to create/overwrite.</param>
    /// <param name="options">Optional writer options.</param>
    public void Save(SimisDocument document, string filePath, WriterOptions? options = null)
    {
        string content = Save(document, options);

        string fullPath = Path.GetFullPath(filePath);
        string tempPath = Path.Combine(Path.GetDirectoryName(fullPath) ?? string.Empty, $"{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (FileStream stream = new(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (StreamWriter streamWriter = new(stream, Encoding.Unicode))
            {
                streamWriter.Write(content);
                streamWriter.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(fullPath))
                File.Replace(tempPath, fullPath, destinationBackupFileName: null);
            else
                File.Move(tempPath, fullPath);
        }
        finally
        {
            DeleteQuietly(tempPath);
        }
    }

    /// <summary>
    /// Deletes a leftover temporary file, ignoring errors so that the original exception is not hidden.
    /// </summary>
    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Parse a SIMIS formatted string into a <see cref="SimisDocument"/>.
    /// </summary>
    /// <param name="content">Text content to parse.</param>
    /// <returns>Parsed document AST.</returns>
    /// <exception cref="SimisParseException">When the text is not well-formed: an unterminated quoted string or comment block,
    /// unbalanced parentheses, or a block without a name. <see cref="SimisParseException.Line"/> tells where.</exception>
    public SimisDocument Parse(string content)
    {
        return Parser.Parse(content);
    }

    /// <summary>
    /// Load and parse a text document from disk, detecting its encoding.
    /// A byte order mark selects UTF-8, UTF-16 or UTF-32. A file without one is read as UTF-16 when its first or second byte is zero,
    /// and as UTF-8 otherwise, which also covers ASCII.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <returns>Parsed document AST.</returns>
    /// <exception cref="NotSupportedException">When the file is a binary or compressed SIMIS file.</exception>
    /// <exception cref="FormatException">When the file is not valid text in the detected encoding.</exception>
    /// <exception cref="SimisParseException">When the text is not well-formed (see <see cref="Parse(string)"/>).</exception>
    public SimisDocument Load(string filePath)
    {
        return Load(filePath, null);
    }

    /// <summary>
    /// Load and parse a text document from disk, detecting its encoding as <see cref="Load(string)"/> does,
    /// but reading a file that has no byte order mark and is not UTF-16 with <paramref name="encoding"/>.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <param name="encoding">Encoding of a file without a byte order mark that is not UTF-16, such as an ANSI file. Null means UTF-8.
    /// ANSI code pages such as Windows-1250 require registering <c>System.Text.CodePagesEncodingProvider</c> first.</param>
    /// <returns>Parsed document AST.</returns>
    /// <exception cref="NotSupportedException">When the file is a binary or compressed SIMIS file.</exception>
    /// <exception cref="FormatException">When the file is not valid text in the detected encoding.</exception>
    /// <exception cref="SimisParseException">When the text is not well-formed (see <see cref="Parse(string)"/>).</exception>
    public SimisDocument Load(string filePath, Encoding? encoding)
    {
        return Parse(LoadText(filePath, encoding));
    }

    /// <summary>
    /// Load the text of a file from disk, detecting its encoding as <see cref="Load(string)"/> does, without parsing it.
    /// Use it with a <see cref="SimisReader"/> to read a file without building a tree.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <returns>The text of the file without the byte order mark.</returns>
    /// <exception cref="NotSupportedException">When the file is a binary or compressed SIMIS file.</exception>
    /// <exception cref="FormatException">When the file is not valid text in the detected encoding.</exception>
    public string LoadText(string filePath)
    {
        return LoadText(filePath, null);
    }

    /// <summary>
    /// Load the text of a file from disk, detecting its encoding as <see cref="Load(string, Encoding?)"/> does, without parsing it.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <param name="encoding">Encoding of a file without a byte order mark that is not UTF-16, such as an ANSI file. Null means UTF-8.</param>
    /// <returns>The text of the file without the byte order mark.</returns>
    /// <exception cref="NotSupportedException">When the file is a binary or compressed SIMIS file.</exception>
    /// <exception cref="FormatException">When the file is not valid text in the detected encoding.</exception>
    public string LoadText(string filePath, Encoding? encoding)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        return SimisTextDecoder.Decode(bytes, encoding);
    }
}
