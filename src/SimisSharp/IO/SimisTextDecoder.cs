using System.IO.Compression;
using System.Text;

namespace KE.MSTS.SimisSharp.IO;

/// <summary>
/// Decodes the bytes of a text SIMIS file. MSTS writes UTF-16LE with a byte order mark, while Open Rails also reads UTF-8 and ASCII files.
/// A compressed file is decompressed first.
/// </summary>
internal static class SimisTextDecoder
{
    /// <summary>
    /// The header of an uncompressed file. A compressed file has another header in its place.
    /// </summary>
    private const string UncompressedHeader = "SIMISA@@@@@@@@@@";

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
    /// A compressed file is decompressed first (see <see cref="DecodeCompressed"/>).
    /// </summary>
    /// <param name="bytes">Content of the file.</param>
    /// <param name="encoding">Encoding of text that has no byte order mark and is not UTF-16, such as an ANSI file. Defaults to UTF-8.</param>
    /// <returns>The decoded text without the byte order mark.</returns>
    /// <exception cref="NotSupportedException">When the bytes are a binary SIMIS file, compressed or not, or a compressed file that does not hold SIMIS text.</exception>
    /// <exception cref="FormatException">When the bytes are not valid text in the detected encoding, or the compressed data is truncated or corrupt.</exception>
    public static string Decode(byte[] bytes, Encoding? encoding = null)
    {
        if (bytes.AsSpan().StartsWith("SIMISA@F"u8))
            return DecodeCompressed(bytes, encoding);

        if (IsBinary(bytes))
            throw new NotSupportedException(BinaryNotSupported);

        return DecodeText(bytes, encoding);
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
    /// <exception cref="NotSupportedException">When the file holds binary data rather than text.</exception>
    /// <exception cref="FormatException">When the file is truncated or corrupt, or its text is not valid in the detected encoding.</exception>
    private static string DecodeCompressed(byte[] bytes, Encoding? encoding)
    {
        ReadOnlySpan<byte> data = Decompress(bytes);
        if (data.IsEmpty)
            throw new FormatException("The compressed file is truncated.");

        byte format = GetFormat(data);
        if (format == 'b')
            throw new NotSupportedException(BinaryNotSupported);

        if (format != 't')
            throw new NotSupportedException("The compressed file does not hold SIMIS text. Only text SIMIS files can be read.");

        ReadOnlySpan<byte> subheader = data[..Math.Min(SubheaderLength, data.Length)];
        return string.Concat(UncompressedHeader, Encoding.Latin1.GetString(subheader), DecodeText(data[subheader.Length..], encoding));
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
    /// Decodes text without a SIMIS header check, as <see cref="Decode"/> describes.
    /// </summary>
    /// <param name="bytes">The text to decode.</param>
    /// <param name="encoding">Encoding of text that has no byte order mark and is not UTF-16, or null for UTF-8.</param>
    /// <returns>The decoded text without the byte order mark.</returns>
    /// <exception cref="FormatException">When the bytes are not valid text in the detected encoding.</exception>
    private static string DecodeText(ReadOnlySpan<byte> bytes, Encoding? encoding)
    {
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
    /// Returns true when <paramref name="bytes"/> start with the ASCII header of an uncompressed binary SIMIS file:
    /// <c>SIMISA@@@@@@@@@@</c> and a subheader for binary data, such as <c>JINX0s1b</c>.
    /// </summary>
    /// <param name="bytes">Content of the file.</param>
    /// <returns>True when the file is binary.</returns>
    private static bool IsBinary(ReadOnlySpan<byte> bytes)
    {
        return bytes.StartsWith("SIMISA@@@@@@@@@@"u8) && GetFormat(bytes[UncompressedHeader.Length..]) == 'b';
    }

    /// <summary>
    /// Returns the format letter of the ASCII subheader that <paramref name="data"/> starts with. The subheader is <c>JINX0</c>,
    /// a two-character file type and <c>b</c> for binary or <c>t</c> for text, as in <c>JINX0s1b</c>.
    /// </summary>
    /// <param name="data">The data that follows the SIMIS header.</param>
    /// <returns>The format letter, or 0 when <paramref name="data"/> does not start with a subheader.</returns>
    private static byte GetFormat(ReadOnlySpan<byte> data)
    {
        return data.StartsWith("JINX0"u8) && data.Length > FormatIndex ? data[FormatIndex] : (byte)0;
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
