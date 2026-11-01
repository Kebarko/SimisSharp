using System.Text;

namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Decodes the bytes of a text SIMIS file. MSTS writes UTF-16LE with a byte order mark, while Open Rails also reads UTF-8 and ASCII files.
/// </summary>
internal static class SimisTextDecoder
{
    /// <summary>
    /// Strict UTF-8 decoder, which throws on invalid bytes instead of replacing them, so that text in another encoding is reported rather than misread.
    /// </summary>
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Strict UTF-16 little endian decoder.
    /// </summary>
    private static readonly Encoding Utf16LittleEndian = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Strict UTF-16 big endian decoder.
    /// </summary>
    private static readonly Encoding Utf16BigEndian = new UnicodeEncoding(bigEndian: true, byteOrderMark: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Strict UTF-32 little endian decoder.
    /// </summary>
    private static readonly Encoding Utf32LittleEndian = new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);

    /// <summary>
    /// Strict UTF-32 big endian decoder.
    /// </summary>
    private static readonly Encoding Utf32BigEndian = new UTF32Encoding(bigEndian: true, byteOrderMark: false, throwOnInvalidCharacters: true);

    /// <summary>
    /// The UTF-8 byte order mark.
    /// </summary>
    private static ReadOnlySpan<byte> Utf8Bom => [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// The UTF-16 little endian byte order mark.
    /// </summary>
    private static ReadOnlySpan<byte> Utf16LittleEndianBom => [0xFF, 0xFE];

    /// <summary>
    /// The UTF-16 big endian byte order mark.
    /// </summary>
    private static ReadOnlySpan<byte> Utf16BigEndianBom => [0xFE, 0xFF];

    /// <summary>
    /// The UTF-32 little endian byte order mark, which starts like the UTF-16 one, so it is checked first.
    /// </summary>
    private static ReadOnlySpan<byte> Utf32LittleEndianBom => [0xFF, 0xFE, 0x00, 0x00];

    /// <summary>
    /// The UTF-32 big endian byte order mark.
    /// </summary>
    private static ReadOnlySpan<byte> Utf32BigEndianBom => [0x00, 0x00, 0xFE, 0xFF];

    /// <summary>
    /// Decodes <paramref name="bytes"/> to text. A byte order mark selects UTF-8, UTF-16 or UTF-32.
    /// Without one, text whose first or second byte is zero is UTF-16, because a SIMIS file starts with an ASCII character,
    /// and other text is decoded with <paramref name="encoding"/>, which defaults to UTF-8.
    /// </summary>
    /// <param name="bytes">Content of the file.</param>
    /// <param name="encoding">Encoding of text that has no byte order mark and is not UTF-16, such as an ANSI file. Defaults to UTF-8.</param>
    /// <returns>The decoded text without the byte order mark.</returns>
    /// <exception cref="NotSupportedException">When the bytes are a binary or compressed SIMIS file.</exception>
    /// <exception cref="FormatException">When the bytes are not valid text in the detected encoding.</exception>
    public static string Decode(ReadOnlySpan<byte> bytes, Encoding? encoding = null)
    {
        if (IsBinaryOrCompressed(bytes))
            throw new NotSupportedException("Binary and compressed SIMIS files are not supported. Only text SIMIS files can be read.");

        (Encoding detected, int bomLength) = DetectEncoding(bytes, encoding);

        try
        {
            return detected.GetString(bytes[bomLength..]);
        }
        catch (DecoderFallbackException ex)
        {
            string hint = ReferenceEquals(detected, Utf8) && bomLength == 0
                ? " A file without a byte order mark is read as UTF-8 unless its encoding, such as an ANSI code page, is passed to Load."
                : string.Empty;

            throw new FormatException($"The file is not valid {detected.WebName} text.{hint}", ex);
        }
    }

    /// <summary>
    /// Returns true when <paramref name="bytes"/> start with the ASCII header of a binary or compressed SIMIS file.
    /// A compressed file starts with <c>SIMISA@F</c>. An uncompressed file starts with <c>SIMISA@@@@@@@@@@JINX0</c>,
    /// followed by a two-character file type and <c>b</c> for binary or <c>t</c> for text, as in <c>JINX0s1b</c>.
    /// </summary>
    /// <param name="bytes">Content of the file.</param>
    /// <returns>True when the file is binary or compressed.</returns>
    private static bool IsBinaryOrCompressed(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith("SIMISA@F"u8))
            return true;

        ReadOnlySpan<byte> header = "SIMISA@@@@@@@@@@JINX0"u8;
        int formatIndex = header.Length + 2;

        return bytes.StartsWith(header) && bytes.Length > formatIndex && bytes[formatIndex] == (byte)'b';
    }

    /// <summary>
    /// Returns the encoding of <paramref name="bytes"/> and the length of its byte order mark.
    /// </summary>
    /// <param name="bytes">Content of the file.</param>
    /// <param name="encoding">Encoding of text that has no byte order mark and is not UTF-16, or null for UTF-8.</param>
    /// <returns>The encoding, and the number of bytes of the byte order mark, or 0 when there is none.</returns>
    private static (Encoding Encoding, int BomLength) DetectEncoding(ReadOnlySpan<byte> bytes, Encoding? encoding)
    {
        if (bytes.StartsWith(Utf32LittleEndianBom))
            return (Utf32LittleEndian, Utf32LittleEndianBom.Length);

        if (bytes.StartsWith(Utf32BigEndianBom))
            return (Utf32BigEndian, Utf32BigEndianBom.Length);

        if (bytes.StartsWith(Utf16LittleEndianBom))
            return (Utf16LittleEndian, Utf16LittleEndianBom.Length);

        if (bytes.StartsWith(Utf16BigEndianBom))
            return (Utf16BigEndian, Utf16BigEndianBom.Length);

        if (bytes.StartsWith(Utf8Bom))
            return (Utf8, Utf8Bom.Length);

        if (bytes.Length >= 2 && bytes[0] != 0 && bytes[1] == 0)
            return (Utf16LittleEndian, 0);

        if (bytes.Length >= 2 && bytes[0] == 0 && bytes[1] != 0)
            return (Utf16BigEndian, 0);

        return (encoding ?? Utf8, 0);
    }
}
