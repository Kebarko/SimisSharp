using System.Globalization;
using System.Text;

namespace KE.MSTS.SimisSharp;

/// <summary>
/// Parses and formats Simis values the way Open Rails reads them, independent of the current culture.
/// Numbers use a decimal point and may carry a unit suffix such as <c>10t</c> or <c>1.2kN</c>,
/// hexadecimal numbers have no prefix, and booleans are <c>true</c>, <c>false</c> or an integer.
/// Units are returned as written. Converting between units is left to the caller.
/// </summary>
public static class SimisValue
{
    private const NumberStyles NumberStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands
        | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowTrailingWhite;

    private const NumberStyles HexStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowHexSpecifier | NumberStyles.AllowTrailingWhite;

    /// <summary>
    /// Decodes the escape sequences of the text between the quotes of a quoted value, as Open Rails does:
    /// <c>\n</c> is a line feed, <c>\t</c> is a tab, and a backslash before any other character stands for that character,
    /// so <c>\\</c> is a backslash and <c>\"</c> is a double quote.
    /// </summary>
    /// <param name="text">Text between the quotes, as written.</param>
    /// <returns>The decoded text.</returns>
    public static string Unescape(ReadOnlySpan<char> text)
    {
        int backslash = text.IndexOf('\\');
        if (backslash < 0)
            return text.ToString();

        StringBuilder sb = new(text.Length);
        sb.Append(text[..backslash]);

        for (int i = backslash; i < text.Length; i++)
        {
            char c = text[i];
            if (c != '\\' || i == text.Length - 1)
            {
                sb.Append(c);
                continue;
            }

            char next = text[++i];
            sb.Append(next switch
            {
                'n' => '\n',
                't' => '\t',
                _ => next,
            });
        }

        return sb.ToString();
    }

