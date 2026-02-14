using System.Text;

namespace KE.MSTS.SimisSharp;

/// <summary>
/// Utility class for matching strings against wildcard patterns.
/// </summary>
public static class WildcardMatcher
{
    /// <summary>
    /// Determines whether the specified text matches the given pattern, which may include wildcard characters.
    /// </summary>
    /// <param name="text">The string to test for a match against the pattern. This parameter cannot be null.</param>
    /// <param name="pattern">The pattern to match the text against. The pattern may contain the wildcard characters '?' to match any single character and '*' to match any sequence of characters, including an empty sequence. This parameter cannot be null.</param>
    /// <returns>true if the text matches the pattern; otherwise, false.</returns>
    public static bool IsMatch(string text, string pattern)
    {
        if (text == null || pattern == null)
            return false;

        // Normalize the pattern by collapsing consecutive '*' characters into a single '*'.
        pattern = NormalizePattern(pattern);

        // Count the number of non-asterisk characters in the pattern to quickly rule out impossible matches.
        int requiredChars = 0;
        foreach (char c in pattern)
        {
            if (c != '*')
                requiredChars++;
        }

        if (text.Length < requiredChars)
            return false;

        // Create a 2D table to store results of subproblems.
        // table[i, j] will be true if the first i characters in text match the first j characters in pattern.
        var table = new bool[text.Length + 1, pattern.Length + 1];

        // Empty text matches empty pattern.
        table[0, 0] = true;

        // Handle patterns with leading '*' characters.
        for (int i = 1; i <= pattern.Length; i++)
        {
            if (pattern[i - 1] == '*')
                table[0, i] = table[0, i - 1];
        }

        //  Fill the table.
        for (int i = 1; i <= text.Length; i++)
        {
            for (int j = 1; j <= pattern.Length; j++)
            {
                if (pattern[j - 1] == '*')
                {
                    // '*' can match zero characters (table[i, j - 1]) or one/more characters (table[i - 1, j]).
                    table[i, j] = table[i, j - 1] || table[i - 1, j];
                }
                else if (pattern[j - 1] == '?' || pattern[j - 1] == text[i - 1])
                {
                    // Match current character if it's '?' or matches the text character.
                    table[i, j] = table[i - 1, j - 1];
                }
            }
        }

        // The result is in the bottom-right cell of the table.
        return table[text.Length, pattern.Length];
    }

    /// <summary>
    /// Normalizes the given wildcard pattern by collapsing consecutive asterisk ('*') characters into a single asterisk.
    /// </summary>
    /// <param name="pattern">The input wildcard pattern.</param>
    /// <returns>A normalized pattern in which any sequence of consecutive '*' characters is replaced with a single '*'.</returns>
    private static string NormalizePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return pattern;

        var sb = new StringBuilder();

        bool isPreviousStar = false;
        foreach (char c in pattern)
        {
            if(c == '*')
            {
                if (!isPreviousStar)
                    sb.Append(c);
                isPreviousStar = true;
            }
            else
            {
                sb.Append(c);
                isPreviousStar = false;
            }
        }

        return sb.ToString();
    }
}
