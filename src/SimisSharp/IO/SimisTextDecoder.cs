using System.IO.Compression;
using System.Text;

namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Decodes the bytes of a text SIMIS file. MSTS writes UTF-16LE with a byte order mark, while Open Rails also reads UTF-8 and ASCII files.
/// A compressed file is decompressed first. Content that is not a text SIMIS document is rejected.
/// </summary>
internal static class SimisTextDecoder
{
    /// <summary>
    /// The header of an uncompressed file, which the subheader follows. A compressed file has another header in its place.
    /// </summary>
    private const string UncompressedHeader = "SIMISA@@@@@@@@@@";

    /// <summary>
    /// The start of the subheader: <c>JINX0</c>, which a two-character file type and the format letter follow, as in <c>JINX0D0t</c>.
    /// </summary>
    private const string SubheaderStart = "JINX0";

    /// <summary>
    /// Length of the header of a compressed file: <c>SIMISA@F</c>, the uncompressed length as a 32-bit integer and <c>@@@@</c>.
    /// </summary>
    private const int CompressedHeaderLength = 16;

    /// <summary>
    /// Length of the subheader that follows the header, such as <c>JINX0D0t______</c> and a line break.
    /// </summary>
    private const int SubheaderLength = 16;

    /// <summary>
    /// Index of the format letter in the subheader: <c>b</c> for binary or <c>t</c> for text.
    /// </summary>
    private const int FormatIndex = 7;

    /// <summary>
    /// Message of the exception thrown for a binary file.
    /// </summary>
    private const string BinaryNotSupported = "Binary SIMIS files are not supported, compressed or not. Only text SIMIS files can be read.";

    /// <summary>
    /// Message of the exception thrown for content that is not a SIMIS document.
    /// </summary>
    private const string NotSimisDocument = "The file is not a SIMIS document. A text SIMIS file starts with a header such as SIMISA@@@@@@@@@@JINX0D0t______.";

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
    /// The text must start with the header of a text SIMIS file, such as <c>SIMISA@@@@@@@@@@JINX0D0t______</c>, which is checked before the rest is decoded.
    /// A compressed file is decompressed first (see <see cref="DecodeCompressed"/>).
    /// </summary>
    /// <param name="bytes">Content of the file.</param>
    /// <param name="encoding">Encoding of text that has no byte order mark and is not UTF-16, such as an ANSI file. Defaults to UTF-8.</param>
    /// <returns>The decoded text without the byte order mark.</returns>
    /// <exception cref="NotSupportedException">When the bytes are not a SIMIS document, or are a binary SIMIS file, compressed or not.</exception>
    /// <exception cref="FormatException">When the bytes are not valid text in the detected encoding, or the compressed data is truncated or corrupt.</exception>
    public static string Decode(byte[] bytes, Encoding? encoding = null)
    {
        if (bytes.AsSpan().StartsWith("SIMISA@F"u8))
            return DecodeCompressed(bytes, encoding);

        (Encoding detected, int bomLength) = DetectEncoding(bytes, encoding);
        ReadOnlySpan<byte> text = bytes.AsSpan(bomLength);

        string header = DecodeStart(text, detected, UncompressedHeader.Length + FormatIndex + 1);
        CheckSubheader(header.StartsWith(UncompressedHeader, StringComparison.Ordinal) ? header.AsSpan(UncompressedHeader.Length) : []);

        return DecodeText(text, detected, bomLength > 0);
    }

    /// <summary>
    /// Decompresses a compressed file and decodes its text as Open Rails does. The 16-byte header of a compressed file is <c>SIMISA@F</c>,
    /// the uncompressed length as a 32-bit integer, which is not needed, and <c>@@@@</c>. A zlib stream follows, which holds the rest of the file
    /// as an uncompressed file has it: the ASCII subheader, such as <c>JINX0D0t______</c> and a line break, and the content.
    /// The text after the subheader is decoded as <see cref="Decode"/> decodes a file.
    /// </summary>
    /// <param name="bytes">Content of the file, which starts with <c>SIMISA@F</c>.</param>
    /// <param name="encoding">Encoding of text that has no byte order mark and is not UTF-16, or null for UTF-8.</param>
    /// <returns>The text an uncompressed file with the same content has, which starts with <c>SIMISA@@@@@@@@@@</c>.</returns>
    /// <exception cref="NotSupportedException">When the file holds binary data or no SIMIS document, as a compressed texture does.</exception>
    /// <exception cref="FormatException">When the file is truncated or corrupt, or its text is not valid in the detected encoding.</exception>
    private static string DecodeCompressed(byte[] bytes, Encoding? encoding)
    {
        ReadOnlySpan<byte> data = Decompress(bytes);
        if (data.IsEmpty)
            throw new FormatException("The compressed file is truncated.");

        CheckSubheader(DecodeStart(data, Encoding.ASCII, FormatIndex + 1));

        ReadOnlySpan<byte> subheader = data[..Math.Min(SubheaderLength, data.Length)];
        ReadOnlySpan<byte> text = data[subheader.Length..];
        (Encoding detected, int bomLength) = DetectEncoding(text, encoding);

        return string.Concat(UncompressedHeader, Encoding.Latin1.GetString(subheader), DecodeText(text[bomLength..], detected, bomLength > 0));
    }