    /// <summary>
    /// Encodes <paramref name="text"/> for writing between the quotes of a quoted value, so that <see cref="Unescape"/> reads it back unchanged:
    /// a backslash becomes <c>\\</c>, a double quote <c>\"</c>, a line feed <c>\n</c> and a tab <c>\t</c>.
    /// </summary>
    /// <param name="text">Text to encode.</param>
    /// <returns>The encoded text, without the surrounding quotes.</returns>
    public static string Escape(string text)
    {
        if (text.AsSpan().IndexOfAny("\\\"\n\t") < 0)
            return text;

        StringBuilder sb = new(text.Length + 8);
        foreach (char c in text)
        {
            switch (c)
            {
                case '\\':
                    sb.Append(@"\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\n':
                    sb.Append(@"\n");
                    break;
                case '\t':
                    sb.Append(@"\t");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses a number without a unit, such as <c>1.5</c> or <c>-2e3</c>. A trailing comma is ignored, as Open Rails does.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is a number.</returns>
    public static bool TryParseDouble(string? text, out double value)
    {
        return TryParseDouble(text.AsSpan(), out value);
    }

    /// <summary>
    /// Parses a number without a unit, such as <c>1.5</c> or <c>-2e3</c>. A trailing comma is ignored, as Open Rails does.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is a number.</returns>
    public static bool TryParseDouble(ReadOnlySpan<char> text, out double value)
    {
        return double.TryParse(text.TrimEnd(','), NumberStyle, NumberFormatInfo.InvariantInfo, out value);
    }

    /// <summary>
    /// Parses a number with an optional unit suffix, such as <c>10t</c>, <c>1.2kN</c> or <c>-3.5e2m</c>.
    /// Spaces between the number and the unit are skipped, and a <c>#</c> in the unit starts a comment, so <c>220kN#est</c> has the unit <c>kN</c>.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="value">The number.</param>
    /// <param name="unit">The unit as written, or an empty string when there is none. Open Rails compares units case-insensitively.</param>
    /// <returns>True when <paramref name="text"/> starts with a number.</returns>
    public static bool TryParseNumber(string? text, out double value, out string unit)
    {
        bool parsed = TryParseNumber(text.AsSpan(), out value, out ReadOnlySpan<char> unitSpan);
        unit = unitSpan.ToString();
        return parsed;
    }

    /// <summary>
    /// Parses a number with an optional unit suffix, as <see cref="TryParseNumber(string?, out double, out string)"/> does,
    /// returning the unit as a part of <paramref name="text"/>.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="value">The number.</param>
    /// <param name="unit">The unit as written, or an empty span when there is none.</param>
    /// <returns>True when <paramref name="text"/> starts with a number.</returns>
    public static bool TryParseNumber(ReadOnlySpan<char> text, out double value, out ReadOnlySpan<char> unit)
    {
        unit = [];

        int numberLength = GetNumberLength(text);

        if (!TryParseDouble(text[..numberLength], out value))
            return false;

        ReadOnlySpan<char> suffix = text[numberLength..].TrimStart(' ');
        int commentStart = suffix.IndexOf('#');
        if (commentStart >= 0)
            suffix = suffix[..commentStart];

        unit = suffix.Trim();
        return true;
    }

    /// <summary>
    /// Parses a signed 32-bit integer, such as <c>-7</c>. A trailing comma is ignored, as Open Rails does.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is an integer in range.</returns>
    public static bool TryParseInt32(string? text, out int value)
    {
        return TryParseInt32(text.AsSpan(), out value);
    }

    /// <summary>
    /// Parses a signed 32-bit integer, such as <c>-7</c>. A trailing comma is ignored, as Open Rails does.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is an integer in range.</returns>
    public static bool TryParseInt32(ReadOnlySpan<char> text, out int value)
    {
        return int.TryParse(text.TrimEnd(','), NumberStyle, NumberFormatInfo.InvariantInfo, out value);
    }

    /// <summary>
    /// Parses an unsigned 32-bit integer, such as <c>4294967295</c>. A trailing comma is ignored, as Open Rails does.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is an unsigned integer in range.</returns>
    public static bool TryParseUInt32(string? text, out uint value)
    {
        return TryParseUInt32(text.AsSpan(), out value);
    }

    /// <summary>
    /// Parses an unsigned 32-bit integer, such as <c>4294967295</c>. A trailing comma is ignored, as Open Rails does.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is an unsigned integer in range.</returns>
    public static bool TryParseUInt32(ReadOnlySpan<char> text, out uint value)
    {
        return uint.TryParse(text.TrimEnd(','), NumberStyle, NumberFormatInfo.InvariantInfo, out value);
    }

    /// <summary>
    /// Parses a hexadecimal number without a prefix, such as <c>ff00ff00</c>.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is a hexadecimal number in range.</returns>
    public static bool TryParseHex(string? text, out uint value)
    {
        return TryParseHex(text.AsSpan(), out value);
    }

    /// <summary>
    /// Parses a hexadecimal number without a prefix, such as <c>ff00ff00</c>.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is a hexadecimal number in range.</returns>
    public static bool TryParseHex(ReadOnlySpan<char> text, out uint value)
    {
        return uint.TryParse(text, HexStyle, NumberFormatInfo.InvariantInfo, out value);
    }

    /// <summary>
    /// Parses a boolean: <c>true</c> or <c>false</c> in any letter case, or an integer where any value other than zero is true.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is a boolean.</returns>
    public static bool TryParseBoolean(string? text, out bool value)
    {
        return TryParseBoolean(text.AsSpan(), out value);
    }

    /// <summary>
    /// Parses a boolean: <c>true</c> or <c>false</c> in any letter case, or an integer where any value other than zero is true.
    /// </summary>
    /// <returns>True when <paramref name="text"/> is a boolean.</returns>
    public static bool TryParseBoolean(ReadOnlySpan<char> text, out bool value)
    {
        if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        bool parsed = int.TryParse(text, NumberStyles.Any, NumberFormatInfo.InvariantInfo, out int number);
        value = parsed && number != 0;
        return parsed;
    }

    /// <summary>
    /// Formats a number with an optional unit, such as <c>12.5t</c>, using the shortest text that reads back as the same number.
    /// Numbers below 1e-5 or from 1e15 in magnitude are written with an exponent.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="value"/> is not finite.</exception>
    /// <exception cref="ArgumentException">When <paramref name="unit"/> would not be read back as the same unit.</exception>
    public static string Format(double value, string? unit = null)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "Only finite numbers can be written.");

        return AppendUnit(value.ToString(CultureInfo.InvariantCulture), unit);
    }

    /// <summary>
    /// Formats a single precision number with an optional unit, using the shortest text that reads back as the same number.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="value"/> is not finite.</exception>
    /// <exception cref="ArgumentException">When <paramref name="unit"/> would not be read back as the same unit.</exception>
    public static string Format(float value, string? unit = null)
    {
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "Only finite numbers can be written.");

        return AppendUnit(value.ToString(CultureInfo.InvariantCulture), unit);
    }

    /// <summary>
    /// Formats an integer with an optional unit, such as <c>42</c> or <c>800kW</c>.
    /// </summary>
    /// <exception cref="ArgumentException">When <paramref name="unit"/> would not be read back as the same unit.</exception>
    public static string Format(long value, string? unit = null)
    {
        return AppendUnit(value.ToString(CultureInfo.InvariantCulture), unit);
    }

    /// <summary>
    /// Formats a hexadecimal number without a prefix, in lower case, padded with zeros to <paramref name="digits"/> digits.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="digits"/> is negative.</exception>
    public static string FormatHex(uint value, int digits = 8)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(digits);

        return value.ToString("x" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Appends <paramref name="unit"/> to a formatted number after checking that it reads back as the same unit.
    /// </summary>
    private static string AppendUnit(string number, string? unit)
    {
        if (string.IsNullOrEmpty(unit))
            return number;

        string result = number + unit;
        if (!TryParseNumber(result, out _, out string parsedUnit) || parsedUnit != unit)
            throw new ArgumentException($"The unit '{unit}' cannot be written after a number.", nameof(unit));

        return result;
    }

    /// <summary>
    /// Returns the length of the number at the start of <paramref name="text"/>, using the same rules as Open Rails:
    /// an optional sign, then digits, decimal points, commas and an exponent.
    /// </summary>
    private static int GetNumberLength(ReadOnlySpan<char> text)
    {
        int i = 0;
        for (; i < text.Length; i++)
        {
            char c = text[i];

            if (i == 0 && (c is '+' or '-'))
                continue;

            if (c is '.' or ',')
                continue;

            if ((c is 'e' or 'E') && i < text.Length - 1)
            {
                char next = text[i + 1];
                if (next is '+' or '-')
                {
                    i++;
                    continue;
                }

                if (char.IsAsciiDigit(next))
                    continue;

                break;
            }

            if (!char.IsAsciiDigit(c))
                break;
        }

        return i;
    }
}