    /// <summary>
    /// Decompresses the zlib stream that follows the header of a compressed file.
    /// <see cref="ZLibStream"/> does not report data that ends early, so a truncated file decompresses to less data without an error.
    /// </summary>
    /// <param name="bytes">Content of the file, which starts with <c>SIMISA@F</c>.</param>
    /// <returns>The decompressed data, which is empty when the file has no data after the header.</returns>
    /// <exception cref="FormatException">When the compressed data is corrupt.</exception>
    private static ReadOnlySpan<byte> Decompress(byte[] bytes)
    {
        if (bytes.Length <= CompressedHeaderLength)
            return [];

        MemoryStream data = new();
        try
        {
            using MemoryStream compressed = new(bytes, CompressedHeaderLength, bytes.Length - CompressedHeaderLength, writable: false);
            using ZLibStream zlib = new(compressed, CompressionMode.Decompress);
            zlib.CopyTo(data);
        }
        catch (InvalidDataException ex)
        {
            throw new FormatException("The compressed data of the file is corrupt.", ex);
        }

        return data.GetBuffer().AsSpan(0, (int)data.Length);
    }

    /// <summary>
    /// Decodes text with the encoding detected for it.
    /// </summary>
    /// <param name="bytes">The text to decode, without its byte order mark.</param>
    /// <param name="encoding">The detected encoding.</param>
    /// <param name="hasByteOrderMark">True when a byte order mark selected the encoding, which decides the hint in the error message.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="FormatException">When the bytes are not valid text in the encoding.</exception>
    private static string DecodeText(ReadOnlySpan<byte> bytes, Encoding encoding, bool hasByteOrderMark)
    {
        try
        {
            return encoding.GetString(bytes);
        }
        catch (DecoderFallbackException ex)
        {
            string hint = ReferenceEquals(encoding, Utf8) && !hasByteOrderMark
                ? " A file without a byte order mark is read as UTF-8 unless its encoding, such as an ANSI code page, is passed to Load."
                : string.Empty;

            throw new FormatException($"The file is not valid {encoding.WebName} text.{hint}", ex);
        }
    }

    /// <summary>
    /// Decodes the first <paramref name="length"/> characters of <paramref name="bytes"/> to check the SIMIS header, whose characters are ASCII.
    /// Only the bytes of these characters are decoded, so the content of a file that is not a SIMIS document is never decoded.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    /// <param name="encoding">Encoding of the bytes.</param>
    /// <param name="length">Number of characters to decode.</param>
    /// <returns>The characters, fewer when the bytes are shorter, or an empty string when the bytes are not valid in the encoding.</returns>
    private static string DecodeStart(ReadOnlySpan<byte> bytes, Encoding encoding, int length)
    {
        // Every ASCII character takes the same number of bytes in the encodings SIMIS files use.
        int byteCount = Math.Min(bytes.Length, encoding.GetByteCount("@") * length);

        try
        {
            return encoding.GetString(bytes[..byteCount]);
        }
        catch (DecoderFallbackException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Checks that <paramref name="subheader"/> is that of a text file. The subheader is <c>JINX0</c>,
    /// a two-character file type and <c>b</c> for binary or <c>t</c> for text, as in <c>JINX0D0t</c>.
    /// </summary>
    /// <param name="subheader">The start of the subheader, or an empty span when the content has no SIMIS header.</param>
    /// <exception cref="NotSupportedException">When the subheader is that of a binary file, or is missing.</exception>
    private static void CheckSubheader(ReadOnlySpan<char> subheader)
    {
        char format = subheader.StartsWith(SubheaderStart) && subheader.Length > FormatIndex ? subheader[FormatIndex] : '\0';

        if (format == 'b')
            throw new NotSupportedException(BinaryNotSupported);

        if (format != 't')
            throw new NotSupportedException(NotSimisDocument);
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
